using HarmonyLib;
using MjolnirMenu.Core;
using MjolnirMenu.Features;
using UnityEngine;

namespace MjolnirMenu.Patches
{
    [HarmonyPatch(typeof(Minimap))]
    internal static class MinimapPatches
    {
        /// <summary>Ctrl + left click on the big map teleports there when Map Warp is on.</summary>
        [HarmonyPrefix, HarmonyPatch(nameof(Minimap.OnMapLeftClick))]
        private static bool OnMapLeftClick_Prefix(Minimap __instance)
        {
            if (!State.MapClickTeleport) return true;
            if (!(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) return true;

            var world = __instance.ScreenToWorldPoint(Input.mousePosition);
            Warp.ToMapPoint(world);
            return false;
        }

        /// <summary>Legit exploration while the map is revealed still goes into the fog snapshot.</summary>
        [HarmonyPostfix, HarmonyPatch("Explore", typeof(Vector3), typeof(float))]
        private static void Explore_Postfix(Minimap __instance, Vector3 p, float radius) => MapFog.OnExplore(__instance, p, radius);
    }
}
