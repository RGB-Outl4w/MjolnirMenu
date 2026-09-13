using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using BepInEx;
using MjolnirMenu.Core;

namespace MjolnirMenu.Features
{
    /// <summary>
    /// Snapshots of <see cref="State"/> as "Field=Value" text files in BepInEx/config/MjolnirMenu.presets/.
    /// Every public static bool/float/int/string field is included except the menu-open flag.
    /// </summary>
    public static class Presets
    {
        private static readonly HashSet<string> Skip = new HashSet<string> { nameof(State.MenuOpen) };

        private static bool _autoLoadedThisSession;
        private static Player? _lastPlayer;

        public static string Dir => Path.Combine(Paths.ConfigPath, "MjolnirMenu.presets");

        public static List<string> List()
        {
            var names = new List<string>();
            try
            {
                if (!Directory.Exists(Dir)) return names;
                foreach (var f in Directory.GetFiles(Dir, "*.preset"))
                    names.Add(Path.GetFileNameWithoutExtension(f));
                names.Sort(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Preset list failed: {e.Message}");
            }
            return names;
        }

        private static string PathFor(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return Path.Combine(Dir, name + ".preset");
        }

        private static IEnumerable<FieldInfo> Fields()
        {
            foreach (var f in typeof(State).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (Skip.Contains(f.Name)) continue;
                var t = f.FieldType;
                if (t == typeof(bool) || t == typeof(float) || t == typeof(int) || t == typeof(string))
                    yield return f;
            }
        }

        public static bool Save(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) name = "default";
            try
            {
                Directory.CreateDirectory(Dir);
                var lines = new List<string> { $"# MjolnirMenu preset  v{MyPluginInfo.PLUGIN_VERSION}  {DateTime.Now:yyyy-MM-dd HH:mm}" };
                foreach (var f in Fields())
                {
                    var v = f.GetValue(null);
                    string s = v switch
                    {
                        float fl => fl.ToString("R", CultureInfo.InvariantCulture),
                        int i => i.ToString(CultureInfo.InvariantCulture),
                        bool b => b ? "true" : "false",
                        string str => str.Replace("\r", "").Replace("\n", " "),
                        _ => "",
                    };
                    lines.Add($"{f.Name}={s}");
                }
                File.WriteAllLines(PathFor(name), lines);
                Hotkeys.Notify($"Preset '{name}' saved");
                return true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Preset save failed: {e.Message}");
                return false;
            }
        }

        public static bool Load(string name, bool quiet = false)
        {
            try
            {
                var path = PathFor(name);
                if (!File.Exists(path)) return false;
                var map = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var raw in File.ReadAllLines(path))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line[0] == '#') continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    map[line.Substring(0, eq)] = line.Substring(eq + 1);
                }

                foreach (var f in Fields())
                {
                    if (!map.TryGetValue(f.Name, out var s)) continue;
                    var t = f.FieldType;
                    if (t == typeof(bool) && bool.TryParse(s, out var b)) f.SetValue(null, b);
                    else if (t == typeof(float) && float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var fl)) f.SetValue(null, fl);
                    else if (t == typeof(int) && int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) f.SetValue(null, i);
                    else if (t == typeof(string)) f.SetValue(null, s);
                }

                PlayerCheats.ApplyAll();
                WorldCheats.ApplyAll();
                MenuConfig.HideCheatTags.Value = State.HideCheatTags;
                if (!quiet) Hotkeys.Notify($"Preset '{name}' loaded");
                return true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Preset load failed: {e.Message}");
                return false;
            }
        }

        public static void Delete(string name)
        {
            try
            {
                var path = PathFor(name);
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Preset delete failed: {e.Message}");
            }
        }

        /// <summary>Applies the configured auto-load preset once, the first time a Player exists this session.</summary>
        public static void Tick()
        {
            var p = Player.m_localPlayer;
            if (p == null)
            {
                _lastPlayer = null;
                return;
            }
            if (ReferenceEquals(p, _lastPlayer)) return;
            _lastPlayer = p;

            if (_autoLoadedThisSession) return;
            _autoLoadedThisSession = true;
            var name = MenuConfig.AutoLoadPreset.Value;
            if (!string.IsNullOrWhiteSpace(name) && Load(name))
                Plugin.Log.LogInfo($"Auto-loaded preset '{name}'");
        }
    }
}
