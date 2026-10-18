using System.Collections.Generic;
using MjolnirMenu.Core;
using MjolnirMenu.UI;
using UnityEngine;

namespace MjolnirMenu.Features
{
    /// <summary>
    /// Morph: hides your own model and shows a stripped copy of a creature's "Visual" in its place, animated by
    /// copying your animator's parameters (walk / run / swim / jump use the same names on every Character).
    /// With empty hands, attack uses the creature's own attacks: its attack animation plays on the copy and the
    /// hit is applied with the creature weapon's damage, range and projectile. The camera scales to its height.
    /// Only you see the model; other players see your hits land.
    /// </summary>
    public static class Morph
    {
        public sealed class Ability
        {
            public string Label = "";
            public ItemDrop.ItemData Item = null!;
            public Attack Attack = null!;
        }

        private static GameObject? _model;
        private static Player? _player;
        private static Animator? _src, _dst;
        private static readonly List<AnimatorControllerParameter> _params = new List<AnimatorControllerParameter>();
        private static readonly List<string> _triggers = new List<string>();
        private static readonly List<Renderer> _hidden = new List<Renderer>();
        private static float _nextHide;

        public static readonly List<Ability> Abilities = new List<Ability>();
        public static int Selected;
        private static Ability? _pending;
        private static float _hitAt, _cooldownUntil, _nextCycle;

        private static float _heightRatio = 1f;
        private static GameCamera? _cam;
        private static Vector3 _camOffset, _camCombatOffset;
        private static float _camMaxDist;

        public static string Current { get; private set; } = "";
        public static bool Active => _model != null;

        /// <summary>True when an attack press should become a creature attack (morphed, abilities, empty hands).</summary>
        public static bool Claims(Humanoid h)
            => Active && Abilities.Count > 0 && ReferenceEquals(h, _player) && h.GetRightItem() == null && h.GetLeftItem() == null;

        public static void To(string prefabName, string label)
        {
            var p = Player.m_localPlayer;
            var prefab = ZNetScene.instance?.GetPrefab(prefabName);
            var visual = prefab != null ? prefab.transform.Find("Visual") : null;
            var ch = prefab != null ? prefab.GetComponent<Character>() : null;
            if (p == null || visual == null || ch == null)
            {
                Hotkeys.Notify(L.F("Can't morph into {0}", label));
                return;
            }
            Back(false);

            // Instantiate under an inactive holder so no component on the copy ever runs, then strip everything
            // but transforms, renderers, LOD groups and the animator.
            var holder = new GameObject("MjolnirMorphHolder");
            holder.SetActive(false);
            var copy = Object.Instantiate(visual.gameObject, holder.transform, false);
            for (int pass = 0; pass < 2; pass++) // second pass catches components another one depended on
            {
                foreach (var mb in copy.GetComponentsInChildren<MonoBehaviour>(true)) if (mb != null) Object.DestroyImmediate(mb);
                foreach (var j in copy.GetComponentsInChildren<Joint>(true)) if (j != null) Object.DestroyImmediate(j);
                foreach (var c in copy.GetComponentsInChildren<Collider>(true)) if (c != null) Object.DestroyImmediate(c);
                foreach (var rb in copy.GetComponentsInChildren<Rigidbody>(true)) if (rb != null) Object.DestroyImmediate(rb);
            }
            copy.transform.SetParent(p.transform, false);
            copy.transform.localPosition = visual.localPosition;
            copy.transform.localRotation = visual.localRotation;
            copy.transform.localScale = Vector3.Scale(visual.localScale, prefab!.transform.localScale);
            Object.Destroy(holder);

            _model = copy;
            _player = p;
            _src = p.m_animator;
            _dst = copy.GetComponentInChildren<Animator>();
            _params.Clear();
            _triggers.Clear();
            if (_dst != null && _src != null)
            {
                _dst.fireEvents = false; // the receivers (CharacterAnimEvent, FootStep) were stripped
                _dst.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var have = new Dictionary<string, AnimatorControllerParameterType>();
                foreach (var sp in _src.parameters) have[sp.name] = sp.type;
                foreach (var dp in _dst.parameters)
                {
                    if (dp.type == AnimatorControllerParameterType.Trigger) _triggers.Add(dp.name);
                    else if (have.TryGetValue(dp.name, out var t) && t == dp.type) _params.Add(dp);
                }
            }

            CollectAbilities(prefab.GetComponent<Humanoid>());
            var mobCap = prefab.GetComponent<CapsuleCollider>();
            var meCap = p.GetComponent<CapsuleCollider>();
            _heightRatio = mobCap != null && meCap != null && meCap.height > 0f
                ? Mathf.Clamp(mobCap.height * prefab.transform.localScale.y / meCap.height, 0.4f, 8f) : 1f;
            ApplyCamera();

            Current = label;
            _nextHide = 0f;
            Hotkeys.Notify(L.F("Morphed into {0}", label));
        }

        private static void CollectAbilities(Humanoid? h)
        {
            Abilities.Clear();
            Selected = 0;
            if (h == null) return;
            var gos = new List<GameObject>();
            if (h.m_defaultItems != null) gos.AddRange(h.m_defaultItems);
            if (h.m_randomWeapon != null) gos.AddRange(h.m_randomWeapon);
            if (h.m_randomSets != null) foreach (var set in h.m_randomSets) if (set?.m_items != null) gos.AddRange(set.m_items);
            var seen = new HashSet<string>();
            foreach (var go in gos)
            {
                var item = go != null ? go.GetComponent<ItemDrop>()?.m_itemData : null;
                if (item?.m_shared == null || !seen.Add(go!.name)) continue;
                Add(go.name, item, item.m_shared.m_attack);
                Add(go.name + " (2)", item, item.m_shared.m_secondaryAttack);
            }
        }

        private static void Add(string name, ItemDrop.ItemData item, Attack? atk)
        {
            if (atk == null || string.IsNullOrEmpty(atk.m_attackAnimation)) return;
            Abilities.Add(new Ability { Label = name.Replace('_', ' '), Item = item, Attack = atk });
        }

        public static void Back(bool notify = true)
        {
            foreach (var r in _hidden) if (r != null) r.forceRenderingOff = false;
            _hidden.Clear();
            if (_model != null) Object.Destroy(_model);
            bool was = _model != null || Current.Length > 0;
            _model = null;
            _player = null;
            _src = _dst = null;
            _pending = null;
            Abilities.Clear();
            RestoreCamera();
            Current = "";
            if (notify && was) Hotkeys.Notify("Back to your own shape");
        }

        /// <summary>Primary attack: use the selected creature attack. Secondary attack: select the next one.</summary>
        public static void OnAttack(bool secondary)
        {
            if (Abilities.Count == 0) return;
            if (secondary)
            {
                if (Time.time < _nextCycle) return;
                _nextCycle = Time.time + 0.3f;
                Selected = (Selected + 1) % Abilities.Count;
                return;
            }
            Use(Selected);
        }

        public static void Use(int i)
        {
            if (_dst == null || i < 0 || i >= Abilities.Count || Time.time < _cooldownUntil) return;
            var a = Abilities[i];
            string? trig = FindTrigger(a.Attack.m_attackAnimation);
            if (trig != null) _dst.SetTrigger(trig);
            _pending = a;
            // ponytail: fixed wind-up instead of the clip's OnAttackTrigger event (events are off on the copy).
            _hitAt = Time.time + 0.4f;
            _cooldownUntil = Time.time + Mathf.Clamp(a.Item.m_shared.m_aiAttackInterval, 0.8f, 3f);
        }

        /// <summary>Attack.Start triggers name, name + chain level or name + random index; take whichever exists.</summary>
        private static string? FindTrigger(string anim)
        {
            if (_triggers.Contains(anim)) return anim;
            var options = _triggers.FindAll(t => t.StartsWith(anim));
            return options.Count == 0 ? null : options[Random.Range(0, options.Count)];
        }

        private static void Hit(Ability a)
        {
            var p = _player;
            if (p == null) return;
            var atk = a.Attack;
            var sh = a.Item.m_shared;
            var aim = GameCamera.instance != null ? GameCamera.instance.transform.forward : p.transform.forward;
            var flat = new Vector3(aim.x, 0f, aim.z).sqrMagnitude > 0.001f ? new Vector3(aim.x, 0f, aim.z).normalized : p.transform.forward;

            var hit = new HitData
            {
                m_damage = sh.m_damages.Clone(),
                m_pushForce = sh.m_attackForce,
                m_toolTier = 100,
                m_hitType = HitData.HitType.PlayerHit,
            };
            hit.SetAttacker(p);

            if ((atk.m_attackType == Attack.AttackType.Projectile || atk.m_attackType == Attack.AttackType.TriggerProjectile) && atk.m_attackProjectile != null)
            {
                var origin = p.GetCenterPoint() + flat * (0.8f * _heightRatio) + Vector3.up * (0.4f * _heightRatio);
                for (int n = 0; n < Mathf.Max(1, atk.m_projectiles); n++)
                {
                    var go = Object.Instantiate(atk.m_attackProjectile, origin, Quaternion.LookRotation(aim));
                    go.GetComponent<IProjectile>()?.Setup(p, aim * Mathf.Max(atk.m_projectileVel, 10f), -1f, hit.Clone(), a.Item, null);
                }
                return;
            }

            float range = Mathf.Max(atk.m_attackRange, 1.5f);
            var center = (atk.m_attackType == Attack.AttackType.Area ? p.transform.position : p.transform.position + flat * range * 0.6f)
                         + Vector3.up * Mathf.Max(atk.m_attackHeight, 1f);
            var done = new HashSet<IDestructible>();
            foreach (var col in Physics.OverlapSphere(center, range * 0.75f))
            {
                var d = col.GetComponentInParent<IDestructible>();
                if (d == null || !done.Add(d)) continue;
                if (d is Character c && (ReferenceEquals(c, p) || c.IsPlayer() || c.IsTamed())) continue;
                var h = hit.Clone();
                h.m_point = col.ClosestPoint(center);
                h.m_dir = (h.m_point - p.transform.position).normalized;
                h.m_hitCollider = col;
                d.Damage(h);
            }
        }

        private static void ApplyCamera()
        {
            var cam = GameCamera.instance;
            if (cam == null) return;
            if (!ReferenceEquals(cam, _cam))
            {
                _cam = cam;
                _camOffset = cam.m_3rdOffset;
                _camCombatOffset = cam.m_3rdCombatOffset;
                _camMaxDist = cam.m_maxDistance;
            }
            cam.m_3rdOffset = _camOffset * _heightRatio;
            cam.m_3rdCombatOffset = _camCombatOffset * _heightRatio;
            cam.m_maxDistance = _camMaxDist * Mathf.Max(1f, _heightRatio);
        }

        private static void RestoreCamera()
        {
            if (_cam != null)
            {
                _cam.m_3rdOffset = _camOffset;
                _cam.m_3rdCombatOffset = _camCombatOffset;
                _cam.m_maxDistance = _camMaxDist;
            }
            _cam = null;
            _heightRatio = 1f;
        }

        public static void Tick()
        {
            if (Current.Length == 0) return;
            var p = Player.m_localPlayer;
            if (_model == null || p == null || !ReferenceEquals(p, _player)) { Back(false); return; }
            if (!ReferenceEquals(GameCamera.instance, _cam)) ApplyCamera();

            if (_pending != null && Time.time >= _hitAt)
            {
                var a = _pending;
                _pending = null;
                Hit(a);
            }

            // Gear swaps spawn new renderers on the player, so re-hide twice a second.
            if (Time.unscaledTime >= _nextHide && p.m_visual != null)
            {
                _nextHide = Time.unscaledTime + 0.5f;
                foreach (var r in p.m_visual.GetComponentsInChildren<Renderer>(true))
                    if (!r.forceRenderingOff) { r.forceRenderingOff = true; _hidden.Add(r); }
            }

            if (_src == null || _dst == null) return;
            foreach (var prm in _params)
            {
                if (prm.type == AnimatorControllerParameterType.Float) _dst.SetFloat(prm.nameHash, _src.GetFloat(prm.nameHash));
                else if (prm.type == AnimatorControllerParameterType.Bool) _dst.SetBool(prm.nameHash, _src.GetBool(prm.nameHash));
                else if (prm.type == AnimatorControllerParameterType.Int) _dst.SetInteger(prm.nameHash, _src.GetInteger(prm.nameHash));
            }
        }

        /// <summary>Small panel above the hotbar: the creature's attacks, the selected one highlighted.</summary>
        public static void DrawHud()
        {
            if (!Active) return;
            Styles.EnsureBuilt();
            var saved = GUI.skin;
            GUI.skin = Styles.Skin;
            try
            {
                int rows = Mathf.Max(1, Abilities.Count);
                var r = new Rect(Screen.width / 2f - 170f, Screen.height - 200f - rows * 24f, 340f, 58f + rows * 24f);
                GUI.Box(r, GUIContent.none, Styles.Window);
                GUILayout.BeginArea(new Rect(r.x + 12f, r.y + 8f, r.width - 24f, r.height - 12f));
                GUILayout.Label(L.F("Morph: {0}", Current), Styles.Header);
                bool empty = _player != null && _player.GetRightItem() == null && _player.GetLeftItem() == null;
                GUILayout.Label(L.T(Abilities.Count == 0 ? "No attacks — movement only"
                    : empty ? "Attack: use selected · Secondary attack: next" : "Put your weapons away to use these"), Styles.Hint);
                for (int i = 0; i < Abilities.Count; i++)
                    if (GUILayout.Button(Abilities[i].Label, i == Selected ? Styles.ButtonActive : Styles.Button)) Selected = i;
                GUILayout.EndArea();
            }
            finally { GUI.skin = saved; }
        }
    }
}
