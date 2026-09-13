using HarmonyLib;
using MjolnirMenu.Core;
using MjolnirMenu.Features;

namespace MjolnirMenu.Patches
{
    /// <summary>Player-level hooks. Player overrides the Character virtuals, so patch Player, not Character.</summary>
    [HarmonyPatch(typeof(Player))]
    internal static class PlayerPatches
    {
        [HarmonyPrefix, HarmonyPatch(nameof(Player.UseStamina))]
        private static bool UseStamina_Prefix(Player __instance)
        {
            if (!ReferenceEquals(__instance, Player.m_localPlayer)) return true;
            if (State.InfiniteStamina) return false;
            if (State.CrouchInfiniteStamina && __instance.IsCrouching()) return false;
            return true;
        }

        /// <summary>
        /// Teleport (portal or ours) waits on m_teleportTimer: &gt;2s before the move, &gt;8s for portals
        /// before landing. Feeding a scaled dt shortens the whole vortex sequence.
        /// </summary>
        [HarmonyPrefix, HarmonyPatch("UpdateTeleport")]
        private static void UpdateTeleport_Prefix(Player __instance, ref float dt)
        {
            if (State.FastTeleport && __instance.m_teleporting && ReferenceEquals(__instance, Player.m_localPlayer))
                dt *= State.TeleportSpeed;
        }

        [HarmonyPrefix, HarmonyPatch(nameof(Player.UseEitr))]
        private static bool UseEitr_Prefix(Player __instance)
            => !(State.InfiniteEitr && ReferenceEquals(__instance, Player.m_localPlayer));

        [HarmonyPostfix, HarmonyPatch(nameof(Player.GetMaxCarryWeight))]
        private static void GetMaxCarryWeight_Postfix(Player __instance, ref float __result)
        {
            if (State.InfiniteCarryWeight && ReferenceEquals(__instance, Player.m_localPlayer))
                __result = 100000f;
        }

        [HarmonyPostfix, HarmonyPatch("GetRunSpeedFactor")]
        private static void GetRunSpeedFactor_Postfix(Player __instance, ref float __result)
        {
            if (State.SpeedHack && ReferenceEquals(__instance, Player.m_localPlayer))
                __result *= State.SpeedMultiplier;
        }

        [HarmonyPostfix, HarmonyPatch("GetJogSpeedFactor")]
        private static void GetJogSpeedFactor_Postfix(Player __instance, ref float __result)
        {
            if (State.SpeedHack && ReferenceEquals(__instance, Player.m_localPlayer))
                __result *= State.SpeedMultiplier;
        }

        // Free craft: skip the resource check (only for the actual craft, not recipe discovery) and the consume step.
        [HarmonyPrefix, HarmonyPatch(nameof(Player.HaveRequirements), typeof(Recipe), typeof(bool), typeof(int), typeof(int))]
        private static bool HaveRequirements_Prefix(Player __instance, bool discover, ref bool __result)
        {
            if (!State.FreeCraft || discover || !ReferenceEquals(__instance, Player.m_localPlayer)) return true;
            __result = true;
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(nameof(Player.ConsumeResources))]
        private static bool ConsumeResources_Prefix(Player __instance)
            => !(State.FreeCraft && ReferenceEquals(__instance, Player.m_localPlayer));

        // Menu open => game ignores gameplay keys/mouse so typing in the search box doesn't swing an axe.
        [HarmonyPostfix, HarmonyPatch("TakeInput")]
        private static void TakeInput_Postfix(ref bool __result)
        {
            if (State.MenuOpen) __result = false;
        }

        // Player object goes away on logout/death: clear tracked snapshots so the next instance is re-captured.
        [HarmonyPrefix, HarmonyPatch("OnDestroy")]
        private static void OnDestroy_Prefix(Player __instance)
        {
            if (ReferenceEquals(__instance, Player.m_localPlayer))
            {
                Spawner.Invalidate();
                Effects.Invalidate();
            }
        }
    }

    [HarmonyPatch(typeof(PlayerController))]
    internal static class PlayerControllerPatches
    {
        /// <summary>Second input gate: movement and mouse-look come through here.</summary>
        [HarmonyPostfix, HarmonyPatch("TakeInput")]
        private static void TakeInput_Postfix(ref bool __result)
        {
            if (State.MenuOpen) __result = false;
        }
    }

    [HarmonyPatch(typeof(PlayerProfile))]
    internal static class PlayerProfilePatches
    {
        /// <summary>
        /// The game tags loot/crafts as cheated whenever the player dealt damage in god/ghost/fly mode
        /// (Character.ApplyDamage sets the player ZDO's 'cheated' flag). bypasscheatchecks is its own
        /// off switch; we report it as set.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch("s_bypassCheatChecks", MethodType.Getter)]
        private static void BypassCheatChecks_Postfix(ref bool __result)
        {
            if (State.HideCheatTags) __result = true;
        }
    }
}
