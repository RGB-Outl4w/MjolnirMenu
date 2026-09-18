using System.Collections.Generic;
using MjolnirMenu.Core;
using UnityEngine;

namespace MjolnirMenu.Features
{
    /// <summary>
    /// Player-side cheats. Game fields we touch (god flag, fly flag, jump force, swim speed,
    /// camera water clamp) are reconciled every frame against <see cref="State"/> so a respawn
    /// or a new Player instance never drifts out of sync, and turning a toggle off restores the original.
    /// </summary>
    public static class PlayerCheats
    {
        private static Player? _trackedPlayer;
        private static float _origJumpForce;
        private static float _origSwimSpeed;
        private static float _origCrouchSpeed;
        /// <summary>The game caps the player grid at 9 rows (Player.SetInventorySize).</summary>
        public const int MaxExtraRows = 5;
        private const string OrigRowsKey = "mjolnir_invrows";

        /// <summary>Armor pieces equipped on top of an occupied slot (multi-equip). Local player only.</summary>
        public static readonly HashSet<ItemDrop.ItemData> ExtraEquipped = new HashSet<ItemDrop.ItemData>();

        private static GameCamera? _trackedCamera;
        private static float _origMinWaterDistance;

        /// <summary>Camera clamp value that effectively lets the camera go under the surface.</summary>
        private const float UnderwaterClamp = -5000f;

        public static void Tick()
        {
            var p = Player.m_localPlayer;
            if (p == null)
            {
                _trackedPlayer = null;
            }
            else
            {
                if (!ReferenceEquals(p, _trackedPlayer))
                {
                    // Fresh Player object (login / respawn): snapshot vanilla values before we scale them.
                    _trackedPlayer = p;
                    _origJumpForce = p.m_jumpForce;
                    _origSwimSpeed = p.m_swimSpeed;
                    _origCrouchSpeed = p.m_crouchSpeed;
                    ExtraEquipped.Clear();
                }

                ApplyGodMode();
                ApplyGhostMode();
                ApplyFly();
                ApplyJump();
                ApplySwimSpeed();
                ApplyCrouchSpeed();
                ApplyInventoryRows(p);
                ApplyMultiEquip(p);

                if (State.InfiniteStamina && p.m_stamina < p.GetMaxStamina())
                    p.m_stamina = p.GetMaxStamina();

                if (State.InfiniteEitr && p.GetMaxEitr() > 0f && p.m_eitr < p.GetMaxEitr())
                    p.m_eitr = p.GetMaxEitr();

                if (State.InfiniteDurability)
                    RefillDurability(p);

                if (State.InstaReload)
                    InstaReload(p);
            }

            ApplyCamera();
        }

        public static void ApplyAll()
        {
            ApplyGodMode();
            ApplyGhostMode();
            ApplyFly();
            ApplyJump();
            ApplySwimSpeed();
            ApplyCrouchSpeed();
            ApplyCamera();
        }

        public static void ApplyGodMode()
        {
            var p = Player.m_localPlayer;
            if (p != null && p.m_godMode != State.GodMode)
                p.SetGodMode(State.GodMode);
        }

        public static void ApplyGhostMode()
        {
            var p = Player.m_localPlayer;
            if (p != null && p.InGhostMode() != State.GhostMode)
                p.SetGhostMode(State.GhostMode);
        }

        public static void ApplyFly()
        {
            var p = Player.m_localPlayer;
            if (p != null && p.m_debugFly != State.Fly)
                p.ToggleDebugFly();
        }

        public static void ApplyJump()
        {
            var p = Player.m_localPlayer;
            if (p == null || !ReferenceEquals(p, _trackedPlayer)) return;
            float target = State.JumpHack ? _origJumpForce * State.JumpMultiplier : _origJumpForce;
            if (!Mathf.Approximately(p.m_jumpForce, target))
                p.m_jumpForce = target;
        }

        public static void ApplySwimSpeed()
        {
            var p = Player.m_localPlayer;
            if (p == null || !ReferenceEquals(p, _trackedPlayer)) return;
            float target = State.SwimSpeedHack ? _origSwimSpeed * State.SwimSpeedMultiplier : _origSwimSpeed;
            if (!Mathf.Approximately(p.m_swimSpeed, target))
                p.m_swimSpeed = target;
        }

        public static void ApplyCrouchSpeed()
        {
            var p = Player.m_localPlayer;
            if (p == null || !ReferenceEquals(p, _trackedPlayer)) return;
            float target = State.CrouchSpeedHack ? _origCrouchSpeed * State.CrouchSpeedMultiplier : _origCrouchSpeed;
            if (!Mathf.Approximately(p.m_crouchSpeed, target))
                p.m_crouchSpeed = target;
        }

        /// <summary>
        /// Rides the game's own inventory-size feature (Player.SetInventorySize / "invrows" key), so the panel
        /// resizes natively and the size survives relog. The vanilla row count is parked in a unique key while
        /// the cheat is on. Shrinking pulls hidden items into free slots; the game drops whatever is left at your feet.
        /// </summary>
        private static void ApplyInventoryRows(Player p)
        {
            bool cheated = p.TryGetUniqueKeyValue(OrigRowsKey, out var s);
            if (!cheated) p.TryGetUniqueKeyValue("invrows", out s);
            if (!int.TryParse(s, out int orig)) orig = 4;
            int extra = Mathf.Clamp(State.ExtraInventoryRows, 0, MaxExtraRows);
            if (extra > 0 && !cheated) p.AddUniqueKeyValue(OrigRowsKey, orig.ToString());
            else if (extra == 0 && cheated) p.RemoveUniqueKeyValue(OrigRowsKey);

            var inv = p.GetInventory();
            int want = Mathf.Min(orig + extra, 9);
            if (want == inv.m_height) return;
            if (want < inv.m_height)
            {
                inv.m_height = want;
                foreach (var item in inv.GetAllItems())
                    if (item.m_gridPos.y >= want)
                    {
                        var slot = inv.FindEmptySlot(true);
                        if (slot.x < 0) break;
                        item.m_gridPos = slot;
                    }
            }
            p.SetInventorySize(want);
        }

        /// <summary>Drop extras that left the inventory; unequip all of them when the toggle goes off.</summary>
        private static void ApplyMultiEquip(Player p)
        {
            if (ExtraEquipped.Count == 0) return;
            var inv = p.GetInventory();
            if (!State.MultiEquip || ExtraEquipped.RemoveWhere(i => !inv.ContainsItem(i)) > 0)
            {
                foreach (var item in new List<ItemDrop.ItemData>(ExtraEquipped)) p.UnequipItem(item, false);
                if (!State.MultiEquip) ExtraEquipped.Clear();
                p.SetupEquipment();
            }
        }

        /// <summary>Strip the 'obtained using cheats' flag from everything currently carried.</summary>
        public static int ClearCheatTags()
        {
            var p = Player.m_localPlayer;
            if (p == null) return 0;
            int n = 0;
            foreach (var item in p.GetInventory().GetAllItems())
            {
                if (item == null || !item.m_cheated) continue;
                item.m_cheated = false;
                n++;
            }
            p.GetInventory().Changed();
            Hotkeys.Notify($"Cleared cheat tag on {n} item(s)");
            return n;
        }

        /// <summary>GameCamera keeps itself m_minWaterDistance above the water line; push that far below to allow diving shots.</summary>
        public static void ApplyCamera()
        {
            var cam = GameCamera.instance;
            if (cam == null)
            {
                _trackedCamera = null;
                return;
            }
            if (!ReferenceEquals(cam, _trackedCamera))
            {
                _trackedCamera = cam;
                _origMinWaterDistance = cam.m_minWaterDistance;
            }
            float target = State.UnderwaterCamera ? UnderwaterClamp : _origMinWaterDistance;
            if (!Mathf.Approximately(cam.m_minWaterDistance, target))
                cam.m_minWaterDistance = target;
        }

        /// <summary>Crossbows: mark the weapon loaded the moment it isn't, skipping the reload action.</summary>
        private static void InstaReload(Player p)
        {
            var w = p.GetCurrentWeapon();
            if (w?.m_shared?.m_attack == null || !w.m_shared.m_attack.m_requiresReload) return;
            if (p.IsWeaponLoaded()) return;
            p.CancelReloadAction();
            p.SetWeaponLoaded(w);
        }

        /// <summary>Tops up every durability-using item in the inventory (weapons, armor, tools).</summary>
        private static void RefillDurability(Player p)
        {
            var inv = p.GetInventory();
            if (inv == null) return;
            foreach (var item in inv.GetAllItems())
            {
                if (item?.m_shared == null || !item.m_shared.m_useDurability) continue;
                float max = item.GetMaxDurability();
                if (item.m_durability < max) item.m_durability = max;
            }
        }

        public static void HealFull()
        {
            var p = Player.m_localPlayer;
            if (p == null) return;
            p.SetHealth(p.GetMaxHealth());
            p.AddStamina(p.GetMaxStamina());
            if (p.GetMaxEitr() > 0f) p.AddEitr(p.GetMaxEitr());
        }
    }
}
