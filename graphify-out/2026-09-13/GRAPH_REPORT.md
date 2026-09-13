# Graph Report - .  (2026-09-13)

## Corpus Check
- Corpus is ~6,187 words - fits in a single context window. You may not need a graph.

## Summary
- 209 nodes · 354 edges · 13 communities
- Extraction: 92% EXTRACTED · 8% INFERRED · 0% AMBIGUOUS · INFERRED: 28 edges (avg confidence: 0.91)
- Token cost: 60,000 input · 5,500 output

## Community Hubs (Navigation)
- [[_COMMUNITY_BepInEx Plugin Bootstrap & Install|BepInEx Plugin Bootstrap & Install]]
- [[_COMMUNITY_Item & Creature Spawner|Item & Creature Spawner]]
- [[_COMMUNITY_Teleport & Saved Positions|Teleport & Saved Positions]]
- [[_COMMUNITY_Namespace & File Layout|Namespace & File Layout]]
- [[_COMMUNITY_Player Cheats & State Reset|Player Cheats & State Reset]]
- [[_COMMUNITY_ESP Overlay Rendering|ESP Overlay Rendering]]
- [[_COMMUNITY_Menu Tabs & Project Concepts|Menu Tabs & Project Concepts]]
- [[_COMMUNITY_Player Harmony Patches|Player Harmony Patches]]
- [[_COMMUNITY_Build Configuration|Build Configuration]]
- [[_COMMUNITY_IMGUI Skin Styles|IMGUI Skin Styles]]
- [[_COMMUNITY_Character Damage Patch|Character Damage Patch]]

## God Nodes (most connected - your core abstractions)
1. `MenuWindow` - 19 edges
2. `MjolnirMenu.Core` - 15 edges
3. `PlayerCheats` - 14 edges
4. `Warp` - 14 edges
5. `PlayerPatches` - 12 edges
6. `State` - 11 edges
7. `MjolnirMenu.Features` - 11 edges
8. `Esp` - 11 edges
9. `Spawner` - 11 edges
10. `WorldCheats` - 11 edges

## Surprising Connections (you probably didn't know these)
- `ESP Tab (player/creature/resource labels, tracers, range/cap)` --references--> `Esp`  [INFERRED]
  README.md → src/MjolnirMenu/Features/Esp.cs
- `Spawner Tab (ObjectDB items, ZNetScene creatures, kill radius)` --references--> `Spawner`  [INFERRED]
  README.md → src/MjolnirMenu/Features/Spawner.cs
- `Fly` --references--> `PlayerCheats`  [INFERRED]
  README.md → src/MjolnirMenu/Features/PlayerCheats.cs
- `God Mode` --references--> `PlayerCheats`  [INFERRED]
  README.md → src/MjolnirMenu/Features/PlayerCheats.cs
- `Per-frame reconciliation of game fields against State` --rationale_for--> `PlayerCheats`  [INFERRED]
  README.md → src/MjolnirMenu/Features/PlayerCheats.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Menu tabs of the MjolnirMenu IMGUI window** — readme_player_tab, readme_world_tab, readme_spawner_tab, readme_teleport_tab, readme_esp_tab, readme_settings_tab [EXTRACTED 1.00]
- **State-driven cheat flow: State read by patches, reconciled by features every frame** — src_mjolnirmenu_core_state_mjolnirmenu_core_state, src_mjolnirmenu_features_playercheats_mjolnirmenu_features_playercheats, src_mjolnirmenu_features_worldcheats_mjolnirmenu_features_worldcheats, src_mjolnirmenu_patches_playerpatches_mjolnirmenu_patches_playerpatches, readme_state_single_source_of_truth [INFERRED 0.85]
- **Build and install pipeline (BepInEx install, dotnet build, deploy)** — readme_build_process, readme_install_process, readme_install_bepinex_script, readme_deploy_script, readme_krafs_publicizer [EXTRACTED 1.00]

## Communities (13 total, 0 thin omitted)

### Community 0 - "BepInEx Plugin Bootstrap & Install"
Cohesion: 0.08
Nodes (27): BaseUnityPlugin, ConfigEntry, ConfigFile, Harmony, ManualLogSource, Auto-disable cheats on logout, BepInEx 5, Build (dotnet build, VALHEIM_DIR, Managed assemblies, BepInEx NuGet feed) (+19 more)

### Community 1 - "Item & Creature Spawner"
Cohesion: 0.11
Nodes (14): Entry, GameObject, int, Rect, bool, List, string, Entry (+6 more)

### Community 2 - "Teleport & Saved Positions"
Cohesion: 0.13
Nodes (14): Minimap, PlayerInfo, MjolnirMenu.positions.txt (persisted named positions), Teleport Tab (map click, temple/bed, saved positions, player teleport), SavedPos, bool, List, string (+6 more)

### Community 3 - "Namespace & File Layout"
Cohesion: 0.14
Nodes (9): MjolnirMenu.UI, MjolnirMenu.Patches, MjolnirMenu, MjolnirMenu.Core, MjolnirMenu.Features, Kind, HarmonyPatch, HarmonyPostfix (+1 more)

### Community 4 - "Player Cheats & State Reset"
Cohesion: 0.18
Nodes (9): Fly, God Mode, Player Tab (God mode, ghost, infinite stamina/eitr, fly, speed/jump hack), float, Player, PlayerCheats, HarmonyPatch, HarmonyPrefix (+1 more)

### Community 5 - "ESP Overlay Rendering"
Cohesion: 0.13
Nodes (13): Camera, Kind, Color, float, GUIStyle, List, string, Texture2D (+5 more)

### Community 6 - "Menu Tabs & Project Concepts"
Cohesion: 0.11
Nodes (15): InventoryGui, caveman (Claude Code helper), ESP Tab (player/creature/resource labels, tracers, range/cap), Ethical use notice (own/permitted servers only, no anti-detection), graphify (knowledge graph tooling), Tabbed IMGUI Menu Window, MjolnirMenu, Spawner Tab (ObjectDB items, ZNetScene creatures, kill radius) (+7 more)

### Community 7 - "Player Harmony Patches"
Cohesion: 0.38
Nodes (5): HarmonyPatch, HarmonyPostfix, HarmonyPrefix, Player, PlayerPatches

### Community 8 - "Build Configuration"
Cohesion: 0.22
Nodes (7): net462, BepInEx.Core (5.4.21), BepInEx.PluginInfoProps (2.1.0), HarmonyX (2.10.2), Krafs.Publicizer (2.2.1), Microsoft.NETFramework.ReferenceAssemblies (1.0.3), Microsoft.NET.Sdk

### Community 9 - "IMGUI Skin Styles"
Cohesion: 0.28
Nodes (5): bool, Color, GUIStyle, Texture2D, Styles

### Community 10 - "Character Damage Patch"
Cohesion: 0.33
Nodes (5): Character, HitData, HarmonyPatch, HarmonyPrefix, CharacterPatches

## Knowledge Gaps
- **13 isolated node(s):** `Kind`, `net462`, `BepInEx.Core (5.4.21)`, `BepInEx.PluginInfoProps (2.1.0)`, `HarmonyX (2.10.2)` (+8 more)
  These have ≤1 connection - possible missing edges or undocumented components.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `MjolnirMenu.Core` connect `Namespace & File Layout` to `BepInEx Plugin Bootstrap & Install`?**
  _High betweenness centrality (0.219) - this node is a cross-community bridge._
- **Why does `MenuWindow` connect `Item & Creature Spawner` to `BepInEx Plugin Bootstrap & Install`, `Teleport & Saved Positions`, `Namespace & File Layout`, `Player Cheats & State Reset`, `Menu Tabs & Project Concepts`, `IMGUI Skin Styles`?**
  _High betweenness centrality (0.200) - this node is a cross-community bridge._
- **Why does `Tabbed IMGUI Menu Window` connect `Menu Tabs & Project Concepts` to `BepInEx Plugin Bootstrap & Install`, `Item & Creature Spawner`, `Teleport & Saved Positions`, `Player Cheats & State Reset`?**
  _High betweenness centrality (0.129) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `MenuWindow` (e.g. with `Tabbed IMGUI Menu Window` and `Source Layout (Plugin, Core, Features, Patches, UI)`) actually correct?**
  _`MenuWindow` has 2 INFERRED edges - model-reasoned connections that need verification._
- **Are the 4 inferred relationships involving `PlayerCheats` (e.g. with `Fly` and `God Mode`) actually correct?**
  _`PlayerCheats` has 4 INFERRED edges - model-reasoned connections that need verification._
- **Are the 2 inferred relationships involving `Warp` (e.g. with `MjolnirMenu.positions.txt (persisted named positions)` and `Teleport Tab (map click, temple/bed, saved positions, player teleport)`) actually correct?**
  _`Warp` has 2 INFERRED edges - model-reasoned connections that need verification._
- **Are the 2 inferred relationships involving `PlayerPatches` (e.g. with `Harmony patches on Player, Character, Minimap, EnvMan, Game` and `Player Tab (God mode, ghost, infinite stamina/eitr, fly, speed/jump hack)`) actually correct?**
  _`PlayerPatches` has 2 INFERRED edges - model-reasoned connections that need verification._