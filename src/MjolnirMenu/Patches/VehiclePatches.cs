using HarmonyLib;
using MjolnirMenu.Core;
using MjolnirMenu.Features;
using UnityEngine;

namespace MjolnirMenu.Patches
{
    [HarmonyPatch(typeof(Vagon))]
    internal static class VagonPatches
    {
        [HarmonyPostfix, HarmonyPatch("AttachTo")]
        private static void AttachTo_Postfix(Vagon __instance, GameObject go)
        {
            var p = Player.m_localPlayer;
            if (p != null && go == p.gameObject) Vehicles.Cart = __instance;
        }

        [HarmonyPostfix, HarmonyPatch("Detach")]
        private static void Detach_Postfix(Vagon __instance)
        {
            if (ReferenceEquals(Vehicles.Cart, __instance)) Vehicles.Cart = null;
        }
    }

    /// <summary>
    /// Every collision hit a ship or cart takes or deals goes through ImpactEffect.OnCollisionEnter on the owner,
    /// so one gate covers rocks, shores, trees and ship-vs-cart in both directions.
    /// </summary>
    [HarmonyPatch(typeof(ImpactEffect), "OnCollisionEnter")]
    internal static class ImpactPatch
    {
        private static bool Prefix(ImpactEffect __instance, Collision info)
        {
            if (!State.VehicleGod) return true;
            return !(IsVehicle(__instance.transform) || (info.collider != null && IsVehicle(info.collider.transform)));
        }

        private static bool IsVehicle(Transform t) => t.GetComponentInParent<Ship>() != null || t.GetComponentInParent<Vagon>() != null;
    }
}
