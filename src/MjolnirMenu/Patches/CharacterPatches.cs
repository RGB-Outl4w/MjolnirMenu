using HarmonyLib;
using MjolnirMenu.Core;

namespace MjolnirMenu.Patches
{
    [HarmonyPatch(typeof(Character))]
    internal static class CharacterPatches
    {
        /// <summary>God mode safety net + no fall damage. Only ever touches the local player.</summary>
        [HarmonyPrefix, HarmonyPatch(nameof(Character.Damage))]
        private static bool Damage_Prefix(Character __instance, HitData hit)
        {
            if (!ReferenceEquals(__instance, Player.m_localPlayer)) return true;
            if (State.GodMode) return false;
            if (State.NoFallDamage && hit.m_hitType == HitData.HitType.Fall) return false;
            return true;
        }
    }
}
