using HarmonyLib;
using MjolnirMenu.Core;
using UnityEngine;

namespace MjolnirMenu.Patches
{
    [HarmonyPatch(typeof(Character))]
    internal static class CharacterPatches
    {
        /// <summary>
        /// Incoming: god mode safety net + no fall damage for the local player.
        /// Outgoing: melee/ranged damage multiplier when the local player is the attacker
        /// (runs before the hit is sent to the target's owner, so it works in multiplayer too).
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(nameof(Character.Damage))]
        private static bool Damage_Prefix(Character __instance, HitData hit)
        {
            var local = Player.m_localPlayer;
            if (local == null) return true;

            if (ReferenceEquals(__instance, local))
            {
                if (State.GodMode) return false;
                if (State.NoFallDamage && hit.m_hitType == HitData.HitType.Fall) return false;
                return true;
            }

            if (State.DamageHack && ReferenceEquals(hit.GetAttacker(), local))
                hit.m_damage.Modify(hit.m_ranged ? State.RangedMultiplier : State.MeleeMultiplier);

            return true;
        }

        /// <summary>
        /// Vanilla only lets you jump in swim depth within 0.25s of hitting the water
        /// (m_hitWorldTime). Zeroing that timer makes every water jump legal.
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(nameof(Character.Jump))]
        private static void Jump_Prefix(Character __instance)
        {
            if (State.WaterJump && ReferenceEquals(__instance, Player.m_localPlayer) && __instance.InLiquidSwimDepth())
                __instance.m_hitWorldTime = 0f;
        }

        /// <summary>Walk-on-water / seabed: pretend we are never swimming so ground movement logic runs.</summary>
        [HarmonyPrefix, HarmonyPatch(nameof(Character.IsSwimming))]
        private static bool IsSwimming_Prefix(Character __instance, ref bool __result)
        {
            if (!(State.WalkOnWater || State.SeabedWalk) || State.Fly) return true;
            if (!ReferenceEquals(__instance, Player.m_localPlayer)) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Humanoid))]
    internal static class HumanoidPatches
    {
        /// <summary>Weapons/armor/tools never lose durability (belt-and-braces with the per-frame refill).</summary>
        [HarmonyPrefix, HarmonyPatch("DrainEquipedItemDurability")]
        private static bool DrainEquipedItemDurability_Prefix(Humanoid __instance)
            => !(State.InfiniteDurability && ReferenceEquals(__instance, Player.m_localPlayer));

        /// <summary>
        /// Walk on water: after the physics step, snap the local player to the liquid surface and
        /// convince the ground checks (m_lastGroundTouch &lt; 0.2 == IsOnGround) that they are standing.
        /// Seabed walk: just let gravity win; IsSwimming is already forced false so they sink and walk.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(nameof(Humanoid.CustomFixedUpdate))]
        private static void CustomFixedUpdate_Postfix(Humanoid __instance)
        {
            if (State.Fly || !ReferenceEquals(__instance, Player.m_localPlayer)) return;
            if (!(State.WalkOnWater || State.SeabedWalk)) return;

            float depth = __instance.InLiquidDepth();
            if (depth <= 0f) return; // not in liquid at all

            // Both modes: no swim state, so no swim stamina drain / swim animation.
            __instance.m_swimTimer = 999f;

            if (!State.WalkOnWater) return;

            float surface = __instance.GetLiquidLevel();
            var pos = __instance.transform.position;
            if (pos.y < surface)
            {
                pos.y = surface + 0.02f;
                __instance.transform.position = pos;
            }

            var body = __instance.m_body;
            if (body != null)
            {
                var v = body.linearVelocity;
                if (v.y < 0f) { v.y = 0f; body.linearVelocity = v; }
            }

            __instance.m_lastGroundTouch = 0f;
            __instance.m_groundContact = true;
        }
    }

    [HarmonyPatch(typeof(CharacterAnimEvent))]
    internal static class CharacterAnimEventPatches
    {
        // Attack animations drive their own pacing through Speed() animation events; we scale
        // whatever the clip asked for. Base is reset when the attack ends (CustomFixedUpdate sets speed=1).
        private static float _baseSpeed = 1f;
        private static float _emoteGraceUntil;

        private static bool IsLocalAttacking(CharacterAnimEvent ev)
        {
            var c = ev.m_character;
            return State.AttackSpeedHack && c != null && ReferenceEquals(c, Player.m_localPlayer) && c.InAttack();
        }

        [HarmonyPostfix, HarmonyPatch("Speed")]
        private static void Speed_Postfix(CharacterAnimEvent __instance, float speedScale)
        {
            if (!ReferenceEquals(__instance.m_character, Player.m_localPlayer)) return;
            _baseSpeed = speedScale;
            if (IsLocalAttacking(__instance))
                __instance.m_animator.speed = speedScale * State.AttackSpeedMultiplier;
        }

        [HarmonyPostfix, HarmonyPatch(nameof(CharacterAnimEvent.CustomFixedUpdate))]
        private static void CustomFixedUpdate_Postfix(CharacterAnimEvent __instance)
        {
            if (!ReferenceEquals(__instance.m_character, Player.m_localPlayer)) return;
            if (IsLocalAttacking(__instance))
            {
                float target = Mathf.Max(0.05f, _baseSpeed) * State.AttackSpeedMultiplier;
                if (!Mathf.Approximately(__instance.m_animator.speed, target))
                    __instance.m_animator.speed = target;
                return;
            }

            _baseSpeed = 1f;

            // Sit / stand: emote state while sitting, then a short grace so the stand-up transition
            // (no longer tagged emote) is sped up too before the game resets speed to 1.
            if (State.EmoteSpeedHack)
            {
                var c = __instance.m_character;
                bool inEmote = c.InEmote() || c.IsSitting();
                if (inEmote) _emoteGraceUntil = Time.time + 1.5f;
                if (inEmote || Time.time < _emoteGraceUntil)
                {
                    if (!Mathf.Approximately(__instance.m_animator.speed, State.EmoteSpeedMultiplier))
                        __instance.m_animator.speed = State.EmoteSpeedMultiplier;
                }
            }
        }
    }
}
