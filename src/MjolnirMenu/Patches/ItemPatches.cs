using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MjolnirMenu.Core;
using MjolnirMenu.Features;
using UnityEngine;

namespace MjolnirMenu.Patches
{
    /// <summary>
    /// Infinite items: instead of guessing which Inventory.Remove* call is a "consume" and which is a
    /// drag/drop, we wrap the known consumers (ammo, food, fuel, ore, feeding, ...) in a scope flag and
    /// only refuse removals from the local player's inventory while one of them is on the stack.
    /// </summary>
    [HarmonyPatch]
    internal static class ConsumeScopePatches
    {
        private static int _depth;
        internal static bool Active => _depth > 0;

        // Parallel arrays on purpose: System.ValueTuple is not available in the game's Mono profile.
        private static readonly System.Type[] ConsumerTypes =
        {
            typeof(Attack), typeof(Attack), typeof(Player),
            typeof(Fireplace), typeof(Fireplace),
            typeof(Smelter), typeof(Smelter),
            typeof(CookingStation), typeof(CookingStation),
            typeof(Fermenter), typeof(Tameable), typeof(Turret),
            typeof(ShieldGenerator), typeof(Catapult), typeof(Door),
        };

        private static readonly string[] ConsumerMethods =
        {
            "UseAmmo", "ConsumeItem", "ConsumeItem",
            "UseItem", "Interact",
            "OnAddOre", "OnAddFuel",
            "CookItem", "OnAddFuelSwitch",
            "AddItem", "UseItem", "UseItem",
            "OnAddFuel", "OnLoadPointUse", "UseItem",
        };

        private static IEnumerable<MethodBase> TargetMethods()
        {
            for (int i = 0; i < ConsumerTypes.Length; i++)
            {
                foreach (var m in AccessTools.GetDeclaredMethods(ConsumerTypes[i]))
                    if (m.Name == ConsumerMethods[i] && !m.IsAbstract) yield return m;
            }
        }

        private static void Prefix() => _depth++;
        private static void Postfix() => _depth = Mathf.Max(0, _depth - 1);
    }

    [HarmonyPatch(typeof(Inventory))]
    internal static class InventoryRemovePatches
    {
        private static bool Block(Inventory inv)
        {
            if (!State.InfiniteItems || !ConsumeScopePatches.Active) return false;
            var p = Player.m_localPlayer;
            return p != null && ReferenceEquals(inv, p.GetInventory());
        }

        [HarmonyPrefix, HarmonyPatch(nameof(Inventory.RemoveOneItem))]
        private static bool RemoveOneItem_Prefix(Inventory __instance, ref bool __result)
        {
            if (!Block(__instance)) return true;
            __result = true;
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(nameof(Inventory.RemoveItem), typeof(ItemDrop.ItemData))]
        private static bool RemoveItem_Prefix(Inventory __instance, ref bool __result)
        {
            if (!Block(__instance)) return true;
            __result = true;
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(nameof(Inventory.RemoveItem), typeof(ItemDrop.ItemData), typeof(int))]
        private static bool RemoveItemAmount_Prefix(Inventory __instance, ref bool __result)
        {
            if (!Block(__instance)) return true;
            __result = true;
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(nameof(Inventory.RemoveItem), typeof(string), typeof(int), typeof(int), typeof(bool))]
        private static bool RemoveItemByName_Prefix(Inventory __instance)
            => !Block(__instance);
    }

    /// <summary>
    /// Duplicate-on-interact: hand the player a copy of what they interacted with and leave the
    /// source untouched. Walk-over pickup is disabled while this is on so it can't double-collect.
    /// </summary>
    internal static class Dupe
    {
        internal static bool On(Humanoid who)
            => State.InfiniteInteract && who != null && ReferenceEquals(who, Player.m_localPlayer);

        internal static void Give(Humanoid who, GameObject prefab, int amount, string? label = null)
        {
            if (prefab == null || amount <= 0) return;
            var drop = prefab.GetComponent<ItemDrop>();
            if (drop == null) return;
            if (!who.GetInventory().AddItem(prefab, amount))
            {
                who.Message(MessageHud.MessageType.Center, "$msg_noroom", 0, null, false);
                return;
            }
            who.Message(MessageHud.MessageType.TopLeft,
                "$msg_added " + (label ?? drop.m_itemData.m_shared.m_name), amount, drop.m_itemData.GetIcon(), false);
        }
    }

    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Interact))]
    internal static class ItemDropInteractPatch
    {
        private static bool Prefix(ItemDrop __instance, Humanoid character, bool repeat, ref bool __result)
        {
            if (repeat || !Dupe.On(character)) return true;
            var data = __instance.m_itemData;
            if (data == null) return true;
            var clone = data.Clone();
            clone.m_stack = data.m_stack;
            if (character.GetInventory().AddItem(clone))
                character.Message(MessageHud.MessageType.TopLeft, "$msg_added " + data.m_shared.m_name, data.m_stack, data.GetIcon(), false);
            else
                character.Message(MessageHud.MessageType.Center, "$msg_noroom", 0, null, false);
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(Pickable), nameof(Pickable.Interact))]
    internal static class PickableInteractPatch
    {
        private static bool Prefix(Pickable __instance, Humanoid character, bool repeat, ref bool __result)
        {
            if (repeat || !Dupe.On(character)) return true;
            if (__instance.m_picked || __instance.m_itemPrefab == null) return true;
            Dupe.Give(character, __instance.m_itemPrefab, Mathf.Max(1, __instance.m_amount));
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(PickableItem), nameof(PickableItem.Interact))]
    internal static class PickableItemInteractPatch
    {
        private static bool Prefix(PickableItem __instance, Humanoid character, bool repeat, ref bool __result)
        {
            if (repeat || !Dupe.On(character)) return true;
            if (__instance.m_picked || __instance.m_itemPrefab == null) return true;
            Dupe.Give(character, __instance.m_itemPrefab.gameObject, Mathf.Max(1, __instance.m_stack));
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(Beehive), nameof(Beehive.Interact))]
    internal static class BeehiveInteractPatch
    {
        private static bool Prefix(Beehive __instance, Humanoid character, bool repeat, ref bool __result)
        {
            if (repeat || !Dupe.On(character)) return true;
            int level = __instance.GetHoneyLevel();
            if (level <= 0 || __instance.m_honeyItem == null) return true; // let vanilla print its "empty" message
            Dupe.Give(character, __instance.m_honeyItem.gameObject, level);
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(Player), "AutoPickup")]
    internal static class AutoPickupPatch
    {
        private static bool Prefix(Player __instance)
            => !(State.InfiniteInteract && ReferenceEquals(__instance, Player.m_localPlayer));
    }
}
