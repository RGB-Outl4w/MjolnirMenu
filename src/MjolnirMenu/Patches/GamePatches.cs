using HarmonyLib;
using MjolnirMenu.Core;

namespace MjolnirMenu.Patches
{
    /// <summary>Instant growth: Plant.SUpdate grows the plant once TimeSincePlanted passes this.</summary>
    [HarmonyPatch(typeof(Plant), "GetGrowTime")]
    internal static class PlantGrowPatch
    {
        private static void Postfix(ref float __result)
        {
            if (State.InstantGrowth) __result = 0f;
        }
    }

    [HarmonyPatch(typeof(Game))]
    internal static class GamePatches
    {
        /// <summary>Logout: drop every cheat so nothing leaks into the next world/character.</summary>
        [HarmonyPrefix, HarmonyPatch(nameof(Game.Logout))]
        private static void Logout_Prefix()
        {
            State.ResetAll();
            State.MenuOpen = false;
        }
    }
}
