# MjolnirMenu

In-game trainer / mod menu for **Valheim**, built on BepInEx 5 + HarmonyX. Press **Insert** in-game to open a tabbed IMGUI window.

> Intended for single-player and servers you own or have permission to mod. It contains no anti-detection or server-bypass logic; don't use it on other people's servers.

## Features

| Tab | What you get |
|-----|--------------|
| **Player** | God mode, ghost mode (enemies ignore you), infinite stamina / eitr, no fall damage, infinite carry weight, fly, speed hack (×1–10), jump hack (×1–10), crouch speed hack, infinite stamina while crouched, sit/stand animation speed, heal + refill. Water: swim speed hack, jump while swimming, walk on water, walk on the seabed, underwater camera |
| **Combat** | Infinite durability (weapons, armor, tools), melee / ranged damage multipliers (×1–50), attack speed hack (×1–5) |
| **Skills** | Every skill as a slider; max all / reset all / set all to a custom level; per-skill 0 / 50 / 100 buttons |
| **World** | Free build (no piece cost), no placement delay, free crafting (no materials), instant crafting, lock time of day (slider + dawn/noon/dusk/night), force weather (every `EnvSetup` the game knows) |
| **Spawner** | Searchable catalog of every inventory-safe item (`ObjectDB`, filtered to items with an icon and a real item type; "Show all" lifts the filter) and creature (`ZNetScene`); give items with stack + quality, spawn creatures with level, count, tamed; kill everything within a radius |
| **Teleport** | Fast teleport / portals (scales the vortex wait ×1–40), Ctrl + left-click on the big map to teleport, jump to start temple / bed, save & recall named positions (persisted to `BepInEx/config/MjolnirMenu.positions.txt`), teleport to other players who share their position |
| **ESP** | Labels for players, creatures/bosses (name, level, HP) and resources (pickables, ore, trees) with distance and optional tracer lines; range and label cap are configurable |
| **Settings** | Hotkey overview, watermark toggle, never tag loot as "obtained using cheats" (on by default) + clear existing tags, save current speed/jump multipliers as defaults |
| **Credits** | Who made it and what it runs on |

### Hotkeys (change in `BepInEx/config/com.mjolnir.menu.cfg`)

| Key | Action |
|-----|--------|
| `Insert` | Open / close menu |
| `F6` | Toggle god mode |
| `F7` | Toggle fly |
| `End` | Panic: turn every cheat off |

Every cheat is also switched off automatically on logout so nothing leaks into your next world.

While the menu is open the game's input gates (`Player.TakeInput`, `PlayerController.TakeInput`) report false, so clicks and keys go to the menu, not your character.

## Build

Requirements: .NET SDK 8+ (tested with 10), Valheim installed.

```powershell
# optional: point at your game folder if it isn't D:\SteamLibrary\steamapps\common\Valheim
$env:VALHEIM_DIR = "C:\Program Files (x86)\Steam\steamapps\common\Valheim"

dotnet build src\MjolnirMenu -c Release
```

Game assemblies are referenced straight from `valheim_Data\Managed`; BepInEx and Harmony come from the BepInEx NuGet feed (see `nuget.config`), so the project compiles even before BepInEx is installed. `Krafs.Publicizer` exposes the private `m_` fields the patches need.

## Install

1. Install BepInEx for Valheim (denikson's `BepInExPack_Valheim`). Either use the helper – it asks before downloading – or drop the pack in manually:
   ```powershell
   .\scripts\install-bepinex.ps1
   ```
2. Launch Valheim once so BepInEx generates its folders, then quit.
3. Build & deploy:
   ```powershell
   .\scripts\deploy.ps1
   ```
   (plain `dotnet build` also copies the DLL into `BepInEx\plugins\MjolnirMenu` when that folder exists.)
4. Start the game. `BepInEx\LogOutput.log` should contain `Loading [MjolnirMenu 0.3.0]`.

## Layout

```
src/MjolnirMenu/
  Plugin.cs            BepInEx entry point; Harmony PatchAll; Update/OnGUI dispatch
  Core/                MenuConfig (config entries), State (all toggles), Hotkeys
  Features/            PlayerCheats, WorldCheats, SkillCheats, Spawner, Warp (teleport), Esp
  Patches/             Harmony patches on Player, PlayerController, PlayerProfile, Character, Humanoid, CharacterAnimEvent, Minimap, EnvMan, Game
  UI/                  MenuWindow (tabs) and Styles (skin)
scripts/               install-bepinex.ps1, deploy.ps1
graphify-out/          knowledge graph of the codebase (graphify)
```

Design rule: `State` is the only source of truth. Patches read it; `Features/*` reconcile game fields against it every frame and restore originals when a toggle goes off.

## Development tooling

This repo was built with two Claude Code helpers:

- **[caveman](https://github.com/JuliusBrussee/caveman)** – terse assistant output while iterating.
- **[graphify](https://github.com/safishamsi/graphify)** – `graphify-out/graph.html` is an interactive map of how the classes and patches connect; regenerate with `graphify update .` after changes.

## License

MIT
