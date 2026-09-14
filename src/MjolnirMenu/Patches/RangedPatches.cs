using HarmonyLib;
using MjolnirMenu.Core;
using UnityEngine;

namespace MjolnirMenu.Patches
{
    /// <summary>
    /// Bow draw: draw% = m_attackDrawTime / drawDuration while the attack key is held; releasing
    /// fires through StartAttack with that percentage. m_attackDrawTime &lt; 0 means "don't draw until
    /// the key is released" (the game resets it to 0 on release), which is the lever insta-shot uses.
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GetAttackDrawPercentage))]
    internal static class DrawPercentagePatch
    {
        private static void Postfix(Humanoid __instance, ref float __result)
        {
            if (__result <= 0f) return; // not drawing
            if ((State.InstaFocus || State.InstaShot) && ReferenceEquals(__instance, Player.m_localPlayer))
                __result = 1f;
        }
    }

    [HarmonyPatch(typeof(Player), "UpdateAttackBowDraw")]
    internal static class InstaShotPatch
    {
        private static bool _armed = true; // semi-auto: one shot per press

        private static void Postfix(Player __instance, ItemDrop.ItemData weapon)
        {
            if (!State.InstaShot || !ReferenceEquals(__instance, Player.m_localPlayer)) return;

            if (!__instance.m_attackHold)
            {
                _armed = true;
                return;
            }

            if (__instance.m_attackDrawTime > 0f)
            {
                // A draw started this frame. Either fire now or suppress it.
                if (!_armed || __instance.InAttack())
                {
                    __instance.m_attackDrawTime = -1f;
                    HideDraw(__instance, weapon);
                    return;
                }

                __instance.m_attackDrawTime = 999f; // full draw for StartAttack -> Attack.Start
                __instance.StartAttack(null, false);
                __instance.m_attackDrawTime = -1f;  // no re-draw until release (or until auto re-arms below)
                HideDraw(__instance, weapon);
                if (!State.InstaShotAuto) _armed = false;
                return;
            }

            // Full-auto: once the shot animation is over and the key is still held, allow the next draw.
            if (State.InstaShotAuto && _armed && __instance.m_attackDrawTime < 0f && !__instance.InAttack())
                __instance.m_attackDrawTime = 0f;
        }

        private static void HideDraw(Player p, ItemDrop.ItemData weapon)
        {
            var anim = weapon?.m_shared?.m_attack?.m_drawAnimationState;
            if (!string.IsNullOrEmpty(anim)) p.m_zanim.SetBool(anim, false);
        }
    }

    /// <summary>
    /// Hitscan: aim from the camera (that is where the reticle is), find what it hits, and send the
    /// projectile from the bow straight at that point — fast, no gravity, no drag, no spread. The
    /// arrow still flies (so its trail shows) but along a line that passes exactly through the target.
    /// </summary>
    [HarmonyPatch(typeof(Projectile), nameof(Projectile.Setup))]
    internal static class HitscanPatch
    {
        private const float Speed = 180f;
        private const float MaxRange = 500f;
        private static int _mask = -1;

        private static int Mask
        {
            get
            {
                if (_mask < 0)
                    _mask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid",
                        "terrain", "character", "character_net", "character_ghost", "vehicle", "hitbox", "character_noenv");
                return _mask;
            }
        }

        private static void Postfix(Projectile __instance, Character owner, Vector3 velocity)
        {
            if (!State.Hitscan || owner == null || !ReferenceEquals(owner, Player.m_localPlayer)) return;
            if (velocity.sqrMagnitude < 0.01f) return;

            var cam = GameCamera.instance != null ? GameCamera.instance.transform : null;
            Vector3 rayOrigin = cam != null ? cam.position : __instance.transform.position;
            Vector3 rayDir = cam != null ? cam.forward : velocity.normalized;

            Vector3 target = rayOrigin + rayDir * MaxRange;
            float bestDist = float.MaxValue;
            foreach (var h in Physics.RaycastAll(rayOrigin, rayDir, MaxRange, Mask))
            {
                if (h.collider == null || h.collider.transform.root == owner.transform.root) continue;
                if (h.distance < bestDist) { bestDist = h.distance; target = h.point; }
            }

            var dir = (target - __instance.transform.position).normalized;
            __instance.m_gravity = 0f;
            __instance.m_drag = 0f;
            __instance.m_vel = dir * Speed;
            __instance.transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    /// <summary>Menu teleports: hide the fade-to-black while our own teleport is in flight.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.IsTeleporting))]
    internal static class SilentTeleportPatch
    {
        private static void Postfix(Player __instance, ref bool __result)
        {
            if (__result && Features.Warp.Silent && ReferenceEquals(__instance, Player.m_localPlayer))
                __result = false;
        }
    }
}
