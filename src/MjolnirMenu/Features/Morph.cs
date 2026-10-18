using System.Collections.Generic;
using MjolnirMenu.Core;
using MjolnirMenu.UI;
using UnityEngine;

namespace MjolnirMenu.Features
{
    /// <summary>
    /// Morph = possession. A real, networked creature is spawned and driven by your input (its AI is switched
    /// off), so everyone sees it with its own model, animations, attacks, effects, health and hitboxes. Your
    /// player is attached to a seat 30 m under the ground below it (out of everyone's sight, invulnerable) and
    /// the camera is moved to the creature. Without Spoof the creature carries your name.
    /// </summary>
    public static class Morph
    {
        public sealed class Ability
        {
            public string Label = "";
            public ItemDrop.ItemData Item = null!;
            public bool Secondary;
        }

        private static Character? _mob;
        private static BaseAI? _ai;
        private static Player? _player;
        private static GameObject? _seat;
        private static bool _canTakeOff, _canLand;
        private static float _heightRatio = 1f;
        private static float _nextCycle, _abilityRetryUntil;
        private static Vector3 _lastMobPos;

        public static readonly List<Ability> Abilities = new List<Ability>();
        public static int Selected;

        public static string Current { get; private set; } = "";
        public static bool Active => _mob != null;
        public static Character? Mob => _mob;

        public static void To(string prefabName, string label, int level)
        {
            var p = Player.m_localPlayer;
            var prefab = ZNetScene.instance?.GetPrefab(prefabName);
            if (p == null || prefab == null || prefab.GetComponent<Character>() == null || prefab.GetComponent<Player>() != null)
            {
                Hotkeys.Notify(L.F("Can't morph into {0}", label));
                return;
            }
            var spawnAt = Active ? _mob!.transform.position : p.transform.position;
            Back(false);

            var go = Object.Instantiate(prefab, spawnAt + Vector3.up * 0.5f, Quaternion.LookRotation(Flat(p.transform.forward)));
            var mob = go.GetComponent<Character>();
            if (level > 1) mob.SetLevel(level);
            _ai = go.GetComponent<BaseAI>();
            // Bosses and some monsters spawn asleep and only the AI wakes them; do it before switching the AI off.
            if (_ai is MonsterAI monster && monster.IsSleeping()) monster.Wakeup();
            if (_ai != null) _ai.enabled = false; // unregisters its update; the creature only moves when we tell it to
            // Character.TakeOff / Land fire these animator triggers: a creature without them can't change state.
            _canTakeOff = HasParam(mob.m_animator, "fly_takeoff");
            _canLand = HasParam(mob.m_animator, "fly_land");

            if (!State.MorphSpoof)
            {
                // Show your name over the creature. Tameables read their tamed name, everything else the override.
                var zdo = mob.m_nview.GetZDO();
                zdo.Set(ZDOVars.s_overrideHoverName, p.GetPlayerName());
                if (go.GetComponent<Tameable>() != null)
                {
                    mob.SetTamed(true);
                    zdo.Set(ZDOVars.s_tamedName, p.GetPlayerName());
                }
            }

            var mobCap = prefab.GetComponent<CapsuleCollider>();
            var meCap = p.GetComponent<CapsuleCollider>();
            _heightRatio = mobCap != null && meCap != null && meCap.height > 0f
                ? Mathf.Clamp(mobCap.height * prefab.transform.localScale.y / meCap.height, 0.4f, 8f) : 1f;

            _seat = new GameObject("MjolnirMorphSeat");
            _player = p;
            _mob = mob;
            _lastMobPos = mob.transform.position;
            UpdateSeat();
            p.AttachStart(_seat.transform, null, true, false, false, "attach_chair", Vector3.zero, null);
            if (p.m_collider != null) p.m_collider.enabled = false;

            Abilities.Clear();
            Selected = 0;
            _abilityRetryUntil = Time.time + 3f; // monsters get their weapons a moment after spawning
            Current = label;
            Hotkeys.Notify(L.F("Morphed into {0}", label));
        }

        public static void Back(bool notify = true)
        {
            bool was = Current.Length > 0;
            var p = _player;
            var mob = _mob;
            var pos = mob != null ? mob.transform.position : _lastMobPos;
            _mob = null;
            _ai = null;
            _player = null;
            Abilities.Clear();
            Current = "";

            if (p != null)
            {
                if (p.IsAttached()) p.AttachStop();
                if (p.m_collider != null) p.m_collider.enabled = true;
                var safe = pos + Vector3.up * 1f;
                p.transform.position = safe;
                if (p.m_body != null)
                {
                    p.m_body.position = safe;
                    p.m_body.linearVelocity = Vector3.zero;
                }
            }
            if (mob != null && mob.m_nview != null && mob.m_nview.IsValid() && !mob.IsDead())
                ZNetScene.instance.Destroy(mob.gameObject);
            if (_seat != null) Object.Destroy(_seat);
            _seat = null;
            if (notify && was) Hotkeys.Notify("Back to your own shape");
        }

        private static bool HasParam(Animator? a, string name)
        {
            if (a == null) return false;
            foreach (var p in a.parameters) if (p.name == name) return true;
            return false;
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward;
        }

        /// <summary>Your hidden body: 30 m under the ground (or seabed) below the creature, so nobody sees it.</summary>
        private static void UpdateSeat()
        {
            if (_seat == null || _mob == null) return;
            var at = _mob.transform.position;
            float ground = ZoneSystem.instance != null ? ZoneSystem.instance.GetGroundHeight(at) : at.y;
            _seat.transform.position = new Vector3(at.x, Mathf.Min(ground, at.y) - 30f, at.z);
            _seat.transform.rotation = Quaternion.LookRotation(Flat(_mob.transform.forward));
        }

        public static void Tick()
        {
            if (Current.Length == 0) return;
            if (_mob == null || _mob.IsDead() || _player == null || !ReferenceEquals(Player.m_localPlayer, _player))
            {
                Back(true);
                return;
            }
            _lastMobPos = _mob.transform.position;
            UpdateSeat();
            if (Abilities.Count == 0 && Time.time < _abilityRetryUntil) CollectAbilities();
        }

        private static void CollectAbilities()
        {
            if (!(_mob is Humanoid h)) return;
            foreach (var item in h.GetInventory().GetAllItems())
            {
                if (item?.m_shared == null) continue;
                string name = item.m_dropPrefab != null ? item.m_dropPrefab.name : item.m_shared.m_name;
                name = name.Replace('_', ' ');
                if (item.m_shared.m_attack != null && !string.IsNullOrEmpty(item.m_shared.m_attack.m_attackAnimation))
                    Abilities.Add(new Ability { Label = name, Item = item });
                if (item.m_shared.m_secondaryAttack != null && !string.IsNullOrEmpty(item.m_shared.m_secondaryAttack.m_attackAnimation))
                    Abilities.Add(new Ability { Label = name + " (2)", Item = item, Secondary = true });
            }
        }

        /// <summary>
        /// Replaces Player.SetControls while morphed. Movement is camera-relative; flying creatures climb with
        /// jump and sink with crouch; attack uses the selected creature attack, secondary attack picks the next.
        /// </summary>
        public static void Control(Vector3 movedir, bool attack, bool attackHold, bool secondary, bool jump, bool crouch, bool run)
        {
            var mob = _mob;
            var cam = GameCamera.instance;
            if (mob == null || cam == null) return;
            var camFwd = cam.transform.forward;
            var yaw = Quaternion.LookRotation(Flat(camFwd));
            var dir = yaw * new Vector3(movedir.x, 0f, movedir.z);

            bool climb = jump || ZInput.GetButton("Jump");
            bool sink = crouch || ZInput.GetButton("Crouch");
            if (mob.IsFlying())
            {
                if (movedir.z != 0f) dir = (camFwd * movedir.z + yaw * Vector3.right * movedir.x).normalized * Mathf.Min(1f, movedir.magnitude);
                if (climb) dir.y += 1f;
                if (sink) dir.y -= 1f;
                if (sink && _canLand && Altitude(mob) < 8f) mob.Land();
            }
            else if (jump)
            {
                if (_canTakeOff) mob.TakeOff();
                else mob.Jump(false);
            }

            mob.SetMoveDir(Vector3.ClampMagnitude(dir, 1f));
            mob.SetRun(run);
            // Look where the camera looks: aim (GetAimDir = look dir) and head; body follows via AlwaysRotateCamera.
            mob.SetLookDir(camFwd, 0f);

            if (secondary && Abilities.Count > 0 && Time.time >= _nextCycle)
            {
                _nextCycle = Time.time + 0.25f;
                Selected = (Selected + 1) % Abilities.Count;
            }
            if ((attack || attackHold) && Abilities.Count > 0 && mob is Humanoid h && !h.InAttack())
            {
                var a = Abilities[Mathf.Clamp(Selected, 0, Abilities.Count - 1)];
                if (!ReferenceEquals(h.GetCurrentWeapon(), a.Item)) h.EquipItem(a.Item, false);
                h.StartAttack(null, a.Secondary);
            }
        }

        private static float Altitude(Character c)
        {
            var pos = c.transform.position;
            float ground = ZoneSystem.instance != null ? ZoneSystem.instance.GetGroundHeight(pos) : pos.y;
            return pos.y - Mathf.Max(ground, ZoneSystem.instance != null ? ZoneSystem.instance.m_waterLevel : ground);
        }

        /// <summary>Third-person camera around the creature, distance scaled to its size, pushed in by walls.</summary>
        public static void Camera(GameCamera cam, ref Vector3 pos, Quaternion rot)
        {
            if (_mob == null) return;
            var eye = _mob.m_eye != null ? _mob.m_eye.position : _mob.GetCenterPoint();
            var center = eye + Vector3.up * (0.3f * _heightRatio);
            float dist = Mathf.Max(cam.m_distance, 1.5f) * Mathf.Max(1f, _heightRatio);
            var back = rot * Vector3.back;
            pos = Physics.SphereCast(center, 0.2f, back, out var hit, dist, cam.m_blockCameraMask)
                ? center + back * Mathf.Max(0.3f, hit.distance - 0.2f)
                : center + back * dist;
        }

        private static int _hudId = 0x4D4A4F4D;
        private static Rect _hud = new Rect(0, 0, 340, 10);

        /// <summary>Panel above the hotbar: creature health and its attacks. Uses the menu's scale and opacity.</summary>
        public static void DrawHud()
        {
            if (!Active) return;
            Styles.EnsureBuilt();
            var savedSkin = GUI.skin;
            var savedMatrix = GUI.matrix;
            var savedColor = GUI.color;
            GUI.skin = Styles.Skin;
            try
            {
                float s = Mathf.Clamp(MenuConfig.UiScale.Value, 0.75f, 1.5f);
                GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
                GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp(MenuConfig.UiOpacity.Value, 0.4f, 1f));
                // Auto-height window, re-anchored above the hotbar using last frame's height.
                _hud.x = Screen.width / s / 2f - _hud.width / 2f;
                _hud.y = Screen.height / s - 150f - _hud.height;
                _hud = GUILayout.Window(_hudId, new Rect(_hud.x, _hud.y, 340f, 10f), DrawHudWindow, GUIContent.none, Styles.Window);
            }
            finally
            {
                GUI.skin = savedSkin;
                GUI.matrix = savedMatrix;
                GUI.color = savedColor;
            }
        }

        private static void DrawHudWindow(int id)
        {
            GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp(MenuConfig.UiOpacity.Value, 0.4f, 1f));
            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.BeginVertical();
            GUILayout.Label(L.F("Morph: {0}", Current), Styles.Header);
            if (_mob != null) GUILayout.Label(L.F("Health {0:0} / {1:0}", _mob.GetHealth(), _mob.GetMaxHealth()), Styles.Hint);
            GUILayout.Label(L.T(Abilities.Count == 0 ? "No attacks — movement only" : "Attack: use selected · Secondary attack: next"), Styles.Hint);
            if (_mob != null && (_canTakeOff || _canLand || _mob.IsFlying())) GUILayout.Label(L.T("Jump: take off / climb · Crouch: descend / land"), Styles.Hint);
            for (int i = 0; i < Abilities.Count; i++)
                if (GUILayout.Button(Abilities[i].Label, i == Selected ? Styles.ButtonActive : Styles.Button)) Selected = i;
            GUILayout.Space(4);
            GUILayout.EndVertical();
            GUILayout.Space(10);
            GUILayout.EndHorizontal();
        }
    }
}
