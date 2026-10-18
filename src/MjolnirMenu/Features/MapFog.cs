using System;
using System.Collections;
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
            if (key.Length > 0) Load();
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

        public static void Restore()
        {
            var mm = Minimap.instance;
            if (mm == null) return;
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
