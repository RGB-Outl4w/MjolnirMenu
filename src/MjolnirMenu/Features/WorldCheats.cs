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
