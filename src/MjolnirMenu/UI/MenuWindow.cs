using System.Collections.Generic;
using MjolnirMenu.Core;
using MjolnirMenu.Features;
using UnityEngine;

namespace MjolnirMenu.UI
{
    public static class MenuWindow
    {
        private const int WindowId = 0x4D4A4F4C; // "MJOL"
        private static readonly string[] Tabs = { "Player", "World", "Spawner", "Warp", "ESP", "MenuConfig" };

        private static Rect _rect = new Rect(60, 60, 640, 480);
        private static int _tab;
        private static bool _rectLoaded;

        // Spawner tab state
        private static string _filter = "";
        private static bool _showCreatures;
        private static string _stackText = "1";
        private static string _qualityText = "1";
        private static string _levelText = "1";
        private static string _countText = "1";
        private static bool _tamed;
        private static Vector2 _listScroll;
        private static float _killRadius = 30f;
        private static bool _killTamed;

        // Warp tab state
        private static string _posName = "";
        private static Vector2 _tpScroll;

        // World tab
        private static Vector2 _weatherScroll;

        // Skills tab
        private static Vector2 _skillScroll;
        private static string _allSkillsText = "100";

        public static void Draw()
        {
            Styles.EnsureBuilt();

            if (MenuConfig.ShowWatermark.Value)
                GUI.Label(new Rect(8, 4, 300, 20), $"MjolnirMenu  [{MenuConfig.MenuKey.Value}]", Styles.Watermark);

            if (!State.MenuOpen) return;

            if (!_rectLoaded)
            {
                _rectLoaded = true;
                _rect.x = Mathf.Clamp(MenuConfig.WindowX.Value, 0, Mathf.Max(0, Screen.width - _rect.width));
                _rect.y = Mathf.Clamp(MenuConfig.WindowY.Value, 0, Mathf.Max(0, Screen.height - _rect.height));
            }

            // Make sure the cursor is usable even if something else re-locked it.
            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible) Cursor.visible = true;

            var before = _rect;
            _rect = GUILayout.Window(WindowId, _rect, DrawWindow, "MJOLNIR MENU", Styles.Window);
            if (before.x != _rect.x || before.y != _rect.y)
            {
                MenuConfig.WindowX.Value = _rect.x;
                MenuConfig.WindowY.Value = _rect.y;
            }

            // Eat clicks that land on the window so they don't reach the game.
            if (Event.current.isMouse && _rect.Contains(Event.current.mousePosition))
                Event.current.Use();
        }

        private static void DrawWindow(int id)
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < Tabs.Length; i++)
            {
                if (GUILayout.Button(Tabs[i], i == _tab ? Styles.TabActive : Styles.Tab))
                    _tab = i;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(6);

            switch (_tab)
            {
                case 0: DrawPlayer(); break;
                case 1: DrawCombat(); break;
                case 2: DrawWorld(); break;
                case 3: DrawSkills(); break;
                case 4: DrawSpawner(); break;
                case 5: DrawTeleport(); break;
                case 6: DrawEsp(); break;
                default: DrawSettings(); break;
            }

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Player.m_localPlayer != null ? $"{Player.m_localPlayer.GetPlayerName()}  •  {(ZNet.instance != null && ZNet.instance.IsServer() ? "local/host" : "client")}" : "Not in game", Styles.Small);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset all", Styles.Button, GUILayout.Width(80)))
            {
                State.ResetAll();
                Hotkeys.Notify("All cheats OFF");
            }
            if (GUILayout.Button("Close", Styles.Button, GUILayout.Width(60)))
            {
                State.MenuOpen = false;
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, 10000, 22));
        }

        // ---------------- Player ----------------

        private static void DrawPlayer()
        {
            bool inGame = Player.m_localPlayer != null;
            if (!inGame) GUILayout.Label("Load into a world to use player cheats.", Styles.Small);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(260));
            GUILayout.Label("Survival", Styles.Header);
            if (Toggle(ref State.GodMode, "God mode  (no damage)")) PlayerCheats.ApplyGodMode();
            if (Toggle(ref State.GhostMode, "Ghost mode  (enemies ignore you)")) PlayerCheats.ApplyGhostMode();
            Toggle(ref State.InfiniteStamina, "Infinite stamina");
            Toggle(ref State.InfiniteEitr, "Infinite eitr");
            Toggle(ref State.NoFallDamage, "No fall damage");
            Toggle(ref State.InfiniteCarryWeight, "Infinite carry weight");
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label("Movement", Styles.Header);
            if (Toggle(ref State.Fly, "Fly  (jump = up, crouch = down)")) PlayerCheats.ApplyFly();
            Toggle(ref State.SpeedHack, $"Speed hack  ×{State.SpeedMultiplier:0.0}");
            State.SpeedMultiplier = GUILayout.HorizontalSlider(State.SpeedMultiplier, 1f, 10f);
            if (Toggle(ref State.JumpHack, $"Jump hack  ×{State.JumpMultiplier:0.0}")) PlayerCheats.ApplyJump();
            float j = GUILayout.HorizontalSlider(State.JumpMultiplier, 1f, 10f);
            if (j != State.JumpMultiplier) { State.JumpMultiplier = j; PlayerCheats.ApplyJump(); }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("Water", Styles.Header);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(300));
            if (Toggle(ref State.SwimSpeedHack, $"Swim speed hack  ×{State.SwimSpeedMultiplier:0.0}")) PlayerCheats.ApplySwimSpeed();
            float sw = GUILayout.HorizontalSlider(State.SwimSpeedMultiplier, 1f, 10f);
            if (sw != State.SwimSpeedMultiplier) { State.SwimSpeedMultiplier = sw; PlayerCheats.ApplySwimSpeed(); }
            Toggle(ref State.WaterJump, "Jump while swimming");
            if (Toggle(ref State.UnderwaterCamera, "Underwater camera  (follows you below the surface)")) PlayerCheats.ApplyCamera();
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            if (Toggle(ref State.WalkOnWater, "Walk on water  (surface acts as ground)") && State.WalkOnWater) State.SeabedWalk = false;
            if (Toggle(ref State.SeabedWalk, "Walk on seabed  (sink and walk underwater)") && State.SeabedWalk) State.WalkOnWater = false;
            GUILayout.Label("Water modes pause while Fly is on.", Styles.Small);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            GUI.enabled = inGame;
            if (GUILayout.Button("Heal + refill", Styles.Button, GUILayout.Width(110))) PlayerCheats.HealFull();
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        // ---------------- Combat ----------------

        private static void DrawCombat()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(300));
            GUILayout.Label("Gear", Styles.Header);
            Toggle(ref State.InfiniteDurability, "Infinite durability  (weapons, armor, tools)");
            GUILayout.Space(8);
            GUILayout.Label("Attack speed", Styles.Header);
            Toggle(ref State.AttackSpeedHack, $"Attack speed hack  ×{State.AttackSpeedMultiplier:0.0}");
            State.AttackSpeedMultiplier = GUILayout.HorizontalSlider(State.AttackSpeedMultiplier, 1f, 5f);
            GUILayout.Label("Scales the attack animation: swings, draws and combos finish faster.", Styles.Small);
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label("Damage", Styles.Header);
            Toggle(ref State.DamageHack, "Damage multiplier");
            GUILayout.Label($"Melee  ×{State.MeleeMultiplier:0.0}", Styles.Small);
            State.MeleeMultiplier = GUILayout.HorizontalSlider(State.MeleeMultiplier, 1f, 50f);
            GUILayout.Label($"Ranged  ×{State.RangedMultiplier:0.0}", Styles.Small);
            State.RangedMultiplier = GUILayout.HorizontalSlider(State.RangedMultiplier, 1f, 50f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("×1", Styles.Button)) State.MeleeMultiplier = State.RangedMultiplier = 1f;
            if (GUILayout.Button("×2", Styles.Button)) State.MeleeMultiplier = State.RangedMultiplier = 2f;
            if (GUILayout.Button("×5", Styles.Button)) State.MeleeMultiplier = State.RangedMultiplier = 5f;
            if (GUILayout.Button("×10", Styles.Button)) State.MeleeMultiplier = State.RangedMultiplier = 10f;
            if (GUILayout.Button("One-shot", Styles.Button)) State.MeleeMultiplier = State.RangedMultiplier = 50f;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        // ---------------- Skills ----------------

        private static void DrawSkills()
        {
            var skills = SkillCheats.GetSkills();
            if (skills.Count == 0)
            {
                GUILayout.Label("Load into a world to edit skills.", Styles.Small);
                return;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Max all (100)", Styles.Button, GUILayout.Width(110))) SkillCheats.MaxAll();
            if (GUILayout.Button("Reset all (0)", Styles.Button, GUILayout.Width(110))) SkillCheats.ResetAll();
            GUILayout.Space(16);
            GUILayout.Label("Set all to", Styles.Small, GUILayout.Width(60));
            _allSkillsText = GUILayout.TextField(_allSkillsText, GUILayout.Width(50));
            if (GUILayout.Button("Apply", Styles.Button, GUILayout.Width(60))) SkillCheats.SetAll(ParseFloat(_allSkillsText, 0f));
            GUILayout.EndHorizontal();
            GUILayout.Space(6);

            _skillScroll = GUILayout.BeginScrollView(_skillScroll, GUILayout.Height(330));
            foreach (var s in skills)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(SkillCheats.Name(s), Styles.ListRow, GUILayout.Width(150));
                GUILayout.Label($"{s.m_level:0}", Styles.Small, GUILayout.Width(36));
                float nv = GUILayout.HorizontalSlider(s.m_level, 0f, SkillCheats.MaxLevel, GUILayout.Width(220));
                if (Mathf.Abs(nv - s.m_level) > 0.01f) SkillCheats.SetLevel(s, nv);
                if (GUILayout.Button("0", Styles.Button, GUILayout.Width(32))) SkillCheats.SetLevel(s, 0f);
                if (GUILayout.Button("50", Styles.Button, GUILayout.Width(36))) SkillCheats.SetLevel(s, 50f);
                if (GUILayout.Button("100", Styles.Button, GUILayout.Width(40))) SkillCheats.SetLevel(s, 100f);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        // ---------------- World ----------------

        private static void DrawWorld()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(260));
            GUILayout.Label("Build & craft", Styles.Header);
            Toggle(ref State.FreeBuild, "Free build  (no piece cost)");
            Toggle(ref State.NoPlaceDelay, "No placement delay");
            Toggle(ref State.FreeCraft, "Free crafting  (no materials)");
            Toggle(ref State.InstantCraft, "Instant crafting");

            GUILayout.Space(8);
            GUILayout.Label("Time of day", Styles.Header);
            Toggle(ref State.LockTimeOfDay, $"Lock time  ({WorldCheats.CurrentTimeLabel()})");
            State.TimeOfDay = GUILayout.HorizontalSlider(State.TimeOfDay, 0f, 1f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Dawn", Styles.Button)) { State.TimeOfDay = 0.25f; State.LockTimeOfDay = true; }
            if (GUILayout.Button("Noon", Styles.Button)) { State.TimeOfDay = 0.5f; State.LockTimeOfDay = true; }
            if (GUILayout.Button("Dusk", Styles.Button)) { State.TimeOfDay = 0.75f; State.LockTimeOfDay = true; }
            if (GUILayout.Button("Night", Styles.Button)) { State.TimeOfDay = 0.0f; State.LockTimeOfDay = true; }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label($"Weather  ({(string.IsNullOrEmpty(State.ForcedWeather) ? "natural" : State.ForcedWeather)})", Styles.Header);
            if (GUILayout.Button("Natural weather", string.IsNullOrEmpty(State.ForcedWeather) ? Styles.TabActive : Styles.Button))
                State.ForcedWeather = "";
            _weatherScroll = GUILayout.BeginScrollView(_weatherScroll, GUILayout.Height(230));
            var names = WorldCheats.GetWeatherNames();
            if (names.Count == 0) GUILayout.Label("Load into a world first.", Styles.Small);
            foreach (var n in names)
            {
                if (GUILayout.Button(n, n == State.ForcedWeather ? Styles.TabActive : Styles.Button))
                    State.ForcedWeather = n;
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        // ---------------- Spawner ----------------

        private static void DrawSpawner()
        {
            if (!Spawner.EnsureCatalog())
            {
                GUILayout.Label("Catalog loads once you are in a world.", Styles.Small);
                return;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Items", !_showCreatures ? Styles.TabActive : Styles.Tab, GUILayout.Width(90))) _showCreatures = false;
            if (GUILayout.Button("Creatures", _showCreatures ? Styles.TabActive : Styles.Tab, GUILayout.Width(90))) _showCreatures = true;
            GUILayout.Space(10);
            GUILayout.Label("Search:", Styles.Small, GUILayout.Width(50));
            GUI.SetNextControlName("mjolnir_search");
            _filter = GUILayout.TextField(_filter, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("×", Styles.Button, GUILayout.Width(24))) _filter = "";
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (!_showCreatures)
            {
                GUILayout.Label("Stack", Styles.Small, GUILayout.Width(40));
                _stackText = GUILayout.TextField(_stackText, GUILayout.Width(50));
                GUILayout.Label("Quality", Styles.Small, GUILayout.Width(50));
                _qualityText = GUILayout.TextField(_qualityText, GUILayout.Width(40));
            }
            else
            {
                GUILayout.Label("Level", Styles.Small, GUILayout.Width(40));
                _levelText = GUILayout.TextField(_levelText, GUILayout.Width(40));
                GUILayout.Label("Count", Styles.Small, GUILayout.Width(40));
                _countText = GUILayout.TextField(_countText, GUILayout.Width(40));
                _tamed = GUILayout.Toggle(_tamed, "Tamed", Styles.Toggle);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            var source = _showCreatures ? Spawner.Creatures : Spawner.Items;
            _listScroll = GUILayout.BeginScrollView(_listScroll, GUILayout.Height(210));
            int shown = 0;
            foreach (var e in source)
            {
                if (!Spawner.Matches(e, _filter)) continue;
                if (++shown > 300) { GUILayout.Label("… refine your search", Styles.Small); break; }
                GUILayout.BeginHorizontal();
                GUILayout.Label(e.IsBoss ? $"★ {e.DisplayName}" : e.DisplayName, Styles.ListRow, GUILayout.Width(220));
                GUILayout.Label(e.PrefabName, Styles.Small, GUILayout.ExpandWidth(true));
                if (GUILayout.Button(_showCreatures ? "Spawn" : "Give", Styles.Button, GUILayout.Width(60)))
                {
                    if (_showCreatures)
                        Spawner.SpawnCreature(e.PrefabName, ParseInt(_levelText, 1), ParseInt(_countText, 1), _tamed);
                    else
                        Spawner.GiveItem(e.PrefabName, ParseInt(_stackText, 1), ParseInt(_qualityText, 1));
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Kill nearby ({_killRadius:0}m)", Styles.Header, GUILayout.Width(130));
            _killRadius = GUILayout.HorizontalSlider(_killRadius, 5f, 200f, GUILayout.Width(150));
            _killTamed = GUILayout.Toggle(_killTamed, "incl. tamed", Styles.Toggle);
            if (GUILayout.Button("Kill", Styles.Button, GUILayout.Width(60))) Spawner.KillNearby(_killRadius, _killTamed);
            GUILayout.EndHorizontal();
        }

        // ---------------- Warp ----------------

        private static void DrawTeleport()
        {
            Warp.EnsureLoaded();
            bool inGame = Player.m_localPlayer != null;

            GUILayout.Label("Map", Styles.Header);
            Toggle(ref State.MapClickTeleport, "Ctrl + left-click on the big map (M) to teleport");

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUI.enabled = inGame;
            if (GUILayout.Button("Start temple", Styles.Button)) Warp.ToStart();
            if (GUILayout.Button("Bed / spawn point", Styles.Button)) Warp.ToSpawnPoint();
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(270));
            GUILayout.Label("Saved positions", Styles.Header);
            GUILayout.BeginHorizontal();
            _posName = GUILayout.TextField(_posName, GUILayout.ExpandWidth(true));
            GUI.enabled = inGame;
            if (GUILayout.Button("Save here", Styles.Button, GUILayout.Width(80))) { Warp.SaveCurrent(_posName); _posName = ""; }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            _tpScroll = GUILayout.BeginScrollView(_tpScroll, GUILayout.Height(190));
            Warp.SavedPos? toRemove = null;
            foreach (var sp in Warp.Saved)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(sp.Name, Styles.ListRow, GUILayout.Width(120));
                GUILayout.Label($"{sp.Pos.x:0} / {sp.Pos.z:0}", Styles.Small, GUILayout.Width(80));
                GUI.enabled = inGame;
                if (GUILayout.Button("Go", Styles.Button, GUILayout.Width(36))) Warp.To(sp.Pos);
                GUI.enabled = true;
                if (GUILayout.Button("×", Styles.Button, GUILayout.Width(24))) toRemove = sp;
                GUILayout.EndHorizontal();
            }
            if (toRemove != null) Warp.Remove(toRemove);
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label("Players", Styles.Header);
            var others = Warp.OtherPlayers();
            if (others.Count == 0) GUILayout.Label("No other players.", Styles.Small);
            foreach (var pi in others)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(pi.m_name, Styles.ListRow, GUILayout.ExpandWidth(true));
                GUI.enabled = inGame && pi.m_publicPosition;
                if (GUILayout.Button(pi.m_publicPosition ? "Go" : "hidden", Styles.Button, GUILayout.Width(60)))
                    Warp.To(pi.m_position + Vector3.up * 1f);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        // ---------------- ESP ----------------

        private static void DrawEsp()
        {
            GUILayout.Label("Show", Styles.Header);
            Toggle(ref State.EspPlayers, "Players");
            Toggle(ref State.EspCreatures, "Creatures & bosses  (name, level, HP)");
            Toggle(ref State.EspResources, "Resources  (pickables, ore, trees) — heavier");
            GUILayout.Space(6);
            GUILayout.Label("Style", Styles.Header);
            Toggle(ref State.EspDistance, "Distance");
            Toggle(ref State.EspLines, "Tracer lines");
            GUILayout.Space(6);
            GUILayout.Label($"Range  {MenuConfig.EspRange.Value:0}m", Styles.Header);
            MenuConfig.EspRange.Value = GUILayout.HorizontalSlider(MenuConfig.EspRange.Value, 20f, 1000f, GUILayout.Width(300));
            GUILayout.Label($"Max labels  {MenuConfig.EspMaxEntities.Value}", Styles.Header);
            MenuConfig.EspMaxEntities.Value = Mathf.RoundToInt(GUILayout.HorizontalSlider(MenuConfig.EspMaxEntities.Value, 10, 2000, GUILayout.Width(300)));
        }

        // ---------------- MenuConfig ----------------

        private static void DrawSettings()
        {
            GUILayout.Label("Hotkeys  (edit in BepInEx/config/com.mjolnir.menu.cfg)", Styles.Header);
            GUILayout.Label($"Menu: {MenuConfig.MenuKey.Value}    God: {MenuConfig.GodKey.Value}    Fly: {MenuConfig.FlyKey.Value}    Panic reset: {MenuConfig.PanicKey.Value}", Styles.Small);
            GUILayout.Space(8);
            bool wm = MenuConfig.ShowWatermark.Value;
            if (Toggle(ref wm, "Show watermark")) MenuConfig.ShowWatermark.Value = wm;
            GUILayout.Space(8);
            GUILayout.Label("Defaults", Styles.Header);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save speed/jump as default", Styles.Button, GUILayout.Width(200)))
            {
                MenuConfig.SpeedMultiplier.Value = State.SpeedMultiplier;
                MenuConfig.JumpMultiplier.Value = State.JumpMultiplier;
                Hotkeys.Notify("Defaults saved");
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(12);
            GUILayout.Label("MjolnirMenu is meant for single-player and servers you own. Respect other people's servers.", Styles.Small);
            GUILayout.Label($"v{MyPluginInfo.PLUGIN_VERSION}", Styles.Small);
        }

        // ---------------- helpers ----------------

        /// <summary>Draws a toggle; returns true on the frame the value changed.</summary>
        private static bool Toggle(ref bool value, string label)
        {
            bool nv = GUILayout.Toggle(value, "  " + label, Styles.Toggle);
            if (nv == value) return false;
            value = nv;
            return true;
        }

        private static int ParseInt(string s, int fallback)
            => int.TryParse(s, out var v) ? v : fallback;

        private static float ParseFloat(string s, float fallback)
            => float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : fallback;
    }
}
