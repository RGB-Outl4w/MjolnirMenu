using System.Collections.Generic;
using MjolnirMenu.Core;
using MjolnirMenu.Features;
using UnityEngine;

namespace MjolnirMenu.UI
{
    public static class MenuWindow
    {
        private const int WindowId = 0x4D4A4F4C; // "MJOL"
        private static readonly string[] Tabs = { "Player", "Combat", "World", "Skills", "Effects", "Spawner", "Teleport", "ESP", "Settings", "Credits" };

        private static Rect _rect = new Rect(60, 60, 760, 540);
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

        // Effects tab
        private static Vector2 _effectScroll;
        private static Vector2 _activeScroll;
        private static string _effectFilter = "";

        // Settings tab
        private static string _presetName = "";

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
                case 4: DrawEffects(); break;
                case 5: DrawSpawner(); break;
                case 6: DrawTeleport(); break;
                case 7: DrawEsp(); break;
                case 8: DrawSettings(); break;
                default: DrawCredits(); break;
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
            if (Toggle(ref State.CrouchSpeedHack, $"Crouch speed hack  ×{State.CrouchSpeedMultiplier:0.0}")) PlayerCheats.ApplyCrouchSpeed();
            float cs = GUILayout.HorizontalSlider(State.CrouchSpeedMultiplier, 1f, 10f);
            if (cs != State.CrouchSpeedMultiplier) { State.CrouchSpeedMultiplier = cs; PlayerCheats.ApplyCrouchSpeed(); }
            Toggle(ref State.CrouchInfiniteStamina, "Infinite stamina while crouched");
            Toggle(ref State.EmoteSpeedHack, $"Sit / stand animation speed  ×{State.EmoteSpeedMultiplier:0.0}");
            State.EmoteSpeedMultiplier = GUILayout.HorizontalSlider(State.EmoteSpeedMultiplier, 1f, 5f);
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
            Toggle(ref State.InfiniteItems, "Infinite items  (ammo, food, mead, fuel, ore, feed never consumed)");
            Toggle(ref State.InfiniteInteract, "Infinite interaction  (pick-ups / bushes / hives stay; walk-over pickup off)");
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
            GUILayout.Space(6);
            GUILayout.Label("Harvesting & structures  (same master toggle)", Styles.Header);
            GUILayout.Label($"Trees  ×{State.TreeMultiplier:0.0}", Styles.Small);
            State.TreeMultiplier = GUILayout.HorizontalSlider(State.TreeMultiplier, 1f, 50f);
            GUILayout.Label($"Stones / ores  ×{State.RockMultiplier:0.0}", Styles.Small);
            State.RockMultiplier = GUILayout.HorizontalSlider(State.RockMultiplier, 1f, 50f);
            GUILayout.Label($"Player structures  ×{State.PlayerStructureMultiplier:0.0}", Styles.Small);
            State.PlayerStructureMultiplier = GUILayout.HorizontalSlider(State.PlayerStructureMultiplier, 1f, 50f);
            GUILayout.Label($"World structures / destructibles  ×{State.WorldStructureMultiplier:0.0}", Styles.Small);
            State.WorldStructureMultiplier = GUILayout.HorizontalSlider(State.WorldStructureMultiplier, 1f, 50f);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        // ---------------- Effects ----------------

        private static void DrawEffects()
        {
            if (Player.m_localPlayer == null)
            {
                GUILayout.Label("Load into a world to manage effects.", Styles.Small);
                return;
            }

            GUILayout.Label($"Forsaken power  (current: {(string.IsNullOrEmpty(Effects.CurrentPower()) ? "none" : Effects.CurrentPower())})", Styles.Header);
            GUILayout.BeginHorizontal();
            foreach (var gp in Effects.GuardianPowers())
            {
                bool cur = gp.name == Effects.CurrentPower();
                if (GUILayout.Button(Effects.Label(gp), cur ? Styles.TabActive : Styles.Button)) Effects.SelectPower(gp.name);
            }
            if (GUILayout.Button("None", Styles.Button, GUILayout.Width(50))) Effects.SelectPower("");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            Toggle(ref State.NoPowerCooldown, "No cooldown");
            Toggle(ref State.InfinitePower, "Power lasts forever");
            if (GUILayout.Button("Activate now", Styles.Button, GUILayout.Width(100))) Effects.ActivatePower();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(360));
            GUILayout.Label("Active effects", Styles.Header);
            _activeScroll = GUILayout.BeginScrollView(_activeScroll, GUILayout.Height(240));
            var active = new List<StatusEffect>(Effects.Active());
            if (active.Count == 0) GUILayout.Label("None.", Styles.Small);
            foreach (var se in active)
            {
                if (se == null) continue;
                bool frozen = Effects.Frozen.Contains(se.NameHash());
                GUILayout.BeginHorizontal();
                GUILayout.Label(Effects.Label(se), Styles.ListRow, GUILayout.Width(130));
                string t = se.m_ttl > 0f ? $"{se.GetRemaningTime():0}s" : "∞";
                GUILayout.Label(frozen ? $"{t} ❄" : t, Styles.Small, GUILayout.Width(60));
                if (GUILayout.Button("+60s", Styles.Button, GUILayout.Width(44))) Effects.Extend(se, 60f);
                if (GUILayout.Button("+10m", Styles.Button, GUILayout.Width(44))) Effects.Extend(se, 600f);
                if (GUILayout.Button(frozen ? "Unfreeze" : "Freeze", Styles.Button, GUILayout.Width(64)))
                {
                    if (frozen) Effects.Frozen.Remove(se.NameHash()); else Effects.Frozen.Add(se.NameHash());
                }
                if (GUILayout.Button("×", Styles.Button, GUILayout.Width(24))) Effects.Remove(se);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("All effects", Styles.Header, GUILayout.Width(80));
            _effectFilter = GUILayout.TextField(_effectFilter, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("×", Styles.Button, GUILayout.Width(24))) _effectFilter = "";
            GUILayout.EndHorizontal();
            _effectScroll = GUILayout.BeginScrollView(_effectScroll, GUILayout.Height(240));
            int shown = 0;
            foreach (var se in Effects.Catalog())
            {
                string label = Effects.Label(se);
                if (_effectFilter.Length > 0
                    && label.IndexOf(_effectFilter, System.StringComparison.OrdinalIgnoreCase) < 0
                    && se.name.IndexOf(_effectFilter, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (++shown > 200) { GUILayout.Label("… refine your search", Styles.Small); break; }
                GUILayout.BeginHorizontal();
                GUILayout.Label(label, Styles.ListRow, GUILayout.Width(150));
                GUILayout.Label(se.name, Styles.Small, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Apply", Styles.Button, GUILayout.Width(50))) Effects.Apply(se);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
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
            if (!_showCreatures)
                Toggle(ref State.SpawnerShowAll, "Show all");
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
                if (!_showCreatures && !State.SpawnerShowAll && !e.InventorySafe) continue;
                if (!Spawner.Matches(e, _filter)) continue;
                if (++shown > 300) { GUILayout.Label("… refine your search", Styles.Small); break; }
                GUILayout.BeginHorizontal();
                string rowName = e.IsBoss ? $"★ {e.DisplayName}" : e.DisplayName;
                if (!e.IsCreature && !e.InventorySafe) rowName += "  (not an inventory item)";
                GUILayout.Label(rowName, Styles.ListRow, GUILayout.Width(220));
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
            Toggle(ref State.FastTeleport, $"Fast teleport / portals  ×{State.TeleportSpeed:0}  (shortens the vortex wait)");
            State.TeleportSpeed = Mathf.Round(GUILayout.HorizontalSlider(State.TeleportSpeed, 1f, 40f, GUILayout.Width(300)));

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
            GUILayout.Label("Presets  (BepInEx/config/MjolnirMenu.presets)", Styles.Header);
            GUILayout.BeginHorizontal();
            _presetName = GUILayout.TextField(_presetName, GUILayout.Width(180));
            if (GUILayout.Button("Save current", Styles.Button, GUILayout.Width(100))) { Presets.Save(_presetName); }
            GUILayout.EndHorizontal();
            string auto = MenuConfig.AutoLoadPreset.Value;
            foreach (var name in Presets.List())
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(name, Styles.ListRow, GUILayout.Width(180));
                if (GUILayout.Button("Load", Styles.Button, GUILayout.Width(50))) Presets.Load(name);
                bool isAuto = name == auto;
                if (GUILayout.Button(isAuto ? "Auto-load: ON" : "Auto-load", isAuto ? Styles.TabActive : Styles.Button, GUILayout.Width(100)))
                    MenuConfig.AutoLoadPreset.Value = isAuto ? "" : name;
                if (GUILayout.Button("×", Styles.Button, GUILayout.Width(24))) Presets.Delete(name);
                GUILayout.EndHorizontal();
            }
            GUILayout.Label("Auto-load applies the marked preset once when you enter a world.", Styles.Small);
            GUILayout.Space(8);
            GUILayout.Label("Cheat tags", Styles.Header);
            if (Toggle(ref State.HideCheatTags, "Never tag loot / crafts as 'obtained using cheats'")) MenuConfig.HideCheatTags.Value = State.HideCheatTags;
            GUILayout.Label("The game flags you as a cheater when you deal damage in god / ghost / fly mode; this reports the game's own bypass key instead.", Styles.Small);
            GUI.enabled = Player.m_localPlayer != null;
            if (GUILayout.Button("Clear tags on items in inventory", Styles.Button, GUILayout.Width(230))) PlayerCheats.ClearCheatTags();
            GUI.enabled = true;
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

        // ---------------- Credits ----------------

        private static void DrawCredits()
        {
            GUILayout.Label("MjolnirMenu", Styles.Title);
            GUILayout.Label($"v{MyPluginInfo.PLUGIN_VERSION}  •  Valheim trainer / mod menu", Styles.Small);
            GUILayout.Space(10);
            GUILayout.Label("Made by", Styles.Header);
            GUILayout.Label("RGB-Outl4w  —  github.com/RGB-Outl4w/MjolnirMenu", Styles.ListRow);
            GUILayout.Space(10);
            GUILayout.Label("Built with", Styles.Header);
            GUILayout.Label("Claude Code (Anthropic)  —  pair-programmed the whole thing", Styles.ListRow);
            GUILayout.Label("caveman  —  JuliusBrussee/caveman, terse assistant output", Styles.ListRow);
            GUILayout.Label("graphify  —  safishamsi/graphify, codebase knowledge graph", Styles.ListRow);
            GUILayout.Space(10);
            GUILayout.Label("Runs on", Styles.Header);
            GUILayout.Label("BepInEx 5  —  plugin loader (denikson's BepInExPack_Valheim)", Styles.ListRow);
            GUILayout.Label("HarmonyX  —  runtime method patching", Styles.ListRow);
            GUILayout.Label("Krafs.Publicizer  —  access to the game's private fields", Styles.ListRow);
            GUILayout.Space(10);
            GUILayout.Label("Valheim is © Iron Gate Studio. Single-player and your own servers only.", Styles.Small);
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
