using UnityEngine;

namespace MjolnirMenu.UI
{
    /// <summary>Lazy-built IMGUI skin: dark slate panel, gold accents. Built on first OnGUI (GUI.skin needs a GUI context).</summary>
    public static class Styles
    {
        public static readonly Color Accent = new Color(0.93f, 0.72f, 0.22f);
        public static readonly Color Panel = new Color(0.08f, 0.09f, 0.11f, 0.96f);
        public static readonly Color On = new Color(0.35f, 0.85f, 0.45f);
        public static readonly Color Off = new Color(0.75f, 0.75f, 0.75f);

        public static GUIStyle Window = null!;
        public static GUIStyle Title = null!;
        public static GUIStyle Tab = null!;
        public static GUIStyle TabActive = null!;
        public static GUIStyle Toggle = null!;
        public static GUIStyle Button = null!;
        public static GUIStyle Header = null!;
        public static GUIStyle Small = null!;
        public static GUIStyle Watermark = null!;
        public static GUIStyle ListRow = null!;

        private static bool _built;

        public static void EnsureBuilt()
        {
            if (_built) return;
            _built = true;

            Window = new GUIStyle(GUI.skin.window);
            Window.normal.background = Solid(Panel);
            Window.onNormal.background = Window.normal.background;
            Window.normal.textColor = Accent;
            Window.onNormal.textColor = Accent;
            Window.fontStyle = FontStyle.Bold;
            Window.fontSize = 14;
            Window.padding = new RectOffset(10, 10, 24, 10);
            Window.border = new RectOffset(2, 2, 2, 2);

            Title = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            Title.normal.textColor = Accent;

            Tab = new GUIStyle(GUI.skin.button) { fontSize = 12, fixedHeight = 24 };
            Tab.normal.background = Solid(new Color(0.16f, 0.17f, 0.2f));
            Tab.normal.textColor = Off;
            Tab.hover.background = Solid(new Color(0.22f, 0.23f, 0.27f));
            Tab.hover.textColor = Color.white;

            TabActive = new GUIStyle(Tab);
            TabActive.normal.background = Solid(new Color(0.55f, 0.42f, 0.12f));
            TabActive.normal.textColor = Color.white;
            TabActive.hover.background = TabActive.normal.background;

            Toggle = new GUIStyle(GUI.skin.toggle) { fontSize = 12 };
            Toggle.normal.textColor = Off;
            Toggle.onNormal.textColor = On;
            Toggle.hover.textColor = Color.white;
            Toggle.onHover.textColor = On;

            Button = new GUIStyle(GUI.skin.button) { fontSize = 12 };
            Button.normal.background = Solid(new Color(0.2f, 0.21f, 0.25f));
            Button.normal.textColor = Color.white;
            Button.hover.background = Solid(new Color(0.3f, 0.31f, 0.36f));
            Button.hover.textColor = Color.white;
            Button.active.background = Solid(new Color(0.55f, 0.42f, 0.12f));

            Header = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
            Header.normal.textColor = Accent;

            Small = new GUIStyle(GUI.skin.label) { fontSize = 11 };
            Small.normal.textColor = new Color(0.65f, 0.65f, 0.7f);

            Watermark = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
            Watermark.normal.textColor = new Color(Accent.r, Accent.g, Accent.b, 0.8f);

            ListRow = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            ListRow.normal.textColor = Color.white;
            ListRow.hover.textColor = Accent;
        }

        private static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            t.SetPixel(0, 0, c);
            t.Apply();
            t.hideFlags = HideFlags.HideAndDontSave;
            return t;
        }
    }
}
