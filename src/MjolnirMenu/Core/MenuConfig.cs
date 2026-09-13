using BepInEx.Configuration;
using UnityEngine;

namespace MjolnirMenu.Core
{
    /// <summary>BepInEx config entries. Persisted in BepInEx/config/com.mjolnir.menu.cfg.</summary>
    public static class MenuConfig
    {
        public static ConfigEntry<KeyboardShortcut> MenuKey = null!;
        public static ConfigEntry<KeyboardShortcut> GodKey = null!;
        public static ConfigEntry<KeyboardShortcut> FlyKey = null!;
        public static ConfigEntry<KeyboardShortcut> PanicKey = null!;

        public static ConfigEntry<bool> ShowWatermark = null!;
        public static ConfigEntry<float> WindowX = null!;
        public static ConfigEntry<float> WindowY = null!;

        public static ConfigEntry<float> SpeedMultiplier = null!;
        public static ConfigEntry<float> JumpMultiplier = null!;
        public static ConfigEntry<float> EspRange = null!;
        public static ConfigEntry<int> EspMaxEntities = null!;

        public static ConfigEntry<Color> EspPlayerColor = null!;
        public static ConfigEntry<Color> EspCreatureColor = null!;
        public static ConfigEntry<Color> EspBossColor = null!;
        public static ConfigEntry<Color> EspResourceColor = null!;

        public static void Bind(ConfigFile cfg)
        {
            MenuKey = cfg.Bind("Hotkeys", "MenuToggle", new KeyboardShortcut(KeyCode.Insert), "Open/close the MjolnirMenu window.");
            GodKey = cfg.Bind("Hotkeys", "GodToggle", new KeyboardShortcut(KeyCode.F6), "Quick-toggle god mode.");
            FlyKey = cfg.Bind("Hotkeys", "FlyToggle", new KeyboardShortcut(KeyCode.F7), "Quick-toggle fly.");
            PanicKey = cfg.Bind("Hotkeys", "PanicReset", new KeyboardShortcut(KeyCode.End), "Turn every cheat off at once.");

            ShowWatermark = cfg.Bind("UI", "ShowWatermark", true, "Small MjolnirMenu tag in the top-left corner.");
            WindowX = cfg.Bind("UI", "WindowX", 60f, "Saved window X position.");
            WindowY = cfg.Bind("UI", "WindowY", 60f, "Saved window Y position.");

            SpeedMultiplier = cfg.Bind("Player", "SpeedMultiplier", 2f, new ConfigDescription("Run/jog speed multiplier when Speed Hack is on.", new AcceptableValueRange<float>(1f, 10f)));
            JumpMultiplier = cfg.Bind("Player", "JumpMultiplier", 2f, new ConfigDescription("Jump force multiplier when Jump Hack is on.", new AcceptableValueRange<float>(1f, 10f)));

            EspRange = cfg.Bind("ESP", "Range", 150f, new ConfigDescription("Max distance (m) for ESP labels.", new AcceptableValueRange<float>(20f, 1000f)));
            EspMaxEntities = cfg.Bind("ESP", "MaxEntities", 250, new ConfigDescription("Hard cap on labels drawn per frame.", new AcceptableValueRange<int>(10, 2000)));
            EspPlayerColor = cfg.Bind("ESP", "PlayerColor", new Color(0.3f, 0.8f, 1f), "Label color for players.");
            EspCreatureColor = cfg.Bind("ESP", "CreatureColor", new Color(1f, 0.45f, 0.3f), "Label color for creatures.");
            EspBossColor = cfg.Bind("ESP", "BossColor", new Color(1f, 0.2f, 0.9f), "Label color for bosses.");
            EspResourceColor = cfg.Bind("ESP", "ResourceColor", new Color(0.6f, 1f, 0.5f), "Label color for pickables/ore/trees.");
        }
    }
}
