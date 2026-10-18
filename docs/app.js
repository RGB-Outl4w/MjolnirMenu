/* MjolnirMenu site — tab replica + release lookup. Vanilla JS, no deps. */
(function () {
  "use strict";

  // Russian page (ru/index.html) loads ru/strings.js first; everything else passes English through.
  var RU = document.documentElement.lang === "ru" ? (window.MJOLNIR_RU || {}) : null;
  function tr(s) { return RU && RU[s] ? RU[s] : s; }

  // Mirrors src/MjolnirMenu/UI/MenuWindow.cs. Keep in sync when tabs change.
  var TABS = [
    {
      name: "Player",
      intro: "Survival and movement. Everything here is reconciled against the game each frame and restored to vanilla when you turn it off.",
      groups: [
        { title: "Survival", items: [
          ["God mode", "No damage of any kind", true],
          ["Ghost mode", "Enemies ignore you"],
          ["Infinite stamina", "Bar never drains", true],
          ["Infinite eitr", "Magic pool never drains"],
          ["No fall damage", ""],
          ["Infinite carry weight", "Max weight → 100 000"],
          ["No hunger", "Eaten food never runs out; its health and stamina stay full", true],
          ["No knockback / stagger", "Hits, broken blocks and parries never push or stun you"],
          ["Multi-equip armor", "Stack helmets, chests, legs, capes; extras add armor + effects"],
          ["Extra inventory rows  +2", "Up to 9 rows via the game's own resize; survives relog", false, 40]
        ], chips: ["Heal + refill", "Rested now"] },
        { title: "Movement", items: [
          ["Fly", "Vanilla debug fly, no console needed"],
          ["Speed hack", "×1–10 on run and jog", true, 35],
          ["Jump hack", "×1–10 on jump force", false, 20],
          ["Crouch speed hack", "×1–10 while sneaking", false, 30],
          ["Infinite stamina while crouched", "Overridden by the global toggle"],
          ["Sit / stand animation speed", "×1–5 on the emote animator", false, 45]
        ]},
        { title: "Water", items: [
          ["Swim speed hack", "×1–10", false, 40],
          ["Jump while swimming", ""],
          ["Use items while swimming", "Weapons and tools stay out; equip, attack and use hotbar in water"],
          ["Underwater camera", "Camera follows you below the surface"],
          ["Walk on water", "Surface behaves like ground"],
          ["Walk on seabed", "Sink and walk underwater"]
        ]}
      ]
    },
    {
      name: "Combat",
      intro: "Gear, damage and attack pacing. Damage multipliers are applied before the hit leaves your client, so they work in multiplayer too.",
      groups: [
        { title: "Gear", items: [
          ["Infinite durability", "Weapons, armor and tools never wear", true],
          ["Infinite items", "Ammo, food, mead, fuel, ore and feed are never consumed. Drag-and-drop, dropping and selling stay normal"],
          ["Infinite interaction", "Pick-ups, bushes, mushrooms and beehives give you a copy and stay full. Walk-over pickup is disabled while on"]
        ]},
        { title: "Attack speed", items: [
          ["Attack speed hack", "×1–5 on attack animations — swings, draws and combos finish faster", false, 45]
        ]},
        { title: "Ranged", items: [
          ["Insta-focus", "Bows are fully drawn the moment you start aiming"],
          ["Insta-shot", "One full-draw shot per press, or full-auto while held"],
          ["Insta-reload", "Crossbows are loaded the moment they fire"],
          ["Hitscan", "Straight line from the bow to the reticle — no drop, no spread"]
        ]},
        { title: "Damage", items: [
          ["Damage multiplier", "Master toggle", true],
          ["Melee", "×1–50", null, 25],
          ["Ranged", "×1–50", null, 25],
          ["Trees", "×1–50", null, 60],
          ["Stones / ores", "×1–50", null, 60],
          ["Player structures", "×1–50", null, 10],
          ["World structures / destructibles", "×1–50", null, 40]
        ]}
      ]
    },
    {
      name: "World",
      intro: "Building, crafting, time and weather.",
      groups: [
        { title: "Build & craft", items: [
          ["Free build", "No piece cost", true],
          ["No placement delay", ""],
          ["Free crafting", "No materials"],
          ["Instant crafting", ""],
          ["Big stations", "×1–50 capacity for smelters, kilns, fires, beehives, sap collectors", true, 40],
          ["Instant stations", "Smelters, kilns, windmills: 1 item/s. Fermenters 2 s. Cooking 1 s, never burns", true],
          ["Instant growth", "Crops and saplings grow the moment their spot is valid"],
          ["Repair radius  50 m", "", null, 22]
        ], chips: ["Repair all nearby"] },
        { title: "Time of day", items: [
          ["Lock time", "Slider plus Dawn / Noon / Dusk / Night", true, 50]
        ], chips: ["Dawn", "Noon", "Dusk", "Night"] },
        { title: "Weather", list: [["Natural weather", ""], ["Clear", "env"], ["Rain", "env"], ["ThunderStorm", "env"], ["Snow", "env"], ["Ashrain", "env"], ["…every EnvSetup the game defines", ""]] }
      ]
    },
    {
      name: "Vehicles",
      intro: "Ships and carts. Ship cheats apply only while you hold the helm and are restored the moment you let go.",
      groups: [
        { title: "Ship", items: [
          ["Tailwind", "At the helm: full wind wherever you look; waves stay natural", true],
          ["Ship speed hack", "At the helm: ×1–20 sail and paddle force", true, 15],
          ["Responsive steering", "At the helm: ×1–10 rudder speed and turning force", true, 22]
        ], chips: ["Flip nearest ship upright"] },
        { title: "Cart", items: [
          ["Cart speed hack", "Cart weighs nothing and follows at full run speed", true],
          ["Cart no-clip", "Rolls through trees, rocks and mobs; still rides terrain and floors"],
          ["Sticky hitch", "Never unhitches on its own", true],
          ["Hitch from anywhere", "Cart swings into place behind you"],
          ["Carts ride ships", "Loose carts stay locked to the deck without slowing the ship", true]
        ]},
        { title: "Ships and carts", items: [
          ["Hull immunity", "No collision, wave-slam or capsize damage; deals none either", true]
        ]}
      ]
    },
    {
      name: "Skills",
      intro: "Every skill as a slider. Written straight into the game's skill data, so it saves with the character.",
      groups: [
        { title: "Bulk", chips: ["Max all (100)", "Reset all (0)", "Set all to N"] },
        { title: "Per skill", list: [["Swords", "72"], ["Bows", "100"], ["Run", "58"], ["Jump", "40"], ["Sneak", "91"], ["Woodcutting", "100"], ["Pickaxes", "100"], ["Blood magic", "33"], ["…"]] }
      ]
    },
    {
      name: "Effects",
      intro: "Forsaken powers and the status-effect manager.",
      groups: [
        { title: "Forsaken power", chips: ["Eikthyr", "The Elder", "Bonemass", "Moder", "Yagluth", "The Queen", "Fader", "None"], items: [
          ["No cooldown", "", true],
          ["Power lasts forever", "Timer pinned at zero", true]
        ]},
        { title: "Active effects", list: [["Rested", "∞ ❄"], ["Bonemass power", "∞ ❄"], ["Wet", "42s"], ["Cold", "13s"]] },
        { title: "All effects", list: [["Search any StatusEffect", ""], ["Rested", "Apply"], ["Frost resistance", "Apply"], ["Tared", "Apply"], ["…200+", ""]] }
      ]
    },
    {
      name: "Spawner",
      intro: "Every item and creature the game knows, searchable. Items are filtered to real inventory items; Show all lifts the filter.",
      groups: [
        { title: "Items", list: [["Wood", "Wood"], ["Black metal sword", "SwordBlackmetal"], ["Mead: healing", "MeadHealthMedium"], ["Fenris hood", "HelmetFenring"]], chips: ["Stack", "Quality", "Give"] },
        { title: "Creatures", list: [["Greydwarf", "Greydwarf"], ["Troll", "Troll"], ["★ Eikthyr", "Eikthyr"], ["Lox", "Lox"]], chips: ["Level (any)", "Count", "Tamed (bosses too)", "Morph", "Spawn", "Morph back"] },
        { title: "Kill nearby", items: [["Radius", "5–200 m, optionally including tamed", null, 30]], chips: ["Kill"] }
      ]
    },
    {
      name: "Teleport",
      intro: "Menu teleports are instant — no fade, no vortex. Real portals get a separate speed multiplier.",
      groups: [
        { title: "Map", items: [
          ["Ctrl + left-click on the big map to teleport", "", true],
          ["Instant menu teleports", "No fade, no vortex wait", true],
          ["Portals accept any item", "Ore, eggs and other non-teleportable items go through", true],
          ["Fast portals", "×1–40 on the vortex timer for real portals", true, 25]
        ], chips: ["Start temple", "Bed / spawn point", "Death point", "Reveal map", "Reveal POIs", "Restore fog"] },
        { title: "Boss altars", chips: ["Eikthyr", "The Elder", "Bonemass", "Moder", "Yagluth", "The Queen", "Fader", "Kall Fimbulbringer"] },
        { title: "Saved positions", list: [["Base", "-120 / 340"], ["Silver vein", "88 / -1020"], ["Swamp crypt", "412 / 96"]], chips: ["Save here", "Go", "×"] },
        { title: "Players", list: [["Other players who share their position", "Go"]] }
      ]
    },
    {
      name: "ESP",
      intro: "Wallhack-style labels. Characters are cheap; resources are rescanned every couple of seconds.",
      groups: [
        { title: "Show", items: [
          ["Players", "", true],
          ["Creatures & bosses", "Name, level, HP", true],
          ["Resources", "Pickables, ore, trees — heavier"]
        ]},
        { title: "Style", items: [
          ["Distance", "", true],
          ["Tracer lines", ""],
          ["Range", "20–1000 m", null, 15],
          ["Max labels", "10–2000", null, 12]
        ]}
      ]
    },
    {
      name: "Settings",
      intro: "Character rename, hotkeys, language, window look, presets, cheat tags and defaults.",
      groups: [
        { title: "Character", list: [["bjorn", "→ ragnar"]], chips: ["Rename"] },
        { title: "Hotkeys", list: [["Open / close menu", "Insert"], ["God mode", "F6"], ["Fly", "F7"], ["Ghost mode", "not set"], ["Speed hack", "not set"], ["Infinite stamina", "not set"], ["ESP", "not set"], ["Map click teleport", "not set"], ["Panic reset", "End"]], chips: ["Click a key, press the new one", "×", "Default"] },
        { title: "Language", chips: ["Auto (game language)", "English", "Русский"] },
        { title: "Window", items: [
          ["Scale  100%", "75–150% in 5% steps"],
          ["Opacity  100%", "", null, 100],
          ["Show watermark", "Small MjolnirMenu tag in the top-left corner", true]
        ] },
        { title: "Presets", list: [["speedrun", "Load · Auto-load: ON · ×"], ["building", "Load · Auto-load · ×"], ["chill", "Load · Auto-load · ×"]], chips: ["Save current"] },
        { title: "Cheat tags", items: [
          ["Never tag loot / crafts as \"obtained using cheats\"", "Reports the game's own bypass key", true]
        ], chips: ["Clear tags on items in inventory"] },
        { title: "Defaults", chips: ["Save speed/jump as default"] }
      ]
    },
    {
      name: "Credits",
      intro: "Who made it and what it runs on.",
      groups: [
        { title: "Made by", list: [["RGB-Outl4w", "github.com/RGB-Outl4w"]] },
        { title: "Built with", list: [["Claude Code", "Anthropic"], ["caveman", "JuliusBrussee"], ["graphify", "safishamsi"]] },
        { title: "Runs on", list: [["BepInEx 5", "plugin loader"], ["HarmonyX", "method patching"], ["Krafs.Publicizer", "private field access"]] }
      ]
    }
  ];

  var tabsEl = document.getElementById("tabs");
  var bodyEl = document.getElementById("tab-body");
  if (!tabsEl || !bodyEl) return;

  function esc(s) {
    return String(s).replace(/[&<>"]/g, function (c) {
      return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c];
    });
  }

  function renderItem(it) {
    var label = tr(it[0]), hint = tr(it[1] || ""), on = it[2], v = it[3];
    var html = '<div class="tog">';
    // null = value-only row (no checkbox); undefined = plain toggle that is off.
    if (on !== null) html += '<i class="' + (on ? "on" : "") + '" aria-hidden="true"></i>';
    else html += '<i style="visibility:hidden" aria-hidden="true"></i>';
    html += "<span>" + esc(label) + (hint ? "<small>" + esc(hint) + "</small>" : "") + "</span></div>";
    if (typeof v === "number") html += '<span class="slider" style="--v:' + v + '%" aria-hidden="true"></span>';
    return html;
  }

  function renderGroup(g) {
    var html = "<div><h4>" + esc(tr(g.title)) + "</h4>";
    if (g.items) html += g.items.map(renderItem).join("");
    if (g.list) {
      html += '<ul class="list">' + g.list.map(function (r) {
        var dim = !r[1] && r[0].indexOf("…") === 0;
        return '<li class="' + (dim ? "dim" : "") + '"><span>' + esc(tr(r[0])) + "</span><span>" + esc(tr(r[1] || "")) + "</span></li>";
      }).join("") + "</ul>";
    }
    if (g.chips) html += '<div class="row">' + g.chips.map(function (c) { return '<button type="button" class="chip" tabindex="-1">' + esc(tr(c)) + "</button>"; }).join("") + "</div>";
    return html + "</div>";
  }

  function select(i) {
    Array.prototype.forEach.call(tabsEl.children, function (b, j) {
      b.setAttribute("aria-selected", i === j ? "true" : "false");
      b.tabIndex = i === j ? 0 : -1;
    });
    var t = TABS[i];
    bodyEl.innerHTML = '<div class="pane" role="tabpanel"><p class="intro">' + esc(tr(t.intro)) + "</p>" + t.groups.map(renderGroup).join("") + "</div>";
    try { history.replaceState(null, "", "#tab-" + t.name.toLowerCase()); } catch (e) { /* ignore */ }
  }

  TABS.forEach(function (t, i) {
    var b = document.createElement("button");
    b.type = "button";
    b.setAttribute("role", "tab");
    b.textContent = tr(t.name);
    b.addEventListener("click", function () { select(i); });
    b.addEventListener("keydown", function (e) {
      var n = e.key === "ArrowRight" ? i + 1 : e.key === "ArrowLeft" ? i - 1 : -1;
      if (n < 0 || n >= TABS.length) return;
      e.preventDefault();
      select(n);
      tabsEl.children[n].focus();
    });
    tabsEl.appendChild(b);
  });

  var initial = 0;
  var m = /#tab-([a-z]+)/.exec(location.hash);
  if (m) TABS.some(function (t, i) { if (t.name.toLowerCase() === m[1]) { initial = i; return true; } return false; });
  select(initial);

  // Toggle/slider count for the hero stat.
  var toggles = 0;
  TABS.forEach(function (t) { t.groups.forEach(function (g) { if (g.items) toggles += g.items.length; }); });
  var st = document.getElementById("stat-toggles");
  if (st) st.textContent = toggles + "+";

  // Mobile nav.
  var navBtn = document.querySelector(".nav-toggle");
  var nav = document.querySelector(".nav");
  if (navBtn && nav) {
    navBtn.addEventListener("click", function () {
      var open = nav.classList.toggle("open");
      navBtn.setAttribute("aria-expanded", open ? "true" : "false");
    });
    nav.addEventListener("click", function (e) { if (e.target.tagName === "A") nav.classList.remove("open"); });
  }

  // Latest release version + direct DLL link, if GitHub's API is reachable.
  var api = "https://api.github.com/repos/RGB-Outl4w/MjolnirMenu/releases/latest";
  if (window.fetch) {
    fetch(api, { headers: { Accept: "application/vnd.github+json" } })
      .then(function (r) { return r.ok ? r.json() : null; })
      .then(function (rel) {
        if (!rel || !rel.tag_name) return;
        var ver = document.getElementById("dl-ver");
        var mv = document.getElementById("menu-ver");
        if (ver) ver.textContent = rel.tag_name + " · MjolnirMenu.dll";
        if (mv) mv.textContent = rel.tag_name;
        var dll = (rel.assets || []).filter(function (a) { return /\.dll$/i.test(a.name); })[0];
        var dl = document.getElementById("dl");
        if (dll && dl) dl.href = dll.browser_download_url;
      })
      .catch(function () { /* offline or rate-limited: keep the static link */ });
  }
})();
