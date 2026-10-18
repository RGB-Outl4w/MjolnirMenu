using System.Collections.Generic;
using BepInEx.Configuration;
using MjolnirMenu.Core;
using MjolnirMenu.Features;
using UnityEngine;

namespace MjolnirMenu.UI
{
    public static class MenuWindow
    {
        private const int WindowId = 0x4D4A4F4C; // "MJOL"
        private static readonly string[] Tabs = { "Player", "Combat", "World", "Vehicles", "Skills", "Effects", "Spawner", "Teleport", "ESP", "Settings", "Credits" };

        private const float Col = 340f;      // left column width in two-column tabs
        private const float SliderW = 250f;

        private static Rect _rect = new Rect(60, 60, 800, 560);
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

        // Teleport tab state
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
        private static float _repairRadius = 50f;
        private static float _fogConfirmUntil;
        private static string _renameFor = "", _renameText = "";
        private static float _renameConfirmUntil;
        private static Vector2 _settingsScroll;

        public static void Draw()
        {
            Styles.EnsureBuilt();

            var savedSkin = GUI.skin;
            var savedMatrix = GUI.matrix;
            var savedColor = GUI.color;
            GUI.skin = Styles.Skin;
            try
            {
                if (MenuConfig.ShowWatermark.Value)
                    GUI.Label(new Rect(8, 4, 300, 20), $"MjolnirMenu  [{MenuConfig.MenuKey.Value}]", Styles.Watermark);

                if (!State.MenuOpen) return;
                CaptureRebind();

                float scale = Mathf.Clamp(MenuConfig.UiScale.Value, 0.75f, 1.5f);
                GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
                GUI.color = WindowColor;

                if (!_rectLoaded)
                {
                    _rectLoaded = true;
                    _rect.x = Mathf.Clamp(MenuConfig.WindowX.Value, 0, Mathf.Max(0, Screen.width / scale - _rect.width));
                    _rect.y = Mathf.Clamp(MenuConfig.WindowY.Value, 0, Mathf.Max(0, Screen.height / scale - _rect.height));
                    _tab = Mathf.Clamp(MenuConfig.LastTab.Value, 0, Tabs.Length - 1);
                }

                var before = _rect;
                _rect = GUILayout.Window(WindowId, _rect, DrawWindow, GUIContent.none, Styles.Window);
                if (before.x != _rect.x || before.y != _rect.y)
                {
                    MenuConfig.WindowX.Value = _rect.x;
                    MenuConfig.WindowY.Value = _rect.y;
                }

                // Eat clicks that land on the window so they don't reach the game.
                if (Event.current.isMouse && _rect.Contains(Event.current.mousePosition))
                    Event.current.Use();
            }
            finally
            {
                GUI.skin = savedSkin;
                GUI.matrix = savedMatrix;
                GUI.color = savedColor;
            }
        }

        private static Color WindowColor => new Color(1f, 1f, 1f, Mathf.Clamp(MenuConfig.UiOpacity.Value, 0.4f, 1f));

        /// <summary>Settings → Hotkeys is waiting for a key: take the next non-modifier key press.</summary>
        private static void CaptureRebind()
        {
            var b = Hotkeys.Rebinding;
            var e = Event.current;
            if (b == null || e.type != EventType.KeyDown || e.keyCode == KeyCode.None) return;
            var k = e.keyCode;
            if (k == KeyCode.LeftControl || k == KeyCode.RightControl || k == KeyCode.LeftShift || k == KeyCode.RightShift
                || k == KeyCode.LeftAlt || k == KeyCode.RightAlt || k == KeyCode.LeftCommand || k == KeyCode.RightCommand) return;
            e.Use();
            Hotkeys.Rebinding = null;
            if (k == KeyCode.Escape) return;
            var mods = new List<KeyCode>();
            if (e.control) mods.Add(KeyCode.LeftControl);
            if (e.shift) mods.Add(KeyCode.LeftShift);
            if (e.alt) mods.Add(KeyCode.LeftAlt);
            b.Entry.Value = new KeyboardShortcut(k, mods.ToArray());
        }

        private static void DrawWindow(int id)
        {
            GUI.color = WindowColor;
            // Title bar
            GUILayout.BeginHorizontal(Styles.TitleBar);
            GUILayout.Label("MJOLNIR MENU", Styles.TitleText);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"v{MyPluginInfo.PLUGIN_VERSION}", Styles.TitleVersion);
            GUILayout.EndHorizontal();

            // Tabs
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            for (int i = 0; i < Tabs.Length; i++)
            {
                if (GUILayout.Button(L.T(Tabs[i]), i == _tab ? Styles.TabActive : Styles.Tab) && i != _tab)
                    MenuConfig.LastTab.Value = _tab = i;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(14);

            // Body
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Space(14);
            GUILayout.BeginVertical();
            switch (_tab)
            {
                case 0: DrawPlayer(); break;
                case 1: DrawCombat(); break;
                case 2: DrawWorld(); break;
                case 3: DrawVehicles(); break;
                case 4: DrawSkills(); break;
                case 5: DrawEffects(); break;
                case 6: DrawSpawner(); break;
                case 7: DrawTeleport(); break;
                case 8: DrawEsp(); break;
                case 9: DrawSettings(); break;
                default: DrawCredits(); break;
            }
            GUILayout.EndVertical();
            GUILayout.Space(14);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // Footer
            GUILayout.Space(6);
            Ui.Rule();
            GUILayout.BeginHorizontal();
            GUILayout.Space(14);
            var p = Player.m_localPlayer;
            GUILayout.Label(p != null
                ? $"{p.GetPlayerName()}  •  {L.T(ZNet.instance != null && ZNet.instance.IsServer() ? "local/host" : "client")}"
                : Time.unscaledTime - Hotkeys.LastMessageTime < 6f ? Hotkeys.LastMessage : L.T("Not in game"), Styles.Footer);
            GUILayout.FlexibleSpace();
            if (Ui.Button("Reset all", GUILayout.Width(76)))
            {
                State.ResetAll();
                Hotkeys.Notify("All cheats OFF");
            }
            GUILayout.Space(4);
            if (Ui.Button("Close", GUILayout.Width(60)))
                Hotkeys.SetMenuOpen(false);
            GUILayout.Space(14);
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUI.DragWindow(new Rect(0, 0, 10000, 30));
        }

        // ---------------- Player ----------------

        private static void DrawPlayer()
        {
            bool inGame = Player.m_localPlayer != null;
            if (!inGame) Ui.Hint("Load into a world to use player cheats.");

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(Col));
            Ui.Header("Survival");
            if (Ui.Toggle(ref State.GodMode, "God mode", "No damage of any kind")) PlayerCheats.ApplyGodMode();
            if (Ui.Toggle(ref State.GhostMode, "Ghost mode", "Enemies ignore you")) PlayerCheats.ApplyGhostMode();
            Ui.Toggle(ref State.InfiniteStamina, "Infinite stamina", "Bar never drains");
            Ui.Toggle(ref State.InfiniteEitr, "Infinite eitr", "Magic pool never drains");
            Ui.Toggle(ref State.NoFallDamage, "No fall damage");
            Ui.Toggle(ref State.InfiniteCarryWeight, "Infinite carry weight", "Max weight → 100 000");
            Ui.Toggle(ref State.NoHunger, "No hunger", "Eaten food never runs out; its health and stamina stay full");
            Ui.Toggle(ref State.NoKnockback, "No knockback / stagger", "Hits, broken blocks and parries never push or stun you");
            Ui.Toggle(ref State.MultiEquip, "Multi-equip armor", "Stack helmets, chests, legs, capes; extras add armor + effects, not visuals");
            float rows = State.ExtraInventoryRows;
            Ui.LabeledSlider(L.F("Extra inventory rows  +{0:0}", rows), ref rows, 0f, PlayerCheats.MaxExtraRows, SliderW);
            State.ExtraInventoryRows = Mathf.RoundToInt(rows);
            Ui.Space(6);
            GUI.enabled = inGame;
            GUILayout.BeginHorizontal();
            if (Ui.Button("Heal + refill", GUILayout.Width(120))) PlayerCheats.HealFull();
            if (Ui.Button("Rested now", GUILayout.Width(120))) PlayerCheats.RestNow();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUI.enabled = true;
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            Ui.Header("Movement");
            if (Ui.Toggle(ref State.Fly, "Fly", "Vanilla debug fly — jump = up, crouch = down")) PlayerCheats.ApplyFly();
            Ui.Toggle(ref State.SpeedHack, "Speed hack", L.F("×{0:0.0} on run and jog", State.SpeedMultiplier));
            Ui.Slider(ref State.SpeedMultiplier, 1f, 10f, SliderW);
            if (Ui.Toggle(ref State.JumpHack, "Jump hack", L.F("×{0:0.0} on jump force", State.JumpMultiplier))) PlayerCheats.ApplyJump();
            if (Ui.Slider(ref State.JumpMultiplier, 1f, 10f, SliderW)) PlayerCheats.ApplyJump();
            if (Ui.Toggle(ref State.CrouchSpeedHack, "Crouch speed hack", L.F("×{0:0.0} while sneaking", State.CrouchSpeedMultiplier))) PlayerCheats.ApplyCrouchSpeed();
            if (Ui.Slider(ref State.CrouchSpeedMultiplier, 1f, 10f, SliderW)) PlayerCheats.ApplyCrouchSpeed();
            Ui.Toggle(ref State.CrouchInfiniteStamina, "Infinite stamina while crouched", "Overridden by the global toggle");
            Ui.Toggle(ref State.EmoteSpeedHack, "Sit / stand animation speed", L.F("×{0:0.0} on the emote animator", State.EmoteSpeedMultiplier));
            Ui.Slider(ref State.EmoteSpeedMultiplier, 1f, 5f, SliderW);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            Ui.Space(4);
            Ui.Header("Water");
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(Col));
            if (Ui.Toggle(ref State.SwimSpeedHack, "Swim speed hack", $"×{State.SwimSpeedMultiplier:0.0}")) PlayerCheats.ApplySwimSpeed();
            if (Ui.Slider(ref State.SwimSpeedMultiplier, 1f, 10f, SliderW)) PlayerCheats.ApplySwimSpeed();
            Ui.Toggle(ref State.WaterJump, "Jump while swimming");
            Ui.Toggle(ref State.SwimUseItems, "Use items while swimming", "Weapons and tools stay out; equip, attack and use hotbar in water");
            if (Ui.Toggle(ref State.UnderwaterCamera, "Underwater camera", "Camera follows you below the surface")) PlayerCheats.ApplyCamera();
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            if (Ui.Toggle(ref State.WalkOnWater, "Walk on water", "Surface behaves like ground") && State.WalkOnWater) State.SeabedWalk = false;
            if (Ui.Toggle(ref State.SeabedWalk, "Walk on seabed", "Sink and walk underwater") && State.SeabedWalk) State.WalkOnWater = false;
            Ui.Hint("Water modes pause while Fly is on.");
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        // ---------------- Combat ----------------

        private static void DrawCombat()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(Col));
            Ui.Header("Gear");
            Ui.Toggle(ref State.InfiniteDurability, "Infinite durability", "Weapons, armor and tools never wear");
            Ui.Toggle(ref State.InfiniteItems, "Infinite items", "Ammo, food, mead, fuel, ore and feed are never consumed. Drag-and-drop, dropping and selling stay normal");
            Ui.Toggle(ref State.InfiniteInteract, "Infinite interaction", "Pick-ups, bushes and beehives give you a copy and stay full. Walk-over pickup is off while on");
            Ui.Space();
            Ui.Header("Attack speed");
            Ui.Toggle(ref State.AttackSpeedHack, "Attack speed hack", L.F("×{0:0.0} on attack animations — swings, draws and combos finish faster", State.AttackSpeedMultiplier));
            Ui.Slider(ref State.AttackSpeedMultiplier, 1f, 5f, SliderW);
            Ui.Space();
            Ui.Header("Ranged");
            if (Ui.Toggle(ref State.InstaFocus, "Insta-focus", "Bows are fully drawn the moment you start aiming") && State.InstaFocus) State.InstaShot = false;
            if (Ui.Toggle(ref State.InstaShot, "Insta-shot", State.InstaShotAuto ? "Full-auto: fires at full draw while the key is held" : "Semi-auto: one full-draw shot per press") && State.InstaShot) State.InstaFocus = false;
            Ui.Toggle(ref State.InstaShotAuto, "    Full-auto", "Hold to keep firing; release to stop");
            Ui.Toggle(ref State.InstaReload, "Insta-reload", "Crossbows are loaded the moment they fire");
            Ui.Toggle(ref State.Hitscan, "Hitscan", "Straight line from the bow to where the reticle points — no drop, no spread");
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            Ui.Header("Damage");
            Ui.Toggle(ref State.DamageHack, "Damage multiplier", "Master toggle for every slider below");
            Ui.LabeledSlider(L.F("Melee  ×{0:0.0}", State.MeleeMultiplier), ref State.MeleeMultiplier, 1f, 50f, SliderW);
            Ui.LabeledSlider(L.F("Ranged  ×{0:0.0}", State.RangedMultiplier), ref State.RangedMultiplier, 1f, 50f, SliderW);
            GUILayout.BeginHorizontal();
            GUILayout.Space(Ui.Indent);
            if (Ui.Button("×1")) State.MeleeMultiplier = State.RangedMultiplier = 1f;
            if (Ui.Button("×2")) State.MeleeMultiplier = State.RangedMultiplier = 2f;
            if (Ui.Button("×5")) State.MeleeMultiplier = State.RangedMultiplier = 5f;
            if (Ui.Button("×10")) State.MeleeMultiplier = State.RangedMultiplier = 10f;
            if (Ui.Button("One-shot")) State.MeleeMultiplier = State.RangedMultiplier = 50f;
            GUILayout.EndHorizontal();
            Ui.Space(4);
            Ui.LabeledSlider(L.F("Trees  ×{0:0.0}", State.TreeMultiplier), ref State.TreeMultiplier, 1f, 50f, SliderW);
            Ui.LabeledSlider(L.F("Stones / ores  ×{0:0.0}", State.RockMultiplier), ref State.RockMultiplier, 1f, 50f, SliderW);
            Ui.LabeledSlider(L.F("Player structures  ×{0:0.0}", State.PlayerStructureMultiplier), ref State.PlayerStructureMultiplier, 1f, 50f, SliderW);
            Ui.LabeledSlider(L.F("World structures / destructibles  ×{0:0.0}", State.WorldStructureMultiplier), ref State.WorldStructureMultiplier, 1f, 50f, SliderW);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        // ---------------- Effects ----------------

        private static void DrawEffects()
        {
            if (Player.m_localPlayer == null)
            {
                Ui.Hint("Load into a world to manage effects.");
                return;
            }

            string cur = Effects.CurrentPower();
            Ui.Header(L.F("Forsaken power  —  {0}", string.IsNullOrEmpty(cur) ? L.T("none") : cur));
            GUILayout.BeginHorizontal();
            foreach (var gp in Effects.GuardianPowers())
            {
                if (Ui.Button(Effects.Label(gp), gp.name == cur)) Effects.SelectPower(gp.name);
            }
            if (Ui.Button("None", GUILayout.Width(50))) Effects.SelectPower("");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            Ui.Toggle(ref State.NoPowerCooldown, "No cooldown", null, GUILayout.Width(150));
            Ui.Toggle(ref State.InfinitePower, "Power lasts forever", null, GUILayout.Width(190));
            if (Ui.Button("Activate now", GUILayout.Width(100))) Effects.ActivatePower();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            Ui.Space();
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(390));
            Ui.Header("Active effects");
            _activeScroll = Ui.BeginScroll(_activeScroll, 250f);
            var active = new List<StatusEffect>(Effects.Active());
            if (active.Count == 0) Ui.Hint("None.");
            foreach (var se in active)
            {
                if (se == null) continue;
                bool frozen = Effects.Frozen.Contains(se.NameHash());
                GUILayout.BeginHorizontal();
                GUILayout.Label(Effects.Label(se), Styles.ListRow, GUILayout.Width(130));
                string t = se.m_ttl > 0f ? $"{se.GetRemaningTime():0}s" : "∞";
                Ui.Hint(frozen ? $"{t} ❄" : t, GUILayout.Width(54));
                if (Ui.Button("+60s", GUILayout.Width(46))) Effects.Extend(se, 60f);
                if (Ui.Button("+10m", GUILayout.Width(46))) Effects.Extend(se, 600f);
                if (Ui.Button(frozen ? "Unfreeze" : "Freeze", frozen, GUILayout.Width(68)))
                {
                    if (frozen) Effects.Frozen.Remove(se.NameHash()); else Effects.Frozen.Add(se.NameHash());
                }
                if (Ui.Button("×", GUILayout.Width(26))) Effects.Remove(se);
                GUILayout.EndHorizontal();
            }
            Ui.EndScroll();
            GUILayout.EndVertical();

            GUILayout.Space(16);
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            Ui.Header("All effects");
            GUILayout.Space(8);
            _effectFilter = Ui.TextField(_effectFilter, GUILayout.ExpandWidth(true));
            if (Ui.Button("×", GUILayout.Width(26))) _effectFilter = "";
            GUILayout.EndHorizontal();
            _effectScroll = Ui.BeginScroll(_effectScroll, 250f);
            int shown = 0;
            foreach (var se in Effects.Catalog())
            {
                string label = Effects.Label(se);
                if (_effectFilter.Length > 0
                    && label.IndexOf(_effectFilter, System.StringComparison.OrdinalIgnoreCase) < 0
                    && se.name.IndexOf(_effectFilter, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (++shown > 200) { Ui.Hint("… refine your search"); break; }
                GUILayout.BeginHorizontal();
                GUILayout.Label(label, Styles.ListRow, GUILayout.Width(150));
                Ui.Hint(se.name, GUILayout.ExpandWidth(true));
                if (Ui.Button("Apply", GUILayout.Width(54))) Effects.Apply(se);
                GUILayout.EndHorizontal();
            }
            Ui.EndScroll();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        // ---------------- Skills ----------------

        private static void DrawSkills()
        {
            var skills = SkillCheats.GetSkills();
            if (skills.Count == 0)
            {
                Ui.Hint("Load into a world to edit skills.");
                return;
            }

            GUILayout.BeginHorizontal();
            if (Ui.Button("Max all (100)", GUILayout.Width(110))) SkillCheats.MaxAll();
            if (Ui.Button("Reset all (0)", GUILayout.Width(110))) SkillCheats.ResetAll();
            GUILayout.Space(16);
            Ui.Hint("Set all to", GUILayout.Width(60));
            _allSkillsText = Ui.TextField(_allSkillsText, GUILayout.Width(56));
            if (Ui.Button("Apply", GUILayout.Width(60))) SkillCheats.SetAll(ParseFloat(_allSkillsText, 0f));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            Ui.Space(6);

            _skillScroll = Ui.BeginScroll(_skillScroll, 380f);
            foreach (var s in skills)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(SkillCheats.Name(s), Styles.ListRow, GUILayout.Width(150));
                Ui.Hint($"{s.m_level:0}", GUILayout.Width(36));
                float lvl = s.m_level;
                GUILayout.BeginVertical(GUILayout.Width(260));
                GUILayout.Space(5);
                if (Ui.Slider(ref lvl, 0f, SkillCheats.MaxLevel, 250f, false)) SkillCheats.SetLevel(s, lvl);
                GUILayout.EndVertical();
                GUILayout.Space(8);
                if (Ui.Button("0", GUILayout.Width(32))) SkillCheats.SetLevel(s, 0f);
                if (Ui.Button("50", GUILayout.Width(38))) SkillCheats.SetLevel(s, 50f);
                if (Ui.Button("100", GUILayout.Width(44))) SkillCheats.SetLevel(s, 100f);
                GUILayout.EndHorizontal();
            }
            Ui.EndScroll();
        }

        // ---------------- World ----------------

        private static void DrawVehicles()
        {
            bool aboard = Ship.GetLocalShip() != null;
            bool hitched = Vehicles.Cart != null;
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(Col));
            Ui.Header(L.F("Ship  —  {0}", L.T(aboard ? "aboard" : "not aboard")));
            Ui.Toggle(ref State.ShipTailwind, "Tailwind", "At the helm: full wind wherever you look; waves stay natural");
            Ui.Toggle(ref State.ShipSpeedHack, "Ship speed hack", L.F("At the helm: ×{0:0.0} sail and paddle force", State.ShipSpeedMultiplier));
            Ui.Slider(ref State.ShipSpeedMultiplier, 1f, 20f, SliderW);
            Ui.Toggle(ref State.ShipSteering, "Responsive steering", L.F("At the helm: ×{0:0.0} rudder speed and turning force", State.ShipSteeringMultiplier));
            Ui.Slider(ref State.ShipSteeringMultiplier, 1f, 10f, SliderW);
            Ui.Space(6);
            GUI.enabled = Player.m_localPlayer != null;
            if (Ui.Button("Flip nearest ship upright", GUILayout.Width(190)))
                Hotkeys.Notify(Vehicles.FlipNearestShip() ? "Ship flipped upright" : "No ship within 50 m");
            GUI.enabled = true;
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            Ui.Header(L.F("Cart  —  {0}", L.T(hitched ? "hitched" : "not hitched")));
            Ui.Toggle(ref State.CartSpeedHack, "Cart speed hack", "Cart weighs nothing and follows at full run speed — pair with Speed hack");
            Ui.Toggle(ref State.CartNoClip, "Cart no-clip", "Rolls through trees, rocks and mobs; still rides terrain and floors");
            Ui.Toggle(ref State.CartSticky, "Sticky hitch", "Never unhitches on its own; use the cart again to let go");
            Ui.Toggle(ref State.CartSnap, "Hitch from anywhere", "Use the cart from any side — it swings into place behind you");
            Ui.Toggle(ref State.CartRidesShips, "Carts ride ships", "Loose carts on a deck move with the ship instead of rolling aft");
            Ui.Space(6);
            Ui.Header("Ships and carts");
            Ui.Toggle(ref State.VehicleGod, "Hull immunity", "No collision, wave-slam or capsize damage; deals none either");
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private static void DrawWorld()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(Col));
            Ui.Header("Build & craft");
            Ui.Toggle(ref State.FreeBuild, "Free build", "No piece cost");
            Ui.Toggle(ref State.NoPlaceDelay, "No placement delay");
            Ui.Toggle(ref State.FreeCraft, "Free crafting", "No materials");
            Ui.Toggle(ref State.InstantCraft, "Instant crafting");
            Ui.Toggle(ref State.BigStations, "Big stations", L.F("×{0:0} capacity for smelters, kilns, fires, beehives, sap collectors", State.StationMultiplier));
            Ui.Slider(ref State.StationMultiplier, 1f, 50f, SliderW);
            Ui.Toggle(ref State.InstantStations, "Instant stations", "Smelters, kilns, windmills: 1 item/s. Fermenters 2 s. Cooking 1 s, never burns");
            Ui.Toggle(ref State.InstantGrowth, "Instant growth", "Crops and saplings grow the moment their spot is valid");
            Ui.LabeledSlider(L.F("Repair radius  {0:0} m", _repairRadius), ref _repairRadius, 10f, 200f, SliderW);
            GUILayout.BeginHorizontal();
            GUILayout.Space(Ui.Indent);
            GUI.enabled = Player.m_localPlayer != null;
            if (Ui.Button("Repair all nearby", GUILayout.Width(150))) WorldCheats.RepairNearby(_repairRadius);
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            Ui.Space();
            Ui.Header("Time of day");
            Ui.Toggle(ref State.LockTimeOfDay, "Lock time", WorldCheats.CurrentTimeLabel());
            if (Ui.Slider(ref State.TimeOfDay, 0f, 1f, SliderW)) State.LockTimeOfDay = true;
            GUILayout.BeginHorizontal();
            GUILayout.Space(Ui.Indent);
            if (Ui.Button("Dawn")) { State.TimeOfDay = 0.25f; State.LockTimeOfDay = true; }
            if (Ui.Button("Noon")) { State.TimeOfDay = 0.5f; State.LockTimeOfDay = true; }
            if (Ui.Button("Dusk")) { State.TimeOfDay = 0.75f; State.LockTimeOfDay = true; }
            if (Ui.Button("Night")) { State.TimeOfDay = 0.0f; State.LockTimeOfDay = true; }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            Ui.Header(L.F("Weather  —  {0}", string.IsNullOrEmpty(State.ForcedWeather) ? L.T("natural") : State.ForcedWeather));
            if (Ui.Button("Natural weather", string.IsNullOrEmpty(State.ForcedWeather), GUILayout.Width(140)))
                State.ForcedWeather = "";
            Ui.Space(4);
            _weatherScroll = Ui.BeginScroll(_weatherScroll, 300f);
            var names = WorldCheats.GetWeatherNames();
            if (names.Count == 0) Ui.Hint("Load into a world first.");
            GUILayout.BeginHorizontal();
            int col = 0;
            foreach (var n in names)
            {
                if (Ui.Button(n, n == State.ForcedWeather, GUILayout.Width(130))) State.ForcedWeather = n;
                if (++col % 3 == 0) { GUILayout.EndHorizontal(); GUILayout.BeginHorizontal(); }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            Ui.EndScroll();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        // ---------------- Spawner ----------------

        private static void DrawSpawner()
        {
            if (!Spawner.EnsureCatalog())
            {
                Ui.Hint("Catalog loads once you are in a world.");
                return;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(L.T("Items"), !_showCreatures ? Styles.TabActive : Styles.Tab, GUILayout.MinWidth(90))) _showCreatures = false;
            if (GUILayout.Button(L.T("Creatures"), _showCreatures ? Styles.TabActive : Styles.Tab, GUILayout.MinWidth(90))) _showCreatures = true;
            GUILayout.Space(10);
            if (!_showCreatures) Ui.Toggle(ref State.SpawnerShowAll, "Show all", null, GUILayout.Width(110));
            else if (Morph.Active && Ui.Button(L.F("Morph back ({0})", Morph.Current), true)) Morph.Back();
            GUILayout.FlexibleSpace();
            Ui.Hint("Search", GUILayout.Width(44));
            GUI.SetNextControlName("mjolnir_search");
            _filter = Ui.TextField(_filter, GUILayout.Width(220));
            if (Ui.Button("×", GUILayout.Width(26))) _filter = "";
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (!_showCreatures)
            {
                Ui.Hint("Stack", GUILayout.Width(40));
                _stackText = Ui.TextField(_stackText, GUILayout.Width(56));
                GUILayout.Space(8);
                Ui.Hint("Quality", GUILayout.Width(50));
                _qualityText = Ui.TextField(_qualityText, GUILayout.Width(44));
            }
            else
            {
                Ui.Hint("Level", GUILayout.Width(40));
                _levelText = Ui.TextField(_levelText, GUILayout.Width(44));
                GUILayout.Space(8);
                Ui.Hint("Count", GUILayout.Width(40));
                _countText = Ui.TextField(_countText, GUILayout.Width(44));
                GUILayout.Space(8);
                Ui.Toggle(ref _tamed, "Tamed", null, GUILayout.Width(90));
                Ui.Toggle(ref State.MorphSpoof, "Spoof morph", "Others see a real wild creature: its own name, no trace of you", GUILayout.Width(150));
                Ui.Hint("Any level; tamed works on bosses too. Morph uses Level too");
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            var source = _showCreatures ? Spawner.Creatures : Spawner.Items;
            _listScroll = Ui.BeginScroll(_listScroll, 270f);
            int shown = 0;
            foreach (var e in source)
            {
                if (!_showCreatures && !State.SpawnerShowAll && !e.InventorySafe) continue;
                if (!Spawner.Matches(e, _filter)) continue;
                if (++shown > 300) { Ui.Hint("… refine your search"); break; }
                GUILayout.BeginHorizontal();
                string rowName = e.IsBoss ? $"★ {e.DisplayName}" : e.DisplayName;
                if (!e.IsCreature && !e.InventorySafe) rowName += $"  <color={Styles.HintHex}>{L.T("(not an inventory item)")}</color>";
                GUILayout.Label(rowName, Styles.ListRow, GUILayout.Width(260));
                Ui.Hint(e.PrefabName, GUILayout.ExpandWidth(true));
                if (_showCreatures && Ui.Button("Morph", Morph.Current == e.DisplayName, GUILayout.Width(70))) Morph.To(e.PrefabName, e.DisplayName, ParseInt(_levelText, 1));
                if (Ui.Button(_showCreatures ? "Spawn" : "Give", GUILayout.Width(60)))
                {
                    if (_showCreatures)
                        Spawner.SpawnCreature(e.PrefabName, ParseInt(_levelText, 1), ParseInt(_countText, 1), _tamed);
                    else
                        Spawner.GiveItem(e.PrefabName, ParseInt(_stackText, 1), ParseInt(_qualityText, 1));
                }
                GUILayout.EndHorizontal();
            }
            Ui.EndScroll();

            Ui.Space(6);
            Ui.Rule();
            Ui.Space(4);
            GUILayout.BeginHorizontal();
            Ui.Header(L.F("Kill nearby  —  {0:0} m", _killRadius));
            GUILayout.Space(8);
            GUILayout.BeginVertical(GUILayout.Width(180));
            GUILayout.Space(4);
            Ui.Slider(ref _killRadius, 5f, 200f, 170f, false);
            GUILayout.EndVertical();
            Ui.Toggle(ref _killTamed, "incl. tamed", null, GUILayout.Width(120));
            if (Ui.Button("Kill", GUILayout.Width(60))) Spawner.KillNearby(_killRadius, _killTamed);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        // ---------------- Teleport ----------------

        private static void DrawTeleport()
        {
            Warp.EnsureLoaded();
            bool inGame = Player.m_localPlayer != null;

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(Col));
            Ui.Header("Map");
            Ui.Toggle(ref State.MapClickTeleport, "Ctrl + left-click on the big map to teleport");
            Ui.Toggle(ref State.InstantTeleport, "Instant menu teleports", "Map click / saved positions / players: no fade, no vortex wait");
            Ui.Toggle(ref State.PortalAnyItem, "Portals accept any item", "Take ore, eggs and other non-teleportable items through portals");
            Ui.Toggle(ref State.FastTeleport, "Fast portals", L.F("×{0:0} on the vortex timer for real portals", State.TeleportSpeed));
            float ts = State.TeleportSpeed;
            if (Ui.Slider(ref ts, 1f, 40f, SliderW)) State.TeleportSpeed = Mathf.Round(ts);
            Ui.Space(6);
            GUI.enabled = inGame;
            GUILayout.BeginHorizontal();
            if (Ui.Button("Start temple")) Warp.ToStart();
            if (Ui.Button("Bed / spawn point")) Warp.ToSpawnPoint();
            if (Ui.Button("Death point")) Warp.ToDeathPoint();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (Ui.Button("Reveal map")) MapFog.Reveal();
            bool confirm = Time.unscaledTime < _fogConfirmUntil;
            if (Ui.Button(confirm ? "Click again: erase exploration" : "Restore fog", confirm))
            {
                if (confirm) { MapFog.Restore(); _fogConfirmUntil = 0f; }
                else _fogConfirmUntil = Time.unscaledTime + 3f;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            Ui.Space();
            Ui.Header("Boss altars");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < Warp.BossAltars.GetLength(0); i++)
            {
                if (i == 4) { GUILayout.FlexibleSpace(); GUILayout.EndHorizontal(); GUILayout.BeginHorizontal(); }
                if (Ui.Button(Warp.BossAltars[i, 0])) Warp.ToLocation(Warp.BossAltars[i, 0], Warp.BossAltars[i, 1]);
            }
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            Ui.Space();
            Ui.Header("Players");
            var others = Warp.OtherPlayers();
            if (others.Count == 0) Ui.Hint("No other players.");
            foreach (var pi in others)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(pi.m_name, Styles.ListRow, GUILayout.ExpandWidth(true));
                GUI.enabled = inGame && pi.m_publicPosition;
                if (Ui.Button(pi.m_publicPosition ? "Go" : "hidden", GUILayout.Width(60)))
                    Warp.To(pi.m_position + Vector3.up * 1f);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            Ui.Header("Saved positions");
            GUILayout.BeginHorizontal();
            _posName = Ui.TextField(_posName, GUILayout.ExpandWidth(true));
            GUI.enabled = inGame;
            if (Ui.Button("Save here", GUILayout.Width(84))) { Warp.SaveCurrent(_posName); _posName = ""; }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            Ui.Space(4);
            _tpScroll = Ui.BeginScroll(_tpScroll, 300f);
            Warp.SavedPos? toRemove = null;
            foreach (var sp in Warp.Saved)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(sp.Name, Styles.ListRow, GUILayout.Width(150));
                Ui.Hint($"{sp.Pos.x:0} / {sp.Pos.z:0}", GUILayout.ExpandWidth(true));
                GUI.enabled = inGame;
                if (Ui.Button("Go", GUILayout.Width(40))) Warp.To(sp.Pos);
                GUI.enabled = true;
                if (Ui.Button("×", GUILayout.Width(26))) toRemove = sp;
                GUILayout.EndHorizontal();
            }
            if (toRemove != null) Warp.Remove(toRemove);
            Ui.EndScroll();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        // ---------------- ESP ----------------

        private static void DrawEsp()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(Col));
            Ui.Header("Show");
            Ui.Toggle(ref State.EspPlayers, "Players");
            Ui.Toggle(ref State.EspCreatures, "Creatures & bosses", "Name, level, HP");
            Ui.Toggle(ref State.EspResources, "Resources", "Pickables, ore, trees — heavier");
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            Ui.Header("Style");
            Ui.Toggle(ref State.EspDistance, "Distance");
            Ui.Toggle(ref State.EspLines, "Tracer lines");
            Ui.Space(4);
            float range = MenuConfig.EspRange.Value;
            Ui.LabeledSlider(L.F("Range  {0:0} m", range), ref range, 20f, 1000f, SliderW);
            if (!Mathf.Approximately(range, MenuConfig.EspRange.Value)) MenuConfig.EspRange.Value = range;
            float cap = MenuConfig.EspMaxEntities.Value;
            Ui.LabeledSlider(L.F("Max labels  {0:0}", cap), ref cap, 10f, 2000f, SliderW);
            if (Mathf.RoundToInt(cap) != MenuConfig.EspMaxEntities.Value) MenuConfig.EspMaxEntities.Value = Mathf.RoundToInt(cap);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        // ---------------- Settings ----------------

        private static void DrawSettings()
        {
            _settingsScroll = Ui.BeginScroll(_settingsScroll, 420f);

            Ui.Header("Character");
            var sel = Rename.Selected;
            if (sel == null)
                Ui.Hint("Rename your character from the main menu (character select). In a world the name is in use and can't be changed safely.");
            else
            {
                if (_renameFor != sel.m_filename) { _renameFor = sel.m_filename; _renameText = sel.GetName(); }
                GUILayout.BeginHorizontal();
                Ui.Label(sel.GetName(), GUILayout.Width(150));
                _renameText = Ui.TextField(_renameText, GUILayout.Width(170));
                string? err = Rename.Validate(_renameText);
                GUI.enabled = err == null && _renameText.Trim().ToLowerInvariant() != sel.GetName();
                bool confirm = Time.unscaledTime < _renameConfirmUntil;
                if (Ui.Button(confirm ? "Click again to rename" : "Rename", confirm, GUILayout.Width(170)))
                {
                    if (confirm) { Rename.Apply(_renameText); _renameConfirmUntil = 0f; }
                    else _renameConfirmUntil = Time.unscaledTime + 3f;
                }
                GUI.enabled = true;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                Ui.Hint(err ?? "Only the shown name changes. Save file, player ID and everything you own stay the same. A backup (.fch.mjolnir-rename.bak) is written first.");
            }

            Ui.Space();

            Ui.Header("Hotkeys");
            foreach (var b in Hotkeys.All)
            {
                GUILayout.BeginHorizontal();
                Ui.Label(b.Name, GUILayout.Width(190));
                bool waiting = Hotkeys.Rebinding == b;
                var key = b.Entry.Value;
                string text = waiting ? "Press a key… (Esc cancels)" : key.MainKey == KeyCode.None ? "not set" : key.ToString();
                if (Ui.Button(text, waiting, GUILayout.Width(220))) Hotkeys.Rebinding = waiting ? null : b;
                GUI.enabled = b.CanClear;
                if (Ui.Button("×", GUILayout.Width(26))) b.Entry.Value = KeyboardShortcut.Empty;
                GUI.enabled = true;
                if (Ui.Button("Default", GUILayout.Width(80))) b.Entry.Value = (KeyboardShortcut)b.Entry.DefaultValue;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            Ui.Hint("Click a key, then press the new one. Ctrl / Shift / Alt held at the same time become part of the shortcut.");

            Ui.Space();
            Ui.Header("Language");
            GUILayout.BeginHorizontal();
            foreach (var lang in new[] { "Auto", "English", "Russian" })
            {
                string label = lang == "Auto" ? L.T("Auto (game language)") : lang == "Russian" ? "Русский" : "English";
                if (GUILayout.Button(label, MenuConfig.Language.Value == lang ? Styles.ButtonActive : Styles.Button)) MenuConfig.Language.Value = lang;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            Ui.Space();
            Ui.Header("Window");
            GUILayout.BeginHorizontal();
            Ui.Hint(L.F("Scale  {0:0}%", MenuConfig.UiScale.Value * 100f), GUILayout.Width(110));
            if (Ui.Button("−", GUILayout.Width(28))) StepScale(-0.05f);
            if (Ui.Button("+", GUILayout.Width(28))) StepScale(0.05f);
            if (Ui.Button("100%", GUILayout.Width(54))) MenuConfig.UiScale.Value = 1f;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            float op = MenuConfig.UiOpacity.Value;
            Ui.LabeledSlider(L.F("Opacity  {0:0}%", op * 100f), ref op, 0.4f, 1f, SliderW);
            if (!Mathf.Approximately(op, MenuConfig.UiOpacity.Value)) MenuConfig.UiOpacity.Value = op;
            bool wm = MenuConfig.ShowWatermark.Value;
            if (Ui.Toggle(ref wm, "Show watermark", "Small MjolnirMenu tag in the top-left corner")) MenuConfig.ShowWatermark.Value = wm;

            Ui.Space();
            Ui.Header("Presets");
            Ui.Hint("BepInEx/config/MjolnirMenu.presets — auto-load applies the marked preset once when you enter a world.");
            GUILayout.BeginHorizontal();
            _presetName = Ui.TextField(_presetName, GUILayout.Width(200));
            if (Ui.Button("Save current", GUILayout.Width(100))) Presets.Save(_presetName);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            string auto = MenuConfig.AutoLoadPreset.Value;
            foreach (var name in Presets.List())
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(name, Styles.ListRow, GUILayout.Width(200));
                if (Ui.Button("Load", GUILayout.Width(54))) Presets.Load(name);
                bool isAuto = name == auto;
                if (Ui.Button(isAuto ? "Auto-load: ON" : "Auto-load", isAuto, GUILayout.Width(104)))
                    MenuConfig.AutoLoadPreset.Value = isAuto ? "" : name;
                if (Ui.Button("×", GUILayout.Width(26))) Presets.Delete(name);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            Ui.Space();
            Ui.Header("Cheat tags");
            if (Ui.Toggle(ref State.HideCheatTags, "Never tag loot / crafts as \"obtained using cheats\"",
                    "The game flags you as a cheater when you deal damage in god / ghost / fly mode; this reports the game's own bypass key instead"))
                MenuConfig.HideCheatTags.Value = State.HideCheatTags;
            GUI.enabled = Player.m_localPlayer != null;
            if (Ui.Button("Clear tags on items in inventory", GUILayout.Width(230))) PlayerCheats.ClearCheatTags();
            GUI.enabled = true;

            Ui.Space();
            Ui.Header("Defaults");
            if (Ui.Button("Save speed / jump as default", GUILayout.Width(210)))
            {
                MenuConfig.SpeedMultiplier.Value = State.SpeedMultiplier;
                MenuConfig.JumpMultiplier.Value = State.JumpMultiplier;
                Hotkeys.Notify("Defaults saved");
            }

            Ui.Space(12);
            Ui.Hint("MjolnirMenu is meant for single-player and servers you own. Respect other people's servers.");
            Ui.EndScroll();
        }

        // ---------------- Credits ----------------

        private static void DrawCredits()
        {
            GUILayout.Label("MjolnirMenu", Styles.Title);
            Ui.Hint($"v{MyPluginInfo.PLUGIN_VERSION}  •  {L.T("Valheim trainer / mod menu")}  •  rgb-outl4w.github.io/MjolnirMenu");
            Ui.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(Col));
            Ui.Header("Made by");
            GUILayout.Label("RGB-Outl4w", Styles.ListRow);
            Ui.Hint("github.com/RGB-Outl4w/MjolnirMenu");
            Ui.Space(10);
            Ui.Header("Built with");
            GUILayout.Label("Claude Code", Styles.ListRow); Ui.Hint("Anthropic — pair-programmed the whole thing");
            GUILayout.Label("caveman", Styles.ListRow); Ui.Hint("JuliusBrussee/caveman — terse assistant output");
            GUILayout.Label("graphify", Styles.ListRow); Ui.Hint("safishamsi/graphify — codebase knowledge graph");
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            Ui.Header("Runs on");
            GUILayout.Label("BepInEx 5", Styles.ListRow); Ui.Hint("plugin loader (denikson's BepInExPack_Valheim)");
            GUILayout.Label("HarmonyX", Styles.ListRow); Ui.Hint("runtime method patching");
            GUILayout.Label("Krafs.Publicizer", Styles.ListRow); Ui.Hint("access to the game's private fields");
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            Ui.Space(10);
            Ui.Hint("Valheim is © Iron Gate Studio. Single-player and your own servers only.");
        }

        // ---------------- helpers ----------------

        // Buttons, not a slider: a slider would rescale the window under the cursor while you drag it.
        private static void StepScale(float d)
            => MenuConfig.UiScale.Value = Mathf.Clamp(Mathf.Round((MenuConfig.UiScale.Value + d) * 20f) / 20f, 0.75f, 1.5f);

        private static int ParseInt(string s, int fallback)
            => int.TryParse(s, out var v) ? v : fallback;

        private static float ParseFloat(string s, float fallback)
            => float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : fallback;
    }
}
