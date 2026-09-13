using System.Collections.Generic;
using MjolnirMenu.Core;
using UnityEngine;

namespace MjolnirMenu.Features
{
    /// <summary>
    /// Guardian powers (boss "ultimates") and the status-effect manager. Powers are just
    /// StatusEffects named GP_*; the player only remembers the name of the selected one.
    /// </summary>
    public static class Effects
    {
        /// <summary>Name hashes of active effects whose timer we pin to 0 every frame.</summary>
        public static readonly HashSet<int> Frozen = new HashSet<int>();

        private static List<StatusEffect>? _powers;
        private static List<StatusEffect>? _catalog;

        public static List<StatusEffect> GuardianPowers()
        {
            if (_powers != null) return _powers;
            var db = ObjectDB.instance;
            if (db == null || db.m_StatusEffects == null || db.m_StatusEffects.Count == 0) return new List<StatusEffect>();
            _powers = new List<StatusEffect>();
            foreach (var se in db.m_StatusEffects)
                if (se != null && se.name.StartsWith("GP_")) _powers.Add(se);
            return _powers;
        }

        public static List<StatusEffect> Catalog()
        {
            if (_catalog != null) return _catalog;
            var db = ObjectDB.instance;
            if (db == null || db.m_StatusEffects == null || db.m_StatusEffects.Count == 0) return new List<StatusEffect>();
            _catalog = new List<StatusEffect>();
            foreach (var se in db.m_StatusEffects)
                if (se != null) _catalog.Add(se);
            _catalog.Sort((a, b) => string.Compare(Label(a), Label(b), System.StringComparison.OrdinalIgnoreCase));
            return _catalog;
        }

        public static void Invalidate()
        {
            _powers = null;
            _catalog = null;
            Frozen.Clear();
        }

        public static string Label(StatusEffect se)
        {
            string loc = string.IsNullOrEmpty(se.m_name) ? se.name : Localization.instance.Localize(se.m_name);
            return loc.StartsWith("[") || string.IsNullOrEmpty(loc) ? se.name : loc;
        }

        public static string CurrentPower() => Player.m_localPlayer?.m_guardianPower ?? "";

        public static void SelectPower(string name)
        {
            var p = Player.m_localPlayer;
            if (p == null) return;
            p.SetGuardianPower(name);
            Hotkeys.Notify(string.IsNullOrEmpty(name) ? "Power cleared" : $"Power: {name}");
        }

        public static void ActivatePower()
        {
            var p = Player.m_localPlayer;
            if (p == null) return;
            p.m_guardianPowerCooldown = 0f;
            p.StartGuardianPower();
        }

        public static List<StatusEffect> Active()
        {
            var p = Player.m_localPlayer;
            return p?.GetSEMan()?.m_statusEffects ?? new List<StatusEffect>();
        }

        public static void Apply(StatusEffect se)
        {
            var p = Player.m_localPlayer;
            if (p == null) return;
            p.GetSEMan().AddStatusEffect(se.NameHash(), true, 0, 0f, 0);
        }

        public static void Remove(StatusEffect se)
        {
            var p = Player.m_localPlayer;
            if (p == null) return;
            Frozen.Remove(se.NameHash());
            p.GetSEMan().RemoveStatusEffect(se, false);
        }

        /// <summary>Adds seconds to the running instance's ttl (a per-instance clone, so the catalog entry is untouched).</summary>
        public static void Extend(StatusEffect se, float seconds)
        {
            if (se.m_ttl <= 0f) return; // already permanent
            se.m_ttl += seconds;
        }

        public static void Tick()
        {
            var p = Player.m_localPlayer;
            if (p == null)
            {
                if (_powers != null) Invalidate();
                return;
            }

            if (State.NoPowerCooldown && p.m_guardianPowerCooldown > 0f)
                p.m_guardianPowerCooldown = 0f;

            var seman = p.GetSEMan();
            if (seman == null) return;

            if (State.InfinitePower && p.m_guardianPowerHash != 0)
            {
                var gp = seman.GetStatusEffect(p.m_guardianPowerHash);
                if (gp != null) gp.m_time = 0f;
            }

            if (Frozen.Count > 0)
            {
                foreach (var se in seman.m_statusEffects)
                    if (se != null && Frozen.Contains(se.NameHash())) se.m_time = 0f;
            }
        }

        public static int NameHash(this StatusEffect se)
            => se.m_nameHash != 0 ? se.m_nameHash : se.name.GetStableHashCode();
    }
}
