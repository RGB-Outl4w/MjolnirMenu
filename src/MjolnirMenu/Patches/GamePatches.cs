using System.Collections.Generic;
using HarmonyLib;
using MjolnirMenu.Core;
using UnityEngine;

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

    /// <summary>
    /// Loot for high-level creatures. Vanilla multiplies level-scaled drops by (int)Pow(2, level - 1), which
    /// overflows from level 32 and falls back to x1, then caps every entry at 100. Above 3 stars we run vanilla
    /// as level 1 (for its chance rolls and amounts) and scale the level-scaled entries ourselves.
    /// </summary>
    [HarmonyPatch(typeof(CharacterDrop))]
    internal static class LevelLootPatches
    {
        private const int VanillaMaxLevel = 3;
        private const int MaxObjectsPerEntry = 200;

        /// <summary>Vanilla curve up to level 10 (x512), linear after that: level 1000 = x51 200.</summary>
        internal static float Multiplier(int level)
            => level <= 10 ? Mathf.Pow(2f, level - 1) : 512f * level / 10f;

        [HarmonyPrefix, HarmonyPatch(nameof(CharacterDrop.GenerateDropList))]
        private static void Generate_Prefix(CharacterDrop __instance, out int __state)
        {
            __state = 0;
            var ch = __instance.m_character;
            if (ch == null || ch.m_level <= VanillaMaxLevel) return;
            __state = ch.m_level;
            ch.m_level = 1;
        }

        [HarmonyPostfix, HarmonyPatch(nameof(CharacterDrop.GenerateDropList))]
        private static void Generate_Postfix(CharacterDrop __instance, int __state, List<KeyValuePair<GameObject, int>> __result)
        {
            if (__state == 0) return;
            __instance.m_character.m_level = __state;
            float mul = Multiplier(__state);
            foreach (var drop in __instance.m_drops)
            {
                if (drop.m_prefab == null || !drop.m_levelMultiplier || drop.m_levelMultiplierNotAmount) continue;
                int i = __result.FindIndex(kv => kv.Key == drop.m_prefab);
                // Vanilla also scales these drops' chance by the level factor: at this multiplier that's a sure drop.
                int amount = i >= 0 ? __result[i].Value
                    : Random.value < drop.m_chance * mul ? Random.Range(drop.m_amountMin, drop.m_amountMax + 1) : 0;
                if (amount <= 0) continue;
                var kv2 = new KeyValuePair<GameObject, int>(drop.m_prefab, (int)Mathf.Min(amount * mul, int.MaxValue / 2f));
                if (i >= 0) __result[i] = kv2; else __result.Add(kv2);
            }
        }

        /// <summary>
        /// Vanilla spawns one object per unit. Big amounts are dropped as full stacks instead.
        /// ponytail: capped at 200 piles per entry so a level-1000 kill can't freeze the game; raise if wanted.
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(nameof(CharacterDrop.DropItems))]
        private static bool DropItems_Prefix(List<KeyValuePair<GameObject, int>> drops, Vector3 centerPos, float dropArea, bool cheated)
        {
            if (!drops.Exists(d => d.Value > 100)) return true;
            foreach (var d in drops)
            {
                var item = d.Key.GetComponent<ItemDrop>();
                int stack = item != null ? Mathf.Max(1, item.m_itemData.m_shared.m_maxStackSize) : 1;
                int piles = Mathf.Min(MaxObjectsPerEntry, (d.Value + stack - 1) / stack);
                int left = d.Value;
                for (int n = 0; n < piles && left > 0; n++)
                {
                    var pos = centerPos + Quaternion.Euler(0f, Random.Range(0, 360), 0f) * (Random.insideUnitSphere * dropArea);
                    var go = Object.Instantiate(d.Key, pos, Quaternion.Euler(0f, Random.Range(0, 360), 0f));
                    var drop = go.GetComponent<ItemDrop>();
                    if (drop != null)
                    {
                        drop.SetStack(Mathf.Min(stack, left));
                        drop.m_itemData.m_worldLevel = (byte)Game.m_worldLevel;
                        drop.m_itemData.m_cheated = cheated;
                    }
                    left -= stack;
                    var rb = go.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        var v = Random.insideUnitSphere;
                        if (v.y < 0f) v.y = -v.y;
                        rb.AddForce(v * 5f, ForceMode.VelocityChange);
                    }
                }
            }
            return false;
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
