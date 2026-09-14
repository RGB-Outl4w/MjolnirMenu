using System.Collections.Generic;
using MjolnirMenu.Core;
using UnityEngine;

namespace MjolnirMenu.Features
{
    /// <summary>Build / craft / environment cheats. Same reconcile-every-frame pattern as PlayerCheats.</summary>
    public static class WorldCheats
    {
        private static Player? _trackedPlayer;
        private static float _origPlaceDelay;

        private static InventoryGui? _trackedGui;
        private static float _origCraftDuration;

        private static string _appliedWeather = "";

        // Station capacity: originals per instance so toggling off restores vanilla values.
        private static readonly Dictionary<Component, float> _origCapacity = new Dictionary<Component, float>();
        private static float _nextStationScan;
        private static float _appliedStationMul = -1f;

        public static void Tick()
        {
            var p = Player.m_localPlayer;
            if (p != null)
            {
                if (!ReferenceEquals(p, _trackedPlayer))
                {
                    _trackedPlayer = p;
                    _origPlaceDelay = p.m_placeDelay;
                }

                if (p.m_noPlacementCost != State.FreeBuild)
                    p.m_noPlacementCost = State.FreeBuild;

                float delay = State.NoPlaceDelay ? 0f : _origPlaceDelay;
                if (!Mathf.Approximately(p.m_placeDelay, delay))
                    p.m_placeDelay = delay;
            }
            else
            {
                _trackedPlayer = null;
            }

            var gui = InventoryGui.instance;
            if (gui != null)
            {
                if (!ReferenceEquals(gui, _trackedGui))
                {
                    _trackedGui = gui;
                    _origCraftDuration = gui.m_craftDuration;
                }
                float dur = State.InstantCraft ? 0.01f : _origCraftDuration;
                if (!Mathf.Approximately(gui.m_craftDuration, dur))
                    gui.m_craftDuration = dur;
            }
            else
            {
                _trackedGui = null;
            }

            UpdateStations();

            var env = EnvMan.instance;
            if (env != null)
            {
                if (State.LockTimeOfDay)
                {
                    env.m_debugTimeOfDay = true;
                    env.m_debugTime = Mathf.Clamp01(State.TimeOfDay);
                }
                else if (env.m_debugTimeOfDay)
                {
                    env.m_debugTimeOfDay = false;
                }

                if (_appliedWeather != State.ForcedWeather)
                {
                    env.SetForceEnvironment(State.ForcedWeather);
                    _appliedWeather = State.ForcedWeather;
                }
            }
        }

        public static void ApplyAll() => Tick();

        /// <summary>
        /// Smelters / kilns / windmills / spinning wheels (Smelter), fires, beehives, sap collectors and
        /// cooking-station fuel: multiply the per-instance capacity field. Rescans every 2s so newly
        /// loaded stations get it too; restores originals when the toggle goes off.
        /// </summary>
        private static void UpdateStations()
        {
            float mul = State.BigStations ? Mathf.Max(1f, State.StationMultiplier) : 1f;
            bool changed = !Mathf.Approximately(mul, _appliedStationMul);
            if (!State.BigStations && !changed) return;
            if (!changed && Time.unscaledTime < _nextStationScan) return;
            _nextStationScan = Time.unscaledTime + 2f;
            _appliedStationMul = mul;

            foreach (var s in Object.FindObjectsByType<Smelter>(FindObjectsSortMode.None))
            {
                Apply(s, ref s.m_maxOre, mul, "ore");
                Apply(s, ref s.m_maxFuel, mul, "fuel");
            }
            foreach (var f in Object.FindObjectsByType<Fireplace>(FindObjectsSortMode.None))
                ApplyF(f, ref f.m_maxFuel, mul);
            foreach (var b in Object.FindObjectsByType<Beehive>(FindObjectsSortMode.None))
                Apply(b, ref b.m_maxHoney, mul, "honey");
            foreach (var sc in Object.FindObjectsByType<SapCollector>(FindObjectsSortMode.None))
                Apply(sc, ref sc.m_maxLevel, mul, "sap");
            foreach (var c in Object.FindObjectsByType<CookingStation>(FindObjectsSortMode.None))
                Apply(c, ref c.m_maxFuel, mul, "fuel");

            if (mul <= 1f) _origCapacity.Clear();
        }

        private static readonly Dictionary<string, Dictionary<Component, int>> _origInt = new Dictionary<string, Dictionary<Component, int>>();

        private static void Apply(Component c, ref int field, float mul, string key)
        {
            if (!_origInt.TryGetValue(key, out var map)) _origInt[key] = map = new Dictionary<Component, int>();
            if (!map.TryGetValue(c, out int orig)) map[c] = orig = field;
            int target = Mathf.Max(1, Mathf.RoundToInt(orig * mul));
            if (field != target) field = target;
            if (mul <= 1f) map.Remove(c);
        }

        private static void ApplyF(Component c, ref float field, float mul)
        {
            if (!_origCapacity.TryGetValue(c, out float orig)) _origCapacity[c] = orig = field;
            float target = Mathf.Max(1f, orig * mul);
            if (!Mathf.Approximately(field, target)) field = target;
        }

        /// <summary>Names of every environment the game knows (Clear, Rain, ThunderStorm, ...).</summary>
        public static List<string> GetWeatherNames()
        {
            var list = new List<string>();
            var env = EnvMan.instance;
            if (env == null) return list;
            foreach (var e in env.m_environments)
                if (e != null && !string.IsNullOrEmpty(e.m_name))
                    list.Add(e.m_name);
            return list;
        }

        public static string CurrentTimeLabel()
        {
            float t = State.LockTimeOfDay ? State.TimeOfDay : (EnvMan.instance != null ? EnvMan.instance.GetDayFraction() : 0f);
            int minutes = Mathf.RoundToInt(t * 24f * 60f);
            return $"{minutes / 60:00}:{minutes % 60:00}";
        }
    }
}
