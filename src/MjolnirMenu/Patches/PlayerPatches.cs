using HarmonyLib;
using MjolnirMenu.Core;
using MjolnirMenu.Features;

namespace MjolnirMenu.Patches
{
    /// <summary>Player-level hooks. Player overrides the Character virtuals, so patch Player, not Character.</summary>
    [HarmonyPatch(typeof(Player))]
    internal static class PlayerPatches
    {
        [HarmonyPrefix, HarmonyPatch(nameof(Player.UseStamina))]
        private static bool UseStamina_Prefix(Player __instance)
        {
            if (!ReferenceEquals(__instance, Player.m_localPlayer)) return true;
            if (State.InfiniteStamina) return false;
            if (State.CrouchInfiniteStamina && __instance.IsCrouching()) return false;
            return true;
        }

        /// <summary>
        /// Teleport (portal or ours) waits on m_teleportTimer: &gt;2s before the move, &gt;8s for portals
        /// before landing. Feeding a scaled dt shortens the whole vortex sequence.
        /// </summary>
        [HarmonyPrefix, HarmonyPatch("UpdateTeleport")]
        private static void UpdateTeleport_Prefix(Player __instance, ref float dt)
        {
            if (State.FastTeleport && __instance.m_teleporting && ReferenceEquals(__instance, Player.m_localPlayer))
                dt *= State.TeleportSpeed;
        }

        [HarmonyPrefix, HarmonyPatch(nameof(Player.UseEitr))]
        private static bool UseEitr_Prefix(Player __instance)
            => !(State.InfiniteEitr && ReferenceEquals(__instance, Player.m_localPlayer));

        [HarmonyPostfix, HarmonyPatch(nameof(Player.GetMaxCarryWeight))]
        private static void GetMaxCarryWeight_Postfix(Player __instance, ref float __result)
        {
            if (State.InfiniteCarryWeight && ReferenceEquals(__instance, Player.m_localPlayer))
                __result = 100000f;
        }

        [HarmonyPostfix, HarmonyPatch("GetRunSpeedFactor")]
        private static void GetRunSpeedFactor_Postfix(Player __instance, ref float __result)
        {
            if (State.SpeedHack && ReferenceEquals(__instance, Player.m_localPlayer))
                __result *= State.SpeedMultiplier;
        }

        [HarmonyPostfix, HarmonyPatch("GetJogSpeedFactor")]
        private static void GetJogSpeedFactor_Postfix(Player __instance, ref float __result)
        {
            if (State.SpeedHack && ReferenceEquals(__instance, Player.m_localPlayer))
                __result *= State.SpeedMultiplier;
        }

        // Free craft: skip the resource check (only for the actual craft, not recipe discovery) and the consume step.
        [HarmonyPrefix, HarmonyPatch(nameof(Player.HaveRequirements), typeof(Recipe), typeof(bool), typeof(int), typeof(int))]
        private static bool HaveRequirements_Prefix(Player __instance, bool discover, ref bool __result)
        {
            if (!State.FreeCraft || discover || !ReferenceEquals(__instance, Player.m_localPlayer)) return true;
            __result = true;
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(nameof(Player.ConsumeResources))]
        private static bool ConsumeResources_Prefix(Player __instance)
            => !(State.FreeCraft && ReferenceEquals(__instance, Player.m_localPlayer));

        // Menu open => game ignores gameplay keys/mouse so typing in the search box doesn't swing an axe.
        [HarmonyPostfix, HarmonyPatch("TakeInput")]
        private static void TakeInput_Postfix(ref bool __result)
        {
            if (State.MenuOpen) __result = false;
        }

        // Player object goes away on logout/death: clear tracked snapshots so the next instance is re-captured.
        [HarmonyPrefix, HarmonyPatch("OnDestroy")]
        private static void OnDestroy_Prefix(Player __instance)
        {
            if (ReferenceEquals(__instance, Player.m_localPlayer))
            {
                Spawner.Invalidate();
                Effects.Invalidate();
                PlayerCheats.ExtraEquipped.Clear();
            }
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsTeleportable))]
    internal static class TeleportablePatch
    {
        /// <summary>Portals refuse players carrying non-teleportable items (ore, eggs...). Report clean.</summary>
        private static void Postfix(Humanoid __instance, ref bool __result)
        {
            if (State.PortalAnyItem && ReferenceEquals(__instance, Player.m_localPlayer)) __result = true;
        }
    }

    /// <summary>
    /// Multi-equip: equipping armor into an occupied slot marks it equipped and parks it in
    /// <see cref="PlayerCheats.ExtraEquipped"/> instead of swapping. Armor value and equip status effects
    /// of extras are added on top; the model only shows the slot item (VisEquipment has one per type).
    /// </summary>
    [HarmonyPatch(typeof(Humanoid))]
    internal static class MultiEquipPatches
    {
        private static bool Local(Humanoid h) => ReferenceEquals(h, Player.m_localPlayer);

        private static ItemDrop.ItemData? SlotOf(Humanoid h, ItemDrop.ItemData i) => i.m_shared.m_itemType switch
        {
            ItemDrop.ItemData.ItemType.Helmet => h.m_helmetItem,
            ItemDrop.ItemData.ItemType.Chest => h.m_chestItem,
            ItemDrop.ItemData.ItemType.Legs => h.m_legItem,
            ItemDrop.ItemData.ItemType.Shoulder => h.m_shoulderItem,
            ItemDrop.ItemData.ItemType.Utility => h.m_utilityItem,
            _ => null,
        };

        [HarmonyPrefix, HarmonyPatch(nameof(Humanoid.EquipItem))]
        private static bool EquipItem_Prefix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result)
        {
            if (!State.MultiEquip || !Local(__instance) || item == null || PlayerCheats.ExtraEquipped.Contains(item)) return true;
            var slot = SlotOf(__instance, item);
            if (slot == null || ReferenceEquals(slot, item) || !__instance.m_inventory.ContainsItem(item)) return true;
            item.m_equipped = true;
            PlayerCheats.ExtraEquipped.Add(item);
            __instance.SetupEquipment();
            __result = true;
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(nameof(Humanoid.UnequipItem))]
        private static bool UnequipItem_Prefix(Humanoid __instance, ItemDrop.ItemData item)
        {
            if (item == null || !PlayerCheats.ExtraEquipped.Remove(item)) return true;
            item.m_equipped = false;
            __instance.SetupEquipment();
            return false;
        }

        [HarmonyPostfix, HarmonyPatch(nameof(Humanoid.IsItemEquiped))]
        private static void IsItemEquiped_Postfix(ItemDrop.ItemData item, ref bool __result)
        {
            if (!__result && item != null && PlayerCheats.ExtraEquipped.Contains(item)) __result = true;
        }

        [HarmonyPostfix, HarmonyPatch("UpdateEquipmentStatusEffects")]
        private static void StatusEffects_Postfix(Humanoid __instance)
        {
            if (!Local(__instance)) return;
            foreach (var e in PlayerCheats.ExtraEquipped)
            {
                var se = e.m_shared.m_equipStatusEffect;
                if (se != null && __instance.m_equipmentStatusEffects.Add(se))
                    __instance.m_seman.AddStatusEffect(se, false, 0, 0f, -1);
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetBodyArmor))]
    internal static class BodyArmorPatch
    {
        private static void Postfix(Player __instance, ref float __result)
        {
            if (!ReferenceEquals(__instance, Player.m_localPlayer)) return;
            foreach (var e in PlayerCheats.ExtraEquipped) __result += e.GetArmor();
        }
    }

    [HarmonyPatch(typeof(PlayerController))]
    internal static class PlayerControllerPatches
    {
        /// <summary>Second input gate: movement and mouse-look come through here.</summary>
        [HarmonyPostfix, HarmonyPatch("TakeInput")]
        private static void TakeInput_Postfix(ref bool __result)
        {
            if (State.MenuOpen) __result = false;
        }
    }

    [HarmonyPatch(typeof(PlayerProfile))]
    internal static class PlayerProfilePatches
    {
        /// <summary>
        /// The game tags loot/crafts as cheated whenever the player dealt damage in god/ghost/fly mode
        /// (Character.ApplyDamage sets the player ZDO's 'cheated' flag). bypasscheatchecks is its own
        /// off switch; we report it as set.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch("s_bypassCheatChecks", MethodType.Getter)]
        private static void BypassCheatChecks_Postfix(ref bool __result)
        {
            if (State.HideCheatTags) __result = true;
        }
    }
}
