using System;
using UnityEngine;

namespace MjolnirMenu.UI
{
    /// <summary>
    /// IMGUI skin that mirrors the website: dark slate panels, gold accents, rounded corners,
    /// Segoe UI when the OS has it. Everything is generated at runtime (no bundled assets):
    /// rounded-rect textures are drawn pixel by pixel with a signed-distance alpha edge and
    /// used as 9-slice backgrounds.
    /// </summary>
    public static class Styles
    {
        // ---- palette (same values as docs/style.css) ----
        public static readonly Color Bg       = Hex("#111317");
        public static readonly Color Bg2      = Hex("#0b0d10");
        public static readonly Color Panel    = Hex("#15181d");
        public static readonly Color Field    = Hex("#1a1d23");
        public static readonly Color Line     = Hex("#2a2d34");
        public static readonly Color Text     = Hex("#e8e6e1");
        public static readonly Color Body     = Hex("#d6d6d6");
        public static readonly Color Muted    = Hex("#9a9ca4");
        public static readonly Color Gold     = Hex("#e0b83a");
        public static readonly Color Gold2    = Hex("#f2cf5c");
        public static readonly Color GoldDim  = Hex("#8a6f1e");
        public static readonly Color Active   = Hex("#8a6a1f");
        public static readonly Color Chip     = Hex("#23262d");
        public static readonly Color ChipHover= Hex("#2e323a");
        public static readonly Color RowHover = Hex("#1b1e24");
        public static readonly Color Track    = Hex("#2a2e36");
        public static readonly Color Green    = Hex("#5ad86f");
        public static readonly Color CheckBg  = Hex("#1b1e24");
        public static readonly Color CheckLine= Hex("#555555");

        public const string HintHex = "#9a9ca4";
        public const string GoldHex = "#e0b83a";

        // ---- skin + styles ----
        public static GUISkin Skin = null!;
        public static GUIStyle Window = null!;
        public static GUIStyle TitleBar = null!;
        public static GUIStyle TitleText = null!;
        public static GUIStyle TitleVersion = null!;
        public static GUIStyle Tab = null!;
        public static GUIStyle TabActive = null!;
        public static GUIStyle Button = null!;
        public static GUIStyle ButtonActive = null!;
        public static GUIStyle ToggleRow = null!;
        public static GUIStyle Label = null!;
        public static GUIStyle ListRow = null!;
        public static GUIStyle Hint = null!;
        public static GUIStyle Header = null!;
        public static GUIStyle Title = null!;
        public static GUIStyle Watermark = null!;
        public static GUIStyle TextField = null!;
        public static GUIStyle SliderTrack = null!;
        public static GUIStyle SliderThumb = null!;
        public static GUIStyle Footer = null!;

        // ---- textures used by widgets ----
        public static Texture2D CheckOff = null!;
        public static Texture2D CheckOn = null!;
        public static Texture2D TrackTex = null!;
        public static Texture2D FillTex = null!;
        public static Texture2D Solid = null!;

        public static Font? UiFont;
        private static bool _built;

        public static void EnsureBuilt()
        {
            if (_built) return;
            _built = true;

            UiFont = TryOsFont("Segoe UI", "Segoe UI Variable Text", "Noto Sans", "Arial");

            Skin = UnityEngine.Object.Instantiate(GUI.skin);
            Skin.hideFlags = HideFlags.HideAndDontSave;
            if (UiFont != null) Skin.font = UiFont;

            Solid = Flat(Color.white);
            CheckOff = Rounded(16, 3, CheckBg, CheckLine);
            CheckOn = Rounded(16, 3, new Color(Green.r, Green.g, Green.b, 0.18f), Green, new Color(Green.r, Green.g, Green.b, 1f), 4);
            TrackTex = Rounded(8, 3, Track, null);
            FillTex = Rounded(8, 3, GoldDim, null);

            var winTex = Rounded(24, 8, Bg, null);
            var titleTex = Rounded(24, 7, Bg2, null, null, 0, true);
            var chipTex = Rounded(12, 4, Chip, null);
            var chipHoverTex = Rounded(12, 4, ChipHover, null);
            var activeTex = Rounded(12, 4, Active, null);
            var rowHoverTex = Rounded(12, 4, RowHover, null);
            var fieldTex = Rounded(12, 4, Field, Line);
            var fieldFocusTex = Rounded(12, 4, Field, GoldDim);
            var thumbTex = Circle(14, Gold);
            var scrollTrackTex = Flat(new Color(1f, 1f, 1f, 0.03f));
            var scrollThumbTex = Rounded(8, 3, Hex("#3a3f49"), null);

            Window = new GUIStyle(Skin.window);
            Window.normal.background = winTex;
            Window.onNormal.background = winTex;
            Window.border = new RectOffset(10, 10, 10, 10);
            Window.padding = new RectOffset(1, 1, 1, 1);
            Window.contentOffset = Vector2.zero;
            Window.normal.textColor = Color.clear;
            Window.onNormal.textColor = Color.clear;

            TitleBar = new GUIStyle();
            TitleBar.normal.background = titleTex;
            TitleBar.padding = new RectOffset(14, 14, 7, 7);

            TitleText = Text_(Skin.label, 12, FontStyle.Bold, Gold);
            TitleVersion = Text_(Skin.label, 12, FontStyle.Normal, Muted);
            TitleVersion.alignment = TextAnchor.MiddleRight;

            Tab = Text_(Skin.button, 12, FontStyle.Normal, Hex("#bfbfbf"));
            Tab.normal.background = chipTex;
            Tab.hover.background = chipHoverTex;
            Tab.hover.textColor = Color.white;
            Tab.active.background = activeTex;
            Tab.active.textColor = Color.white;
            Tab.border = new RectOffset(5, 5, 5, 5);
            Tab.padding = new RectOffset(13, 13, 5, 5);
            Tab.margin = new RectOffset(3, 3, 0, 0);

            TabActive = new GUIStyle(Tab);
            TabActive.normal.background = activeTex;
            TabActive.normal.textColor = Color.white;
            TabActive.hover.background = activeTex;

            Button = new GUIStyle(Tab);
            Button.normal.textColor = Hex("#eeeeee");
            Button.padding = new RectOffset(10, 10, 4, 4);
            Button.margin = new RectOffset(2, 2, 2, 2);
            Button.fontSize = 12;

            ButtonActive = new GUIStyle(Button);
            ButtonActive.normal.background = activeTex;
            ButtonActive.hover.background = activeTex;
            ButtonActive.normal.textColor = Color.white;

            // Toggle rows are buttons with a transparent face; the checkbox is drawn on top by Ui.Toggle.
            ToggleRow = Text_(Skin.button, 13, FontStyle.Normal, Body);
            ToggleRow.normal.background = null;
            ToggleRow.hover.background = rowHoverTex;
            ToggleRow.active.background = rowHoverTex;
            ToggleRow.focused.background = null;
            ToggleRow.onNormal.background = null;
            ToggleRow.hover.textColor = Color.white;
            ToggleRow.active.textColor = Color.white;
            ToggleRow.border = new RectOffset(5, 5, 5, 5);
            ToggleRow.padding = new RectOffset(28, 8, 4, 4);
            ToggleRow.margin = new RectOffset(0, 0, 1, 1);
            ToggleRow.alignment = TextAnchor.MiddleLeft;
            ToggleRow.richText = true;
            ToggleRow.wordWrap = true;

            Label = Text_(Skin.label, 13, FontStyle.Normal, Body);
            Label.richText = true;
            Label.wordWrap = true;

            ListRow = Text_(Skin.label, 13, FontStyle.Normal, Text);
            ListRow.richText = true;

            Hint = Text_(Skin.label, 11, FontStyle.Normal, Muted);
            Hint.richText = true;
            Hint.wordWrap = true;

            Header = Text_(Skin.label, 11, FontStyle.Bold, Gold);
            Header.margin = new RectOffset(4, 4, 6, 2);

            Title = Text_(Skin.label, 18, FontStyle.Bold, Gold);

            Watermark = Text_(Skin.label, 12, FontStyle.Bold, new Color(Gold.r, Gold.g, Gold.b, 0.8f));

            TextField = Text_(Skin.textField, 12, FontStyle.Normal, Text);
            TextField.normal.background = fieldTex;
            TextField.hover.background = fieldTex;
            TextField.active.background = fieldFocusTex;
            TextField.focused.background = fieldFocusTex;
            TextField.onNormal.background = fieldTex;
            TextField.onFocused.background = fieldFocusTex;
            TextField.border = new RectOffset(5, 5, 5, 5);
            TextField.padding = new RectOffset(8, 8, 4, 4);
            TextField.fixedHeight = 24;

            SliderTrack = new GUIStyle(Skin.horizontalSlider);
            SliderTrack.normal.background = null; // track is drawn by Ui.Slider so the fill can be shown
            SliderTrack.fixedHeight = 14;
            SliderTrack.margin = new RectOffset(0, 0, 0, 0);

            SliderThumb = new GUIStyle(Skin.horizontalSliderThumb);
            SliderThumb.normal.background = thumbTex;
            SliderThumb.hover.background = Circle(14, Gold2);
            SliderThumb.active.background = Circle(14, Gold2);
            SliderThumb.fixedWidth = 14;
            SliderThumb.fixedHeight = 14;
            SliderThumb.border = new RectOffset(0, 0, 0, 0);
            SliderThumb.padding = new RectOffset(0, 0, 0, 0);

            Footer = Text_(Skin.label, 11, FontStyle.Normal, Muted);

            // Skin-level styles so scroll views / text fields inside GUILayout pick them up.
            Skin.label = Label;
            Skin.textField = TextField;
            Skin.button = Button;
            Skin.horizontalSlider = SliderTrack;
            Skin.horizontalSliderThumb = SliderThumb;

            Skin.verticalScrollbar = new GUIStyle(Skin.verticalScrollbar);
            Skin.verticalScrollbar.normal.background = scrollTrackTex;
            Skin.verticalScrollbar.fixedWidth = 8;
            Skin.verticalScrollbar.border = new RectOffset(0, 0, 0, 0);
            Skin.verticalScrollbar.padding = new RectOffset(0, 0, 0, 0);
            Skin.verticalScrollbar.margin = new RectOffset(4, 0, 0, 0);
            Skin.verticalScrollbarThumb = new GUIStyle(Skin.verticalScrollbarThumb);
            Skin.verticalScrollbarThumb.normal.background = scrollThumbTex;
            Skin.verticalScrollbarThumb.hover.background = Rounded(8, 3, GoldDim, null);
            Skin.verticalScrollbarThumb.border = new RectOffset(3, 3, 3, 3);
            Skin.verticalScrollbarThumb.fixedWidth = 8;
            Skin.verticalScrollbarUpButton = Empty();
            Skin.verticalScrollbarDownButton = Empty();
            Skin.horizontalScrollbar = Empty();
            Skin.horizontalScrollbarThumb = Empty();
            Skin.horizontalScrollbarLeftButton = Empty();
            Skin.horizontalScrollbarRightButton = Empty();
            Skin.scrollView = new GUIStyle();
        }

        // ---- helpers ----

        private static GUIStyle Text_(GUIStyle basis, int size, FontStyle fs, Color color)
        {
            var s = new GUIStyle(basis) { fontSize = size, fontStyle = fs };
            if (UiFont != null) s.font = UiFont;
            s.normal.textColor = color;
            s.hover.textColor = color;
            s.active.textColor = color;
            s.focused.textColor = color;
            s.onNormal.textColor = color;
            s.onHover.textColor = color;
            return s;
        }

        private static GUIStyle Empty()
        {
            var s = new GUIStyle();
            s.fixedWidth = 0;
            s.fixedHeight = 0;
            return s;
        }

        private static Font? TryOsFont(params string[] names)
        {
            try
            {
                var installed = Font.GetOSInstalledFontNames();
                foreach (var n in names)
                {
                    if (Array.IndexOf(installed, n) < 0) continue;
                    var f = Font.CreateDynamicFontFromOSFont(n, 13);
                    if (f != null) { f.hideFlags = HideFlags.HideAndDontSave; return f; }
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogDebug($"OS font unavailable: {e.Message}");
            }
            return null;
        }

        public static Color Hex(string hex)
            => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        /// <summary>
        /// The game renders IMGUI in linear colour space and our runtime textures end up being
        /// sRGB-encoded on output, which lifts every dark colour (#111317 showed as #3b3b3b).
        /// Storing the linear equivalent cancels that out. Vertex colours (text) are unaffected.
        /// </summary>
        private static Color Lin(Color c)
        {
            var l = c.linear;
            l.a = c.a;
            return l;
        }

        public static Texture2D Flat(Color c)
        {
            var t = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            t.SetPixel(0, 0, Lin(c));
            t.Apply();
            return t;
        }

        /// <summary>
        /// Anti-aliased rounded square usable as a 9-slice (border = radius). Optional 1px outline
        /// and an optional inner filled square (for the "on" checkbox).
        /// </summary>
        public static Texture2D Rounded(int size, int radius, Color fill, Color? outline, Color? inner = null, int innerInset = 0, bool topOnly = false)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear };
            var px = new Color[size * size];
            float r = Mathf.Clamp(radius, 0, size / 2f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // Texture row 0 is the bottom; "top only" keeps the lower half square.
                float rr = topOnly && y < size / 2 ? 0f : r;
                float d = RoundedDist(x + 0.5f, y + 0.5f, size, size, rr); // <0 inside
                float aOuter = Mathf.Clamp01(0.5f - d);
                Color c = fill;
                if (outline.HasValue)
                {
                    float aInner = Mathf.Clamp01(0.5f - (d + 1f));
                    c = Color.Lerp(outline.Value, fill, aInner);
                }
                if (inner.HasValue)
                {
                    float di = RoundedDist(x + 0.5f - innerInset, y + 0.5f - innerInset, size - innerInset * 2, size - innerInset * 2, 2f);
                    float ai = Mathf.Clamp01(0.5f - di);
                    c = Color.Lerp(c, inner.Value, ai);
                }
                c.a *= aOuter;
                px[y * size + x] = Lin(c);
            }
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        public static Texture2D Circle(int size, Color fill)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear };
            var px = new Color[size * size];
            float c = size / 2f, rad = size / 2f - 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c)) - rad;
                var col = fill;
                col.a *= Mathf.Clamp01(0.5f - d);
                px[y * size + x] = Lin(col);
            }
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        // Signed distance from (x,y) to a w×h rounded rectangle with corner radius r, origin at 0,0.
        private static float RoundedDist(float x, float y, float w, float h, float r)
        {
            float qx = Mathf.Abs(x - w / 2f) - (w / 2f - r);
            float qy = Mathf.Abs(y - h / 2f) - (h / 2f - r);
            float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }
    }
}
