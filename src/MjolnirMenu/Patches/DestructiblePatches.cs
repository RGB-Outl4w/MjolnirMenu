using HarmonyLib;
using MjolnirMenu.Core;

namespace MjolnirMenu.Patches
{
    /// <summary>
    /// Per-category damage multipliers for non-character targets. Each Damage(HitData) runs on the
    /// attacker's client before the hit is forwarded to the owner, so scaling here works everywhere.
    /// </summary>
    internal static class DestructibleDamage
    {
        internal static bool LocalIsAttacker(HitData hit)
        {
            var local = Player.m_localPlayer;
            return State.DamageHack && local != null && ReferenceEquals(hit.GetAttacker(), local);
        }
    }

    [HarmonyPatch(typeof(TreeBase), nameof(TreeBase.Damage))]
    internal static class TreeBaseDamagePatch
    {
        private static void Prefix(HitData hit)
        {
            if (DestructibleDamage.LocalIsAttacker(hit)) hit.m_damage.Modify(State.TreeMultiplier);
        }
    }

    [HarmonyPatch(typeof(TreeLog), nameof(TreeLog.Damage))]
    internal static class TreeLogDamagePatch
    {
        private static void Prefix(HitData hit)
        {
            if (DestructibleDamage.LocalIsAttacker(hit)) hit.m_damage.Modify(State.TreeMultiplier);
        }
    }

    [HarmonyPatch(typeof(MineRock), nameof(MineRock.Damage))]
    internal static class MineRockDamagePatch
    {
        private static void Prefix(HitData hit)
        {
            if (DestructibleDamage.LocalIsAttacker(hit)) hit.m_damage.Modify(State.RockMultiplier);
        }
    }

    [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.Damage))]
    internal static class MineRock5DamagePatch
    {
        private static void Prefix(HitData hit)
        {
            if (DestructibleDamage.LocalIsAttacker(hit)) hit.m_damage.Modify(State.RockMultiplier);
        }
    }

    /// <summary>Build pieces: player-placed vs. world-generated (ruins, dungeons) decided by Piece.IsPlacedByPlayer.</summary>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Damage))]
    internal static class WearNTearDamagePatch
    {
        private static void Prefix(WearNTear __instance, HitData hit)
        {
            if (!DestructibleDamage.LocalIsAttacker(hit)) return;
            var piece = __instance.m_piece != null ? __instance.m_piece : __instance.GetComponent<Piece>();
            bool player = piece != null && piece.IsPlacedByPlayer();
            hit.m_damage.Modify(player ? State.PlayerStructureMultiplier : State.WorldStructureMultiplier);
        }
    }

    /// <summary>Generic destructibles (boulders, stumps, bushes, location props) count as world structures.</summary>
    [HarmonyPatch(typeof(Destructible), nameof(Destructible.Damage))]
    internal static class DestructibleDamagePatch
    {
        private static void Prefix(HitData hit)
        {
            if (DestructibleDamage.LocalIsAttacker(hit)) hit.m_damage.Modify(State.WorldStructureMultiplier);
        }
    }
}
