using UnityEngine;

namespace MjolnirMenu.UI
{
    /// <summary>Layout widgets that look like the website: toggle rows with hints, gold sliders, chips.</summary>
    public static class Ui
    {
        public const float Indent = 28f;

        public static void Header(string text)
            => GUILayout.Label(text.ToUpperInvariant(), Styles.Header);

        public static void Hint(string text, params GUILayoutOption[] opts)
            => GUILayout.Label(text, Styles.Hint, opts);

        public static void Label(string text, params GUILayoutOption[] opts)
            => GUILayout.Label(text, Styles.Label, opts);

        public static string Rich(string label, string? hint)
            => string.IsNullOrEmpty(hint) ? label : $"{label}\n<size=11><color={Styles.HintHex}>{hint}</color></size>";

        /// <summary>
        /// Toggle row: transparent button with a checkbox drawn at the left. Returns true on the
        /// frame the value changed. Hint renders as a muted second line.
        /// </summary>
        public static bool Toggle(ref bool value, string label, string? hint = null, params GUILayoutOption[] opts)
        {
            bool clicked = GUILayout.Button(Rich(label, hint), Styles.ToggleRow, opts);
            var r = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint)
            {
                var box = new Rect(r.x + 7, r.y + (string.IsNullOrEmpty(hint) ? (r.height - 16f) / 2f : 6f), 16, 16);
                GUI.DrawTexture(box, value ? Styles.CheckOn : Styles.CheckOff, ScaleMode.StretchToFill, true);
            }
            if (!clicked) return false;
            value = !value;
            return true;
        }

        /// <summary>Same as Toggle but for a plain bool you don't hold by ref (returns new value).</summary>
        public static bool ToggleValue(bool value, string label, string? hint = null, params GUILayoutOption[] opts)
        {
            Toggle(ref value, label, hint, opts);
            return value;
        }

        /// <summary>
        /// Gold slider with a filled track, indented to sit under a toggle's label. Returns true when changed.
        /// </summary>
        public static bool Slider(ref float value, float min, float max, float width = 240f, bool indent = true)
        {
            GUILayout.BeginHorizontal();
            if (indent) GUILayout.Space(Indent);
            var r = GUILayoutUtility.GetRect(width, 14f, GUILayout.Width(width));
            GUILayout.EndHorizontal();

            float t = Mathf.InverseLerp(min, max, value);
            if (Event.current.type == EventType.Repaint)
            {
                var track = new Rect(r.x, r.y + 5f, r.width, 4f);
                GUI.DrawTexture(track, Styles.TrackTex, ScaleMode.StretchToFill, true, 0f, Color.white, 0f, 3f);
                float fillW = Mathf.Max(4f, r.width * t);
                GUI.DrawTexture(new Rect(track.x, track.y, fillW, track.height), Styles.FillTex, ScaleMode.StretchToFill, true, 0f, Color.white, 0f, 3f);
            }
            float nv = GUI.HorizontalSlider(r, value, min, max, Styles.SliderTrack, Styles.SliderThumb);
            if (Mathf.Approximately(nv, value)) return false;
            value = nv;
            return true;
        }

        /// <summary>Slider with a muted caption to its left (used for value-only rows like "Melee ×2.0").</summary>
        public static bool LabeledSlider(string caption, ref float value, float min, float max, float width = 240f)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(Indent);
            GUILayout.Label(caption, Styles.Hint);
            GUILayout.EndHorizontal();
            return Slider(ref value, min, max, width);
        }

        public static bool Button(string text, params GUILayoutOption[] opts)
            => GUILayout.Button(text, Styles.Button, opts);

        public static bool Button(string text, bool active, params GUILayoutOption[] opts)
            => GUILayout.Button(text, active ? Styles.ButtonActive : Styles.Button, opts);

        public static string TextField(string value, params GUILayoutOption[] opts)
            => GUILayout.TextField(value, Styles.TextField, opts);

        public static void Space(float px = 8f) => GUILayout.Space(px);

        public static Vector2 BeginScroll(Vector2 pos, float height)
            => GUILayout.BeginScrollView(pos, false, false, GUIStyle.none, Styles.Skin.verticalScrollbar, Styles.Skin.scrollView, GUILayout.Height(height));

        public static void EndScroll() => GUILayout.EndScrollView();

        /// <summary>Thin separator line.</summary>
        public static void Rule()
        {
            var r = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
                GUI.DrawTexture(new Rect(r.x, r.y, r.width, 1f), Styles.Solid, ScaleMode.StretchToFill, true, 0f, Styles.Line, 0f, 0f);
        }
    }
}
