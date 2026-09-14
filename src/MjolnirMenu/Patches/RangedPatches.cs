using HarmonyLib;
using MjolnirMenu.Core;
using UnityEngine;

namespace MjolnirMenu.Patches
{
    /// <summary>
    /// Bow draw: draw% = m_attackDrawTime / drawDuration while the attack key is held; releasing
    /// fires through StartAttack with that percentage. Insta-focus clamps the percentage, insta-shot
    /// fires on the first draw frame, hitscan relocates the projectile onto the aim ray.
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
        private static bool _armed = true;

        private static void Postfix(Player __instance, ItemDrop.ItemData weapon)
        {
            if (!State.InstaShot || !ReferenceEquals(__instance, Player.m_localPlayer)) return;

            if (!__instance.m_attackHold)
            {
                _armed = true; // one shot per press
                return;
            }
            if (!_armed || __instance.m_attackDrawTime <= 0f) return; // draw hasn't started yet (or we already fired)

            _armed = false;
            __instance.m_attackDrawTime = 999f; // percentage patch also returns 1, this makes it explicit
            __instance.StartAttack(null, false);
            __instance.m_attackDrawTime = 0f;

            var anim = weapon?.m_shared?.m_attack?.m_drawAnimationState;
            if (!string.IsNullOrEmpty(anim)) __instance.m_zanim.SetBool(anim, false);
        }
    }

    [HarmonyPatch(typeof(Projectile), nameof(Projectile.Setup))]
    internal static class HitscanPatch
    {
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

            var dir = velocity.normalized;
            var origin = __instance.transform.position;

            // Nearest hit along the flight line that isn't the shooter.
            RaycastHit? best = null;
            foreach (var h in Physics.RaycastAll(origin, dir, 400f, Mask))
            {
                if (h.collider == null) continue;
                if (h.collider.transform.root == owner.transform.root) continue;
                if (best == null || h.distance < best.Value.distance) best = h;
            }

            // Straight line, no drop, fast. Projectile.FixedUpdate raycasts from last to next
            // position each step, so a short hop still registers the hit normally.
            __instance.m_gravity = 0f;
            __instance.m_drag = 0f;
            if (best.HasValue)
            {
                var hit = best.Value;
                __instance.transform.position = hit.point - dir * 0.6f;
                __instance.m_vel = dir * Mathf.Max(velocity.magnitude, 60f);
            }
            else
            {
                __instance.m_vel = dir * Mathf.Max(velocity.magnitude, 120f);
            }
        }
    }
}
