using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MjolnirMenu.Core;
using MjolnirMenu.Features;
using MjolnirMenu.UI;

namespace MjolnirMenu
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("valheim.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; } = null!;
        public static ManualLogSource Log { get; private set; } = null!;

        private Harmony? _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            MenuConfig.Bind(Config);
            State.LoadFromSettings();

            _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            _harmony.PatchAll(typeof(Plugin).Assembly);

            Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION} loaded. Press {MenuConfig.MenuKey.Value} in-game.");
        }

        private void Update()
        {
            Hotkeys.Update();
            PlayerCheats.Tick();
            WorldCheats.Tick();
            Effects.Tick();
            Presets.Tick();
            Warp.Tick();
            Vehicles.Tick();
            Esp.Tick();
        }

        private void OnGUI()
        {
            Esp.Draw();
            MenuWindow.Draw();
        }

        private void OnDestroy()
        {
            State.ResetAll();
            _harmony?.UnpatchSelf();
        }
    }
}
