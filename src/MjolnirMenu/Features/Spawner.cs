using System;
using System.Collections.Generic;
using MjolnirMenu.Core;
using UnityEngine;

namespace MjolnirMenu.Features
{
    /// <summary>Prefab catalog + spawn helpers. Catalog is built once ObjectDB/ZNetScene exist.</summary>
    public static class Spawner
    {
        public sealed class Entry
        {
            public string PrefabName = "";
            public string DisplayName = "";
            public bool IsCreature;
            public bool IsBoss;
            /// <summary>Has an icon and a real item type: safe to hold in a player inventory.</summary>
            public bool InventorySafe;
        }

        public static readonly List<Entry> Items = new List<Entry>();
        public static readonly List<Entry> Creatures = new List<Entry>();

        private static bool _built;

        public static bool EnsureCatalog()
        {
            if (_built) return true;
            var db = ObjectDB.instance;
            var scene = ZNetScene.instance;
            if (db == null || scene == null || db.m_items == null || db.m_items.Count == 0) return false;

            Items.Clear();
            Creatures.Clear();

            foreach (var go in db.m_items)
            {
                if (go == null) continue;
                var drop = go.GetComponent<ItemDrop>();
                if (drop == null || drop.m_itemData?.m_shared == null) continue;
                var shared = drop.m_itemData.m_shared;
                bool hasIcon = shared.m_icons != null && shared.m_icons.Length > 0 && shared.m_icons[0] != null;
                Items.Add(new Entry
                {
                    PrefabName = go.name,
                    DisplayName = Localization.instance.Localize(shared.m_name),
                    InventorySafe = hasIcon && shared.m_itemType != ItemDrop.ItemData.ItemType.None && shared.m_maxStackSize >= 1,
                });
            }

            foreach (var go in scene.m_prefabs)
            {
                if (go == null) continue;
                var ch = go.GetComponent<Character>();
                if (ch == null || ch is Player) continue;
                Creatures.Add(new Entry
                {
                    PrefabName = go.name,
                    DisplayName = Localization.instance.Localize(ch.m_name),
                    IsCreature = true,
                    IsBoss = ch.m_boss,
                });
            }

            Items.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            Creatures.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            _built = true;
            Plugin.Log.LogInfo($"Spawner catalog: {Items.Count} items, {Creatures.Count} creatures");
            return true;
        }

        public static void Invalidate() => _built = false;

        public static bool Matches(Entry e, string filter)
        {
            if (string.IsNullOrEmpty(filter)) return true;
            return e.DisplayName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                || e.PrefabName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>Put an item straight into the local player's inventory; overflow drops at their feet.</summary>
        public static void GiveItem(string prefabName, int stack, int quality)
        {
            var p = Player.m_localPlayer;
            if (p == null) return;
            stack = Mathf.Max(1, stack);
            quality = Mathf.Clamp(quality, 1, 10);

            var prefab = ObjectDB.instance?.GetItemPrefab(prefabName);
            if (prefab == null)
            {
                Hotkeys.Notify($"Unknown item '{prefabName}'");
                return;
            }

            var drop = prefab.GetComponent<ItemDrop>();
            int maxStack = Mathf.Max(1, drop.m_itemData.m_shared.m_maxStackSize);
            int remaining = stack;
            while (remaining > 0)
            {
                int n = Mathf.Min(remaining, maxStack);
                var item = p.GetInventory().AddItem(prefabName, n, quality, 0, p.GetPlayerID(), p.GetPlayerName(), false, false);
                if (item == null)
                {
                    DropItem(prefab, remaining, quality);
                    break;
                }
                remaining -= n;
            }
            Hotkeys.Notify($"+{stack} {Localization.instance.Localize(drop.m_itemData.m_shared.m_name)}");
        }

        private static void DropItem(GameObject prefab, int stack, int quality)
        {
            var p = Player.m_localPlayer;
            if (p == null) return;
            var pos = p.transform.position + p.transform.forward * 1.5f + Vector3.up * 1f;
            var go = UnityEngine.Object.Instantiate(prefab, pos, Quaternion.identity);
            var drop = go.GetComponent<ItemDrop>();
            if (drop != null)
            {
                drop.SetStack(stack);
                drop.SetQuality(quality);
            }
        }

        public static void SpawnCreature(string prefabName, int level, int count, bool tamed)
        {
            var p = Player.m_localPlayer;
            if (p == null) return;
            var prefab = ZNetScene.instance?.GetPrefab(prefabName);
            if (prefab == null)
            {
                Hotkeys.Notify($"Unknown prefab '{prefabName}'");
                return;
            }

            count = Mathf.Clamp(count, 1, 50);
            level = Mathf.Clamp(level, 1, 10);
            for (int i = 0; i < count; i++)
            {
                var offset = UnityEngine.Random.insideUnitCircle * (1.5f + count * 0.2f);
                var pos = p.transform.position + p.transform.forward * 4f + new Vector3(offset.x, 0.5f, offset.y);
                var go = UnityEngine.Object.Instantiate(prefab, pos, Quaternion.LookRotation(-p.transform.forward));
                var ch = go.GetComponent<Character>();
                if (ch != null && level > 1) ch.SetLevel(level);
                if (tamed)
                {
                    var t = go.GetComponent<Tameable>();
                    if (t != null) t.Tame();
                }
            }
            Hotkeys.Notify($"Spawned {count}x {prefabName} (lvl {level})");
        }

        /// <summary>Kill every non-player character within <paramref name="radius"/> meters.</summary>
        public static int KillNearby(float radius, bool includeTamed)
        {
            var p = Player.m_localPlayer;
            if (p == null) return 0;
            int killed = 0;
            var origin = p.transform.position;
            // Copy: Damage() can remove entries from the live list mid-loop.
            var all = new List<Character>(Character.GetAllCharacters());
            foreach (var c in all)
            {
                if (c == null || c.IsPlayer() || c.IsDead()) continue;
                if (!includeTamed && c.IsTamed()) continue;
                if (Vector3.Distance(origin, c.transform.position) > radius) continue;

                var hit = new HitData
                {
                    m_point = c.GetCenterPoint(),
                    m_hitType = HitData.HitType.PlayerHit,
                };
                hit.m_damage.m_damage = 1e9f;
                c.Damage(hit);
                killed++;
            }
            Hotkeys.Notify($"Killed {killed}");
            return killed;
        }
    }
}
