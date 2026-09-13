# Graph Report - MjolnirMenu  (2026-09-13)

## Corpus Check
- 22 files · ~7,505 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 247 nodes · 380 edges · 41 communities (14 shown, 27 thin omitted)
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `1ca58f21`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

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
- [[_COMMUNITY_MjolnirMenu|MjolnirMenu]]
- [[_COMMUNITY_Auto-disable cheats on logout|Auto-disable cheats on logout]]
- [[_COMMUNITY_BepInEx 5|BepInEx 5]]
- [[_COMMUNITY_Build (dotnet build, VALHEIM_DIR, Managed assemblies, BepInEx NuGet feed)|Build (dotnet build, VALHEIM_DIR, Managed assemblies, BepInEx NuGet feed)]]
- [[_COMMUNITY_caveman (Claude Code helper)|caveman (Claude Code helper)]]
- [[_COMMUNITY_scriptsdeploy.ps1|scripts/deploy.ps1]]
- [[_COMMUNITY_ESP Tab (playercreatureresource labels, tracers, rangecap)|ESP Tab (player/creature/resource labels, tracers, range/cap)]]
- [[_COMMUNITY_Ethical use notice (ownpermitted servers only, no anti-detection)|Ethical use notice (own/permitted servers only, no anti-detection)]]
- [[_COMMUNITY_Fly|Fly]]
- [[_COMMUNITY_God Mode|God Mode]]
- [[_COMMUNITY_graphify (knowledge graph tooling)|graphify (knowledge graph tooling)]]
- [[_COMMUNITY_Harmony patches on Player, Character, Minimap, EnvMan, Game|Harmony patches on Player, Character, Minimap, EnvMan, Game]]
- [[_COMMUNITY_HarmonyX|HarmonyX]]
- [[_COMMUNITY_Hotkeys (Insert, F6, F7, End) in com.mjolnir.menu.cfg|Hotkeys (Insert, F6, F7, End) in com.mjolnir.menu.cfg]]
- [[_COMMUNITY_Tabbed IMGUI Menu Window|Tabbed IMGUI Menu Window]]
- [[_COMMUNITY_scriptsinstall-bepinex.ps1|scripts/install-bepinex.ps1]]
- [[_COMMUNITY_Install (BepInExPack_Valheim, install-bepinex.ps1, deploy.ps1)|Install (BepInExPack_Valheim, install-bepinex.ps1, deploy.ps1)]]
- [[_COMMUNITY_Krafs.Publicizer|Krafs.Publicizer]]
- [[_COMMUNITY_Panic Key (End turn every cheat off)|Panic Key (End: turn every cheat off)]]
- [[_COMMUNITY_Per-frame reconciliation of game fields against State|Per-frame reconciliation of game fields against State]]
- [[_COMMUNITY_Player Tab (God mode, ghost, infinite staminaeitr, fly, speedjump hack)|Player Tab (God mode, ghost, infinite stamina/eitr, fly, speed/jump hack)]]
- [[_COMMUNITY_MjolnirMenu.positions.txt (persisted named positions)|MjolnirMenu.positions.txt (persisted named positions)]]
- [[_COMMUNITY_Settings Tab (hotkey overview, watermark, save defaults)|Settings Tab (hotkey overview, watermark, save defaults)]]
- [[_COMMUNITY_Spawner Tab (ObjectDB items, ZNetScene creatures, kill radius)|Spawner Tab (ObjectDB items, ZNetScene creatures, kill radius)]]
- [[_COMMUNITY_Design rule State is the only source of truth|Design rule: State is the only source of truth]]
- [[_COMMUNITY_Teleport Tab (map click, templebed, saved positions, player teleport)|Teleport Tab (map click, temple/bed, saved positions, player teleport)]]
- [[_COMMUNITY_Valheim|Valheim]]
- [[_COMMUNITY_World Tab (free build, freeinstant crafting, lock time, force weather)|World Tab (free build, free/instant crafting, lock time, force weather)]]

## God Nodes (most connected - your core abstractions)
1. `MenuWindow` - 20 edges
2. `MjolnirMenu.Core` - 16 edges
3. `PlayerCheats` - 14 edges
4. `MjolnirMenu.Features` - 12 edges
5. `Warp` - 12 edges
6. `Esp` - 10 edges
7. `Spawner` - 10 edges
8. `PlayerPatches` - 10 edges
9. `WorldCheats` - 9 edges
10. `SkillCheats` - 8 edges

## Surprising Connections (you probably didn't know these)
- None detected - all connections are within the same source files.

## Import Cycles
- None detected.

## Communities (41 total, 27 thin omitted)

### Community 0 - "BepInEx Plugin Bootstrap & Install"
Cohesion: 0.12
Nodes (11): BaseUnityPlugin, ConfigEntry, ConfigFile, Harmony, ManualLogSource, MenuConfig, bool, float (+3 more)

### Community 1 - "Item & Creature Spawner"
Cohesion: 0.16
Nodes (8): Entry, GameObject, Hotkeys, bool, List, string, Entry, Spawner

### Community 2 - "Teleport & Saved Positions"
Cohesion: 0.14
Nodes (12): Minimap, PlayerInfo, SavedPos, bool, List, string, Vector3, SavedPos (+4 more)

### Community 3 - "Namespace & File Layout"
Cohesion: 0.13
Nodes (9): MjolnirMenu.UI, MjolnirMenu.Patches, MjolnirMenu, MjolnirMenu.Core, MjolnirMenu.Features, Kind, HarmonyPatch, HarmonyPostfix (+1 more)

### Community 4 - "Player Cheats & State Reset"
Cohesion: 0.19
Nodes (7): GameCamera, float, Player, PlayerCheats, HarmonyPatch, HarmonyPrefix, GamePatches

### Community 5 - "ESP Overlay Rendering"
Cohesion: 0.09
Nodes (18): Camera, Kind, Color, float, GUIStyle, List, string, Texture2D (+10 more)

### Community 6 - "Menu Tabs & Project Concepts"
Cohesion: 0.22
Nodes (6): InventoryGui, float, List, Player, string, WorldCheats

### Community 7 - "Player Harmony Patches"
Cohesion: 0.38
Nodes (5): HarmonyPatch, HarmonyPostfix, HarmonyPrefix, Player, PlayerPatches

### Community 8 - "Build Configuration"
Cohesion: 0.22
Nodes (7): net462, BepInEx.Core (5.4.21), BepInEx.PluginInfoProps (2.1.0), HarmonyX (2.10.2), Krafs.Publicizer (2.2.1), Microsoft.NETFramework.ReferenceAssemblies (1.0.3), Microsoft.NET.Sdk

### Community 9 - "IMGUI Skin Styles"
Cohesion: 0.15
Nodes (11): int, Rect, Skill, float, List, SkillCheats, bool, float (+3 more)

### Community 10 - "Character Damage Patch"
Cohesion: 0.20
Nodes (11): Character, CharacterAnimEvent, HitData, Humanoid, float, HarmonyPatch, HarmonyPostfix, HarmonyPrefix (+3 more)

### Community 13 - "MjolnirMenu"
Cohesion: 0.22
Nodes (8): Build, Development tooling, Features, Hotkeys (change in `BepInEx/config/com.mjolnir.menu.cfg`), Install, Layout, License, MjolnirMenu

## Knowledge Gaps
- **38 isolated node(s):** `Kind`, `net462`, `BepInEx.Core (5.4.21)`, `BepInEx.PluginInfoProps (2.1.0)`, `HarmonyX (2.10.2)` (+33 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **27 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `MjolnirMenu.Core` connect `Namespace & File Layout` to `BepInEx Plugin Bootstrap & Install`?**
  _High betweenness centrality (0.264) - this node is a cross-community bridge._
- **Why does `MenuWindow` connect `IMGUI Skin Styles` to `Item & Creature Spawner`, `Teleport & Saved Positions`, `Namespace & File Layout`, `Player Cheats & State Reset`, `ESP Overlay Rendering`?**
  _High betweenness centrality (0.126) - this node is a cross-community bridge._
- **What connects `Kind`, `net462`, `BepInEx.Core (5.4.21)` to the rest of the system?**
  _42 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `BepInEx Plugin Bootstrap & Install` be split into smaller, more focused modules?**
  _Cohesion score 0.125 - nodes in this community are weakly interconnected._
- **Should `Teleport & Saved Positions` be split into smaller, more focused modules?**
  _Cohesion score 0.1422924901185771 - nodes in this community are weakly interconnected._
- **Should `Namespace & File Layout` be split into smaller, more focused modules?**
  _Cohesion score 0.13333333333333333 - nodes in this community are weakly interconnected._
- **Should `ESP Overlay Rendering` be split into smaller, more focused modules?**
  _Cohesion score 0.0873015873015873 - nodes in this community are weakly interconnected._