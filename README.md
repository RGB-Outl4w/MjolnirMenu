<div align="center">

# ⚒️ MjolnirMenu

**An in-game trainer and mod menu for Valheim, built on BepInEx and HarmonyX.**

[![Release](https://img.shields.io/github/v/release/RGB-Outl4w/MjolnirMenu?include_prereleases&style=flat-square&color=e0b83a)](https://github.com/RGB-Outl4w/MjolnirMenu/releases)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-net462-512bd4?style=flat-square)](src/MjolnirMenu/MjolnirMenu.csproj)
[![BepInEx](https://img.shields.io/badge/BepInEx-5.4-2d7d46?style=flat-square)](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
[![Valheim](https://img.shields.io/badge/Valheim-PC%20%2F%20Steam-8b4513?style=flat-square)](https://store.steampowered.com/app/892970/Valheim/)

Press <kbd>Insert</kbd> in-game. Eleven tabs. Everything reversible.

**[Website](https://rgb-outl4w.github.io/MjolnirMenu/)** · [Features](#features) · [Installation](#installation) · [Usage](#usage) · [Configuration](#configuration) · [Building](#building-from-source) · [Architecture](#architecture) · [FAQ](#faq)

</div>

---

> **Fair play notice.** MjolnirMenu is intended for single-player worlds and servers you own or have explicit permission to mod. It contains no anti-detection, no server bypass, and nothing that hides it from other players. Don't bring it to public servers.

## Features

### Player
| Toggle | Effect |
|---|---|
| God mode | No damage of any kind (`Player.SetGodMode` + `Character.Damage` guard) |
| Ghost mode | Enemies ignore you |
| Infinite stamina / eitr | Pools never drain; bars stay full |
| Infinite stamina while crouched | Crouch-only variant; overridden by the global toggle |
| No fall damage | Fall hits are dropped before they apply |
| Infinite carry weight | `GetMaxCarryWeight` → 100 000 |
| Extra inventory rows | Up to +5 rows through the game's own `SetInventorySize`; native panel, survives relog; shrinking packs items into free slots and drops the rest at your feet |
| Multi-equip armor | Equip several helmets / chests / legs / capes at once; extras add armor and equip effects, the model shows only the slot item |
| Fly | Vanilla debug fly, no console required |
| Speed / jump / crouch hacks | ×1–10 multipliers on run, jog, jump force and crouch speed |
| Sit / stand animation speed | ×1–5 on the emote animator, including the stand-up transition |
| Heal + refill | Instant full health, stamina and eitr |

**Water:** swim speed hack (×1–10) · jump while swimming · use items while swimming · walk on water · walk on the seabed · underwater camera (removes the camera's water clamp so it follows you when you dive).

### Combat
| Toggle | Effect |
|---|---|
| Infinite durability | Weapons, armor and tools never wear |
| Infinite items | Ammo, food, mead, fuel, ore, animal feed and similar consumables are never removed from your inventory. Drag-and-drop, dropping and selling still behave normally |
| Infinite interaction | Interacting with a dropped item, bush, mushroom, pickable or beehive gives you a copy and leaves the source untouched. Walk-over pickup is disabled while this is on so nothing is double-collected |
| Damage multiplier | Separate ×1–50 sliders for melee, ranged, **trees**, **stones / ores**, **player structures** and **world structures / destructibles** |
| Attack speed hack | ×1–5 on attack animations — swings, draws and combos all finish faster |
| Ranged | **Insta-focus** (bow fully drawn instantly), **insta-shot** (full-draw shot on press; semi-auto or full-auto), **insta-reload** (crossbows), **hitscan** (straight line to the reticle, no drop or spread) |

### World
Free build (no piece cost) · no placement delay · free crafting · instant crafting · big stations (×1–50 capacity for smelters, kilns, fires, beehives, sap collectors) · lock time of day (slider, dawn / noon / dusk / night) · force any weather the game defines.

### Vehicles
* **Ship** — *Tailwind* (full-strength wind pinned to where the helmsman looks, no transition lag, full sail from any angle) · *Ship speed hack* (×1–20; extra sail thrust applied at the centre of mass so it can't flip the hull, paddles scaled directly) · *Responsive steering* (×1–10 rudder speed and turning force) · *Flip nearest ship upright* button.
* **Cart** — *Cart speed hack* (cart mass → 1, no extra pull mass on the player, so it follows at full run speed) · *Cart no-clip* (`Rigidbody.excludeLayers` on every cart body: rolls through everything but `terrain` and `piece`) · *Sticky hitch* (unbreakable joint, never unhitches on its own) · *Hitch from anywhere* (cart swings into place behind you).
* **Hull immunity** — `ImpactEffect.OnCollisionEnter` is skipped whenever a ship or cart is on either side, and ships take no capsize damage.

### Skills
Every skill as a slider with 0 / 50 / 100 shortcuts, plus *Max all*, *Reset all* and *Set all to N*. Written straight into `Skills.Skill.m_level`, so it persists with the character.

### Effects
* **Forsaken powers** — pick any boss power (Eikthyr, The Elder, Bonemass, Moder, Yagluth, The Queen, Fader) whether or not you've sacrificed the trophy. *No cooldown* and *Power lasts forever* toggles, plus *Activate now*.
* **Status effect manager** — list of everything currently on you with remaining time; **extend** (+60 s / +10 min), **freeze** (timer pinned at zero) or **remove** each one. Searchable catalog of every `StatusEffect` in the game with one-click *Apply*.

### Spawner
Searchable catalog of every item and creature. Items are filtered to those that belong in an inventory (icon + item type); *Show all* lifts the filter. Give items with stack and quality; spawn creatures with level, count and *tamed*; kill everything within a radius.

### Teleport
Instant menu teleports (no fade, no vortex) · fast portals (×1–40 on the vortex timer) · portals accept any item · <kbd>Ctrl</kbd> + left-click the map to teleport · start temple / bed · named saved positions · teleport to other players who share their location.

### ESP
Labels for players, creatures / bosses (name, level, HP) and resources (pickables, ore, trees) with distance and optional tracer lines. Range and label cap are configurable.

### Settings
* **Presets** — save the whole menu state under a name, load it later, delete it, or mark one as *Auto-load* to have it applied the moment you enter a world.
* **Cheat tags** — the game marks you as a cheater the first time you deal damage in god / ghost / fly mode and stamps every drop afterwards with *"obtained using cheats"*. MjolnirMenu reports the game's own `bypasscheatchecks` key instead (on by default) and can clear tags on items you already carry.
* Hotkey overview, watermark toggle, persistent defaults for multipliers.

### Credits
Who made it and what it runs on.

## Installation

**Requirements:** Valheim (Steam, Windows) and [BepInExPack_Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/).

1. Install BepInEx into your Valheim folder. Either use the helper script (it asks before downloading) or extract the pack manually:
   ```powershell
   .\scripts\install-bepinex.ps1
   ```
2. Launch Valheim once so BepInEx creates its folders, then quit.
3. Drop `MjolnirMenu.dll` into `<Valheim>\BepInEx\plugins\MjolnirMenu\` — from a [release](https://github.com/RGB-Outl4w/MjolnirMenu/releases) or by [building from source](#building-from-source).
4. Start the game. `BepInEx\LogOutput.log` should contain `Loading [MjolnirMenu 0.8.0]`.

## Usage

| Key | Action |
|---|---|
| <kbd>Insert</kbd> | Open / close the menu |
| <kbd>F6</kbd> | Toggle god mode |
| <kbd>F7</kbd> | Toggle fly |
| <kbd>End</kbd> | Panic: switch every cheat off |

While the menu is open the game's input gates (`Player.TakeInput`, `PlayerController.TakeInput`) report *false* and `GameCamera.UpdateMouseCapture` is skipped: keys and clicks reach the menu, not your character, and the cursor stays free. Every cheat is switched off automatically on logout so nothing leaks into the next world.

The in-game window uses the same design as the [website](https://rgb-outl4w.github.io/MjolnirMenu/) — every texture is generated at runtime, so the plugin stays a single DLL.

## Configuration

`BepInEx/config/com.mjolnir.menu.cfg` — generated on first run.

| Section | Key | Default | Purpose |
|---|---|---|---|
| Hotkeys | `MenuToggle` `GodToggle` `FlyToggle` `PanicReset` | Insert / F6 / F7 / End | Key bindings |
| General | `HideCheatTags` | `true` | Report `bypasscheatchecks` so loot is never tagged |
| General | `AutoLoadPreset` | *(empty)* | Preset applied when you enter a world |
| Player | `SpeedMultiplier` `JumpMultiplier` | 2.0 | Default multipliers |
| ESP | `Range` `MaxEntities` `*Color` | 150 m / 250 | ESP tuning |
| UI | `ShowWatermark` `WindowX` `WindowY` | | Window state |

Presets live in `BepInEx/config/MjolnirMenu.presets/*.preset` (plain `Key=Value` text, safe to share). Saved teleport positions are in `BepInEx/config/MjolnirMenu.positions.txt`.

## Building from source

**Requirements:** .NET SDK 8 or newer, Valheim installed.

```powershell
# Only needed if Valheim isn't at D:\SteamLibrary\steamapps\common\Valheim
$env:VALHEIM_DIR = "C:\Program Files (x86)\Steam\steamapps\common\Valheim"

dotnet build src\MjolnirMenu -c Release
```

* Game assemblies are referenced directly from `valheim_Data\Managed`; nothing from the game is redistributed.
* BepInEx and HarmonyX come from the BepInEx NuGet feed (`nuget.config`), so the project compiles even before BepInEx is installed.
* [Krafs.Publicizer](https://github.com/krafs/Publicizer) exposes the private `m_*` fields the patches rely on — no reflection strings.
* A successful build copies the DLL into `BepInEx\plugins\MjolnirMenu` automatically when that folder exists; `scripts\deploy.ps1` does the same explicitly.

## Architecture

```
src/MjolnirMenu/
├── Plugin.cs             BepInEx entry point · Harmony.PatchAll · Update / OnGUI dispatch
├── Core/
│   ├── State.cs          Every toggle and multiplier — the single source of truth
│   ├── MenuConfig.cs     BepInEx ConfigEntry bindings
│   └── Hotkeys.cs        Key handling and on-screen notifications
├── Features/             Reconcile game state against State every frame; restore originals on toggle-off
│   ├── PlayerCheats.cs   Survival, movement, water, camera, durability
│   ├── WorldCheats.cs    Build, craft, time, weather
│   ├── SkillCheats.cs    Skill levels
│   ├── Effects.cs        Forsaken powers and the status-effect manager
│   ├── Spawner.cs        Item / creature catalog and spawning
│   ├── Warp.cs           Teleport and saved positions
│   ├── Esp.cs            Overlay scanning and drawing
│   └── Presets.cs        Save / load / auto-load of State
├── Patches/              Harmony patches, grouped by game type
│   ├── PlayerPatches.cs         stamina · carry weight · speed · crafting · input gate · teleport speed
│   ├── CharacterPatches.cs      damage in/out · water jump · swim state · attack & emote speed
│   ├── DestructiblePatches.cs   per-category damage for trees, rocks, pieces, destructibles
│   ├── ItemPatches.cs           infinite items (scoped) · duplicate-on-interact
│   ├── MinimapPatches.cs        map-click teleport
│   ├── EnvManPatches.cs         forced weather
│   └── GamePatches.cs           reset on logout
└── UI/
    ├── MenuWindow.cs     Tabbed IMGUI window
    └── Styles.cs         Skin
```

**Design rules**

1. `State` is the only source of truth. Patches read it; they never hold state of their own.
2. Anything that writes a game field snapshots the original per `Player` instance and reconciles every frame, so respawns, relogs and toggling off all land back on vanilla values.
3. Patches touch only the local player unless a feature is explicitly about the world.
4. No reflection by name — Publicizer makes private members ordinary C#.

## FAQ

**Does it work in multiplayer?**
Client-side effects (movement, ESP, menu) work anywhere. Damage multipliers are applied before the hit is sent to the target's owner, so they work too. Nothing here is hidden from a server or other players; see the notice at the top.

**Items I picked up are tagged "obtained using cheats".**
That's the game's own flag, set the moment you damage something while god / ghost / fly is on. *Settings → Cheat tags* is on by default and stops new tags; use *Clear tags on items in inventory* for what you already carry.

**A spawned item behaves strangely.**
The item catalog hides prefabs that aren't real inventory items (monster weapons, effects). If you turned on *Show all*, you're on your own.

**Can I make the menu load with my preferred settings?**
Save a preset in *Settings*, then mark it *Auto-load*.

## Development

This repository was built with [Claude Code](https://claude.com/claude-code), using two community add-ons: [caveman](https://github.com/JuliusBrussee/caveman) for terse assistant output and [graphify](https://github.com/safishamsi/graphify) for a queryable knowledge graph of the codebase. The graph is generated locally (`graphify .`) and is not part of the repository.

Game method signatures were verified against `assembly_valheim.dll` with Mono.Cecil rather than guessed; the IL for every patched method was read before the patch was written.

## License

[MIT](LICENSE). Valheim © Iron Gate Studio / Coffee Stain Publishing. This project is not affiliated with either.
