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
            => !(State.InfiniteStamina && ReferenceEquals(__instance, Player.m_localPlayer));

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
                Spawner.Invalidate();
        }
    }
}
