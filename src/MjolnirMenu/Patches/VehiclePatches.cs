using HarmonyLib;
using MjolnirMenu.Core;
using MjolnirMenu.Features;
using UnityEngine;

namespace MjolnirMenu.Patches
{
    [HarmonyPatch(typeof(Vagon))]
    internal static class VagonPatches
    {
        private static bool IsLocal(GameObject go) => Player.m_localPlayer != null && go == Player.m_localPlayer.gameObject;

        /// <summary>
        /// Also runs every frame while hitched (false → detach). Sticky keeps the hitch through sharp turns, dodges
        /// and tipping; snap lets you hitch from anywhere. Teleporting still unhitches, or the cart would fly after you.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch("CanAttach")]
        private static void CanAttach_Postfix(Vagon __instance, GameObject go, ref bool __result)
        {
            if (__result || !IsLocal(go) || Player.m_localPlayer.IsTeleporting()) return;
            bool hitched = ReferenceEquals(Vehicles.Cart, __instance) && __instance.IsAttached();
            if (hitched ? State.CartSticky : State.CartSnap) __result = true;
        }

        [HarmonyPrefix, HarmonyPatch("AttachTo")]
        private static void AttachTo_Prefix(Vagon __instance, GameObject go)
        {
            if (State.CartSnap && IsLocal(go)) Vehicles.SnapCart(__instance, Player.m_localPlayer);
        }

        [HarmonyPostfix, HarmonyPatch("AttachTo")]
        private static void AttachTo_Postfix(Vagon __instance, GameObject go)
        {
            if (IsLocal(go)) Vehicles.Cart = __instance;
        }

        [HarmonyPostfix, HarmonyPatch("Detach")]
        private static void Detach_Postfix(Vagon __instance)
        {
            if (ReferenceEquals(Vehicles.Cart, __instance)) Vehicles.Cart = null;
        }
    }

    [HarmonyPatch(typeof(Ship))]
    internal static class ShipPatches
    {
        private static bool Mine(Ship s) => ReferenceEquals(s, Vehicles.CurrentShip);

        /// <summary>
        /// Vanilla pushes the sail force at the mast top, so scaling it scales the pitch torque and flips the hull.
        /// Speed hack adds the extra at the centre of mass instead: all thrust, no torque.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch("GetSailForce")]
        private static void GetSailForce_Postfix(Ship __instance, Vector3 __result)
        {
            float mul = Vehicles.SpeedMul;
            if (mul > 1f && Mine(__instance))
                __instance.m_body.AddForce(__result * (__instance.m_body.mass * (mul - 1f)));
        }

        /// <summary>
        /// Tailwind: the sail force and the sail's swing read the wind through EnvMan. Swap in the helmsman's wind
        /// for just those two calls, then put the real one back — waves and everything else keep the natural wind.
        /// </summary>
        [HarmonyPrefix, HarmonyPatch("GetSailForce")]
        private static void SailForce_Prefix(Ship __instance, out Vector4? __state) => SwapWind(__instance, out __state);

        [HarmonyPrefix, HarmonyPatch("UpdateSail")]
        private static void UpdateSail_Prefix(Ship __instance, out Vector4? __state) => SwapWind(__instance, out __state);

        [HarmonyPostfix, HarmonyPatch("GetSailForce")]
        private static void SailForce_Postfix(Vector4? __state) => RestoreWind(__state);

        [HarmonyPostfix, HarmonyPatch("UpdateSail")]
        private static void UpdateSail_Postfix(Vector4? __state) => RestoreWind(__state);

        private static void SwapWind(Ship __instance, out Vector4? __state)
        {
            __state = null;
            var env = EnvMan.instance;
            if (Vehicles.TailwindDir is not Vector3 d || env == null || !Mine(__instance)) return;
            __state = env.m_wind;
            env.m_wind = new Vector4(d.x, 0f, d.z, 1f);
        }

        private static void RestoreWind(Vector4? __state)
        {
            if (__state is Vector4 w && EnvMan.instance != null) EnvMan.instance.m_wind = w;
        }

        /// <summary>Vanilla gives a dead-astern wind only 70 % — full sail from any angle while Tailwind steers.</summary>
        [HarmonyPostfix, HarmonyPatch(nameof(Ship.GetWindAngleFactor))]
        private static void GetWindAngleFactor_Postfix(Ship __instance, ref float __result)
        {
            if (Vehicles.TailwindDir != null && Mine(__instance)) __result = 1f;
        }

        [HarmonyPostfix, HarmonyPatch(nameof(Ship.CustomFixedUpdate))]
        private static void CustomFixedUpdate_Postfix(Ship __instance) => Vehicles.RideShip(__instance);

        /// <summary>Hull immunity: wave slams at speed ("hitting air") are water-impact damage, not collisions.</summary>
        [HarmonyPrefix, HarmonyPatch("UpdateWaterForce")]
        private static bool UpdateWaterForce_Prefix(Ship __instance, float depth, float time)
        {
            if (!State.VehicleGod) return true;
            __instance.m_lastDepth = depth;
            __instance.m_lastUpdateWaterForceTime = time;
            return false;
        }

        [HarmonyPrefix, HarmonyPatch("UpdateUpsideDmg")]
        private static bool UpdateUpsideDmg_Prefix() => !State.VehicleGod;
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
