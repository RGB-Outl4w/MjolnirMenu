using MjolnirMenu.Features;

namespace MjolnirMenu.Core
{
    /// <summary>
    /// Single source of truth for every toggle. Harmony patches read these statics;
    /// the UI writes them. Anything that mutates game fields lives in Features/* so it
    /// can also undo itself.
    /// </summary>
    public static class State
    {
        public static bool MenuOpen;

        // Player
        public static bool GodMode;
        public static bool GhostMode;
        public static bool InfiniteStamina;
        public static bool InfiniteEitr;
        public static bool NoFallDamage;
        public static bool Fly;
        public static bool SpeedHack;
        public static bool JumpHack;
        public static bool InfiniteCarryWeight;
        public static float SpeedMultiplier = 2f;
        public static float JumpMultiplier = 2f;

        // World / build
        public static bool FreeBuild;
        public static bool NoPlaceDelay;
        public static bool InstantCraft;
        public static bool FreeCraft;
        public static bool LockTimeOfDay;
        public static float TimeOfDay = 0.5f;
        public static string ForcedWeather = "";

        // Teleport
        public static bool MapClickTeleport;

        // ESP
        public static bool EspPlayers;
        public static bool EspCreatures;
        public static bool EspResources;
        public static bool EspDistance = true;
        public static bool EspLines;

        public static void LoadFromSettings()
        {
            SpeedMultiplier = MenuConfig.SpeedMultiplier.Value;
            JumpMultiplier = MenuConfig.JumpMultiplier.Value;
        }

        /// <summary>Turn everything off and restore any game fields we touched.</summary>
        public static void ResetAll()
        {
            GodMode = GhostMode = InfiniteStamina = InfiniteEitr = NoFallDamage = false;
            Fly = SpeedHack = JumpHack = InfiniteCarryWeight = false;
            FreeBuild = NoPlaceDelay = InstantCraft = FreeCraft = false;
            LockTimeOfDay = false;
            ForcedWeather = "";
            MapClickTeleport = false;
            EspPlayers = EspCreatures = EspResources = EspLines = false;

            PlayerCheats.ApplyAll();
            WorldCheats.ApplyAll();
        }
    }
}
