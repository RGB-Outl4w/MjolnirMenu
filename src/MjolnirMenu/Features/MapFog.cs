using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using MjolnirMenu.Core;
using UnityEngine;

namespace MjolnirMenu.Features
{
    /// <summary>
    /// Reveal map / restore fog that keeps what you explored legitimately. Reveal snapshots the explored
    /// bits first; while the map is revealed, the player's own exploration keeps being recorded into the
    /// snapshot; restore resets the fog and re-explores exactly the snapshot. The snapshot is stored per
    /// character + world in BepInEx/config/MjolnirMenu.fog/, because the revealed map is saved with the profile.
    /// </summary>
    public static class MapFog
    {
        private static BitArray? _own, _others;
        private static string _key = "";
        private static readonly List<Minimap.PinData> _poiPins = new List<Minimap.PinData>();

        /// <summary>Location prefab prefix → map label. Boss altars come from Warp.BossAltars.</summary>
        private static readonly string[,] Pois =
        {
            { "Vendor_BlackForest", "Trader" }, { "Hildir_camp", "Hildir" }, { "Crypt", "Burial chamber" },
            { "SunkenCrypt", "Sunken crypt" }, { "TrollCave", "Troll cave" }, { "MountainCave", "Frost cave" },
            { "Mistlands_DvergrTownEntrance", "Infested mine" }, { "CharredFortress", "Fortress" },
            { "Hildir_crypt", "Hildir's crypt" }, { "Hildir_cave", "Hildir's cave" }, { "Hildir_plainsfortress", "Hildir's tower" },
            { "GoblinCamp", "Fuling village" }, { "WoodVillage", "Draugr village" }, { "NorthVillage", "Village" },
        };
        private static string Dir => Path.Combine(Paths.ConfigPath, "MjolnirMenu.fog");
        private static string FileFor(string key) => Path.Combine(Dir, key + ".bin");

        public static bool HasSnapshot => _own != null;

        private static string CurrentKey()
        {
            var profile = Game.instance?.GetPlayerProfile();
            if (Minimap.instance == null || ZNet.instance == null || profile == null) return "";
            string raw = profile.m_filename + "_" + ZNet.instance.GetWorldName();
            foreach (char c in Path.GetInvalidFileNameChars()) raw = raw.Replace(c, '_');
            return raw;
        }

        /// <summary>Load the snapshot when a world opens, save it when the world closes.</summary>
        public static void Tick()
        {
            string key = CurrentKey();
            if (key == _key) return;
            Save();
            _key = key;
            _own = _others = null;
            _poiPins.Clear();
            if (key.Length > 0) { Load(); if (File.Exists(PoiFlag)) AddPoiPins(); }
        }

        public static void Reveal()
        {
            var mm = Minimap.instance;
            if (mm == null) return;
            if (_own == null)
            {
                _own = new BitArray(mm.m_explored);
                _others = new BitArray(mm.m_exploredOthers);
                Save();
            }
            mm.ExploreAll();
            Hotkeys.Notify("Map revealed");
        }

        private static string PoiFlag => FileFor(_key) + ".pois";

        /// <summary>
        /// Pins every dungeon, trader, village and boss altar in the world. Host / single-player only: clients
        /// don't get the location list. Pins aren't saved with the profile; the flag file re-adds them next session,
        /// and Restore fog removes them while keeping pins you placed or discovered yourself.
        /// </summary>
        public static void RevealPois()
        {
            var zs = ZoneSystem.instance;
            if (Minimap.instance == null || zs == null) return;
            if (ZNet.instance == null || !ZNet.instance.IsServer() || zs.m_locationInstances.Count == 0)
            {
                Hotkeys.Notify("Only the host / single-player knows where every location is");
                return;
            }
            int n = AddPoiPins();
            try { File.WriteAllText(PoiFlag, "1"); } catch (Exception e) { Plugin.Log.LogWarning($"POI flag write failed: {e.Message}"); }
            Hotkeys.Notify(L.F("Pinned {0} places", n));
        }

        private static int AddPoiPins()
        {
            var mm = Minimap.instance;
            var zs = ZoneSystem.instance;
            if (mm == null || zs == null || _poiPins.Count > 0) return _poiPins.Count;
            foreach (var li in zs.m_locationInstances.Values)
            {
                string name = li.m_location?.m_prefabName ?? "";
                string? label = null;
                var type = Minimap.PinType.Icon3;
                for (int i = 0; i < Warp.BossAltars.GetLength(0) && label == null; i++)
                    if (name == Warp.BossAltars[i, 1]) { label = Warp.BossAltars[i, 0]; type = Minimap.PinType.Boss; }
                for (int i = 0; i < Pois.GetLength(0) && label == null; i++)
                    if (name.StartsWith(Pois[i, 0])) label = Pois[i, 1];
                if (label == null || HavePin(mm, li.m_position, type)) continue;
                _poiPins.Add(mm.AddPin(li.m_position, type, L.T(label), false, false, 0L, default));
            }
            return _poiPins.Count;
        }

        /// <summary>A pin of this kind already there (Vegvisir, your own) wins: we don't stack ours on it.</summary>
        private static bool HavePin(Minimap mm, Vector3 pos, Minimap.PinType type)
        {
            foreach (var p in mm.m_pins)
                if (p.m_type == type && Vector3.Distance(p.m_pos, pos) < 30f) return true;
            return false;
        }

        private static void RemovePoiPins()
        {
            var mm = Minimap.instance;
            if (mm != null) foreach (var p in _poiPins) mm.RemovePin(p);
            _poiPins.Clear();
            try { if (_key.Length > 0) File.Delete(PoiFlag); } catch (Exception e) { Plugin.Log.LogWarning($"POI flag delete failed: {e.Message}"); }
        }

        public static void Restore()
        {
            var mm = Minimap.instance;
            if (mm == null) return;
            RemovePoiPins();
            mm.Reset();
            if (_own == null || _others == null || _own.Length != mm.m_explored.Length)
            {
                Hotkeys.Notify("Map fog restored (no snapshot from Reveal — everything is fogged)");
                return;
            }
            int size = mm.m_textureSize;
            for (int i = 0; i < _own.Length; i++)
            {
                if (_own[i]) mm.Explore(i % size, i / size);
                if (_others[i]) mm.ExploreOthers(i % size, i / size);
            }
            mm.m_fogTexture.Apply();
            _own = _others = null;
            try { File.Delete(FileFor(_key)); } catch (Exception e) { Plugin.Log.LogWarning($"Fog snapshot delete failed: {e.Message}"); }
            Hotkeys.Notify("Map fog restored");
        }

        /// <summary>
        /// Mirrors Minimap.Explore(Vector3, float) into the snapshot. ponytail: cartography-table maps read
        /// while revealed aren't recorded (they arrive through ExploreOthers along with save loading).
        /// </summary>
        public static void OnExplore(Minimap mm, Vector3 p, float radius)
        {
            if (_own == null) return;
            int r = Mathf.CeilToInt(radius / mm.m_pixelSize), size = mm.m_textureSize;
            mm.WorldToPixel(p, out int cx, out int cy);
            for (int y = cy - r; y <= cy + r; y++)
            {
                if (y < 0 || y >= size) continue;
                for (int x = cx - r; x <= cx + r; x++)
                    if (x >= 0 && x < size && new Vector2(x - cx, y - cy).magnitude <= r)
                        _own[y * size + x] = true;
            }
        }

        public static void Save()
        {
            if (_own == null || _others == null || _key.Length == 0) return;
            try
            {
                Directory.CreateDirectory(Dir);
                var a = new byte[(_own.Length + 7) / 8];
                var b = new byte[(_others.Length + 7) / 8];
                _own.CopyTo(a, 0);
                _others.CopyTo(b, 0);
                using var w = new BinaryWriter(File.Create(FileFor(_key)));
                w.Write(_own.Length);
                w.Write(a);
                w.Write(b);
            }
            catch (Exception e) { Plugin.Log.LogWarning($"Fog snapshot save failed: {e.Message}"); }
        }

        private static void Load()
        {
            string file = FileFor(_key);
            if (!File.Exists(file)) return;
            try
            {
                using var r = new BinaryReader(File.OpenRead(file));
                int len = r.ReadInt32();
                int bytes = (len + 7) / 8;
                _own = new BitArray(r.ReadBytes(bytes)) { Length = len };
                _others = new BitArray(r.ReadBytes(bytes)) { Length = len };
            }
            catch (Exception e) { Plugin.Log.LogWarning($"Fog snapshot load failed: {e.Message}"); _own = _others = null; }
        }
    }
}
