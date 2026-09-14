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
        public static bool CrouchSpeedHack;
        public static float CrouchSpeedMultiplier = 3f;
        public static bool CrouchInfiniteStamina;
        public static bool EmoteSpeedHack;
        public static float EmoteSpeedMultiplier = 3f;

        // Water
        public static bool SwimSpeedHack;
        public static float SwimSpeedMultiplier = 3f;
        public static bool WaterJump;
        public static bool WalkOnWater;
        public static bool SeabedWalk;
        public static bool UnderwaterCamera;
        public static bool SwimUseItems;

        // Combat
        public static bool InfiniteDurability;
        public static bool DamageHack;
        public static float MeleeMultiplier = 2f;
        public static float RangedMultiplier = 2f;
        public static bool AttackSpeedHack;
        public static float AttackSpeedMultiplier = 2f;
        public static float TreeMultiplier = 1f;
        public static float RockMultiplier = 1f;
        public static float PlayerStructureMultiplier = 1f;
        public static float WorldStructureMultiplier = 1f;
        public static bool InfiniteItems;
        public static bool InfiniteInteract;
        public static bool InstaFocus;
        public static bool InstaShot;
        public static bool InstaShotAuto;
        public static bool InstaReload;
        public static bool Hitscan;

        // Effects
        public static bool NoPowerCooldown;
        public static bool InfinitePower;

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
        public static bool FastTeleport;
        public static float TeleportSpeed = 10f;
        public static bool InstantTeleport = true;

        // Misc
        public static bool HideCheatTags = true;
        public static bool SpawnerShowAll;

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
            HideCheatTags = MenuConfig.HideCheatTags.Value;
        }

        /// <summary>Turn everything off and restore any game fields we touched.</summary>
        public static void ResetAll()
        {
            GodMode = GhostMode = InfiniteStamina = InfiniteEitr = NoFallDamage = false;
            Fly = SpeedHack = JumpHack = InfiniteCarryWeight = false;
            SwimSpeedHack = WaterJump = WalkOnWater = SeabedWalk = UnderwaterCamera = SwimUseItems = false;
            InstaFocus = InstaShot = InstaShotAuto = InstaReload = Hitscan = false;
            CrouchSpeedHack = CrouchInfiniteStamina = EmoteSpeedHack = false;
            FastTeleport = false;
            InfiniteDurability = DamageHack = AttackSpeedHack = false;
            InfiniteItems = InfiniteInteract = false;
            NoPowerCooldown = InfinitePower = false;
            Features.Effects.Frozen.Clear();
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
