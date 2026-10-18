using System;
using System.IO;
using MjolnirMenu.Core;

namespace MjolnirMenu.Features
{
    /// <summary>
    /// Character rename, main menu only. Only the name stored inside the profile changes: the save
    /// file name and the player ID stay the same, so the character's .fch, its world data and
    /// everything it owns (beds, wards, portals, tombstones are keyed by player ID) keep working.
    /// </summary>
    public static class Rename
    {
        public const int MinLength = 3, MaxLength = 15;

        /// <summary>Character selected on the main menu, or null when renaming isn't safe right now.</summary>
        public static PlayerProfile? Selected
        {
            get
            {
                var fs = FejdStartup.instance;
                // In a world the profile is live (ZDOs, peers, autosave): never touch it there.
                if (fs == null || ZNet.instance != null || Player.m_localPlayer != null) return null;
                if (fs.m_profiles == null || fs.m_profileIndex < 0 || fs.m_profileIndex >= fs.m_profiles.Count) return null;
                return fs.m_profiles[fs.m_profileIndex];
            }
        }

        /// <summary>Same rules as character creation: letters only, stored lowercase.</summary>
        public static string? Validate(string name)
        {
            name = name.Trim();
            if (name.Length < MinLength || name.Length > MaxLength) return L.F("Name must be {0}–{1} letters", MinLength, MaxLength);
            foreach (char c in name)
                if (!char.IsLetter(c)) return "Letters only — no spaces, digits or symbols";
            return null;
        }

        public static void Apply(string newName)
        {
            var sel = Selected;
            if (sel == null) { Hotkeys.Notify("Rename only works on the main menu"); return; }
            string? err = Validate(newName);
            if (err != null) { Hotkeys.Notify(err); return; }
            // ponytail: consoles mount platform save storage around profile IO; that API needs another game DLL, so skip those platforms.
            if (FileHelpers.CloudStorageSupported) { Hotkeys.Notify("Renaming isn't supported on this platform's save storage"); return; }

            // Reload from disk instead of trusting the menu's copy: if the file can't be read completely,
            // saving would write back an empty character.
            var profile = new PlayerProfile(sel.m_filename, sel.m_fileSource);
            if (!profile.Load()) { Hotkeys.Notify("Couldn't read the character file — nothing changed"); return; }

            try
            {
                if (FileSourceHelper.IsNotCloud(sel.m_fileSource))
                {
                    string file = Path.Combine(SaveSystem.GetCharacterFolderPath(sel.m_fileSource), sel.m_filename + ".fch");
                    if (File.Exists(file)) File.Copy(file, file + ".mjolnir-rename.bak", true);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Rename backup failed, aborting: {e.Message}");
                Hotkeys.Notify("Couldn't back up the character file — nothing changed");
                return;
            }

            string old = profile.GetName();
            profile.SetName(newName.Trim().ToLowerInvariant());
            if (!profile.Save()) { Hotkeys.Notify("Saving failed — the character is unchanged"); return; }

            Plugin.Log.LogInfo($"Renamed character '{old}' -> '{profile.GetName()}' (file {sel.m_filename}.fch)");
            Hotkeys.Notify(L.F("Renamed {0} → {1}", old, profile.GetName()));
            FejdStartup.instance.UpdateCharacterList();
        }
    }
}
