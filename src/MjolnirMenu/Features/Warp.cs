using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BepInEx;
using MjolnirMenu.Core;
using UnityEngine;

namespace MjolnirMenu.Features
{
    /// <summary>Warp helpers + saved positions (BepInEx/config/MjolnirMenu.positions.txt, one "name|x|y|z" per line).</summary>
    public static class Warp
    {
        public sealed class SavedPos
        {
            public string Name = "";
            public Vector3 Pos;
        }

        public static readonly List<SavedPos> Saved = new List<SavedPos>();
        private static bool _loaded;

        /// <summary>True while one of our own teleports is in flight and the black screen should stay hidden.</summary>
        public static bool Silent { get; private set; }

        private static string FilePath => Path.Combine(Paths.ConfigPath, "MjolnirMenu.positions.txt");

        public static void To(Vector3 pos)
        {
            var p = Player.m_localPlayer;
            if (p == null) return;
            if (p.IsTeleporting())
            {
                Hotkeys.Notify("Already teleporting");
                return;
            }
            // distantTeleport=true makes Player.UpdateTeleport wait for the zone to load and snap to the floor.
            p.m_teleportCooldown = 2f; // TeleportTo refuses within 2s of the last one
            if (!p.TeleportTo(pos, p.transform.rotation, true)) return;
            if (State.InstantTeleport)
            {
                // Skip the 2s pre-move wait: the next UpdateTeleport moves us and finishes as soon as the area is ready.
                p.m_teleportTimer = 100f;
                Silent = true;
            }
        }

        /// <summary>Clears the silent flag once the game finishes (or abandons) the teleport.</summary>
        public static void Tick()
        {
            if (!Silent) return;
            var p = Player.m_localPlayer;
            if (p == null || !p.m_teleporting) Silent = false;
        }

        public static void ToMapPoint(Vector3 worldPos)
        {
            // Y from the world generator: good enough as a start height, UpdateTeleport finds the real floor.
            float y = WorldGenerator.instance != null ? WorldGenerator.instance.GetHeight(worldPos.x, worldPos.z) : 30f;
            To(new Vector3(worldPos.x, Mathf.Max(y, 30f) + 2f, worldPos.z));
        }

        public static void ToStart()
        {
            var zs = ZoneSystem.instance;
            if (zs != null && zs.GetLocationIcon("StartTemple", out var pos))
                To(pos + Vector3.up * 2f);
            else
                To(new Vector3(0f, 32f, 0f));
        }

        public static void ToSpawnPoint()
        {
            var profile = Game.instance?.GetPlayerProfile();
            if (profile != null && profile.HaveCustomSpawnPoint())
                To(profile.GetCustomSpawnPoint() + Vector3.up * 1f);
            else
                ToStart();
        }

        /// <summary>Every other player on the server (public position required for remote ones).</summary>
        public static List<ZNet.PlayerInfo> OtherPlayers()
        {
            var list = new List<ZNet.PlayerInfo>();
            var net = ZNet.instance;
            if (net == null) return list;
            var me = Player.m_localPlayer;
            foreach (var pi in net.GetPlayerList())
            {
                if (me != null && pi.m_name == me.GetPlayerName()) continue;
                list.Add(pi);
            }
            return list;
        }

        // ---- saved positions ----

        public static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            Saved.Clear();
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (var line in File.ReadAllLines(FilePath))
                {
                    var parts = line.Split('|');
                    if (parts.Length != 4) continue;
                    Saved.Add(new SavedPos
                    {
                        Name = parts[0],
                        Pos = new Vector3(
                            float.Parse(parts[1], CultureInfo.InvariantCulture),
                            float.Parse(parts[2], CultureInfo.InvariantCulture),
                            float.Parse(parts[3], CultureInfo.InvariantCulture)),
                    });
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not read saved positions: {e.Message}");
            }
        }

        public static void SaveCurrent(string name)
        {
            var p = Player.m_localPlayer;
            if (p == null) return;
            if (string.IsNullOrWhiteSpace(name)) name = $"Pos {Saved.Count + 1}";
            Saved.Add(new SavedPos { Name = name.Replace('|', '/'), Pos = p.transform.position });
            Persist();
        }

        public static void Remove(SavedPos sp)
        {
            Saved.Remove(sp);
            Persist();
        }

        private static void Persist()
        {
            try
            {
                var lines = new List<string>();
                foreach (var s in Saved)
                    lines.Add(string.Join("|", s.Name,
                        s.Pos.x.ToString("R", CultureInfo.InvariantCulture),
                        s.Pos.y.ToString("R", CultureInfo.InvariantCulture),
                        s.Pos.z.ToString("R", CultureInfo.InvariantCulture)));
                File.WriteAllLines(FilePath, lines);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not write saved positions: {e.Message}");
            }
        }
    }
}
