using System.Collections.Generic;
using MjolnirMenu.Core;
using UnityEngine;

namespace MjolnirMenu.Features
{
    /// <summary>
    /// Cosmetic morph: hides your own model and shows a stripped copy of a creature's "Visual" in its place,
    /// animated by copying your animator's parameters (walk / run / swim / jump use the same names on every
    /// Character). Only you see it; your hitbox, stats and attacks stay the player's.
    /// </summary>
    public static class Morph
    {
        private static GameObject? _model;
        private static Player? _player;
        private static Animator? _src, _dst;
        private static readonly List<AnimatorControllerParameter> _params = new List<AnimatorControllerParameter>();
        private static readonly List<Renderer> _hidden = new List<Renderer>();
        private static float _nextHide;

        public static string Current { get; private set; } = "";
        public static bool Active => _model != null;

        public static void To(string prefabName, string label)
        {
            var p = Player.m_localPlayer;
            var prefab = ZNetScene.instance?.GetPrefab(prefabName);
            var visual = prefab != null ? prefab.transform.Find("Visual") : null;
            if (p == null || visual == null || prefab!.GetComponent<Character>() == null)
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
            copy.transform.localScale = Vector3.Scale(visual.localScale, prefab.transform.localScale);
            Object.Destroy(holder);

            _model = copy;
            _player = p;
            _src = p.m_animator;
            _dst = copy.GetComponentInChildren<Animator>();
            _params.Clear();
            if (_dst != null && _src != null)
            {
                _dst.fireEvents = false; // the receivers (CharacterAnimEvent, FootStep) were stripped
                _dst.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var have = new Dictionary<string, AnimatorControllerParameterType>();
                foreach (var sp in _src.parameters) have[sp.name] = sp.type;
                foreach (var dp in _dst.parameters)
                    if (dp.type != AnimatorControllerParameterType.Trigger && have.TryGetValue(dp.name, out var t) && t == dp.type)
                        _params.Add(dp);
            }
            Current = label;
            _nextHide = 0f;
            Hotkeys.Notify(L.F("Morphed into {0}", label));
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
            Current = "";
            if (notify && was) Hotkeys.Notify("Back to your own shape");
        }

        public static void Tick()
        {
            if (Current.Length == 0) return;
            var p = Player.m_localPlayer;
            if (_model == null || p == null || !ReferenceEquals(p, _player)) { Back(false); return; }

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
    }
}
