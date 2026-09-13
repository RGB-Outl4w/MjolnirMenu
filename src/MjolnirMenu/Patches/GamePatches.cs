using HarmonyLib;
using MjolnirMenu.Core;

namespace MjolnirMenu.Patches
{
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
