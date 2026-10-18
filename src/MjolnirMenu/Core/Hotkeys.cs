using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using MjolnirMenu.Features;
using UnityEngine;

namespace MjolnirMenu.Core
{
    public static class Hotkeys
    {
        /// <summary>One rebindable key: the config entry, its menu label and what it does.</summary>
        public sealed class Bind
        {
            public string Name = "";
            public ConfigEntry<KeyboardShortcut> Entry = null!;
            public Action Run = null!;
            public bool CanClear = true;
        }

        private static List<Bind>? _all;

        public static List<Bind> All => _all ??= new List<Bind>
        {
            new Bind { Name = "Open / close menu", Entry = MenuConfig.MenuKey, Run = () => SetMenuOpen(!State.MenuOpen), CanClear = false },
            new Bind { Name = "God mode", Entry = MenuConfig.GodKey, Run = () => Flip("God mode", State.GodMode = !State.GodMode, PlayerCheats.ApplyGodMode) },
            new Bind { Name = "Fly", Entry = MenuConfig.FlyKey, Run = () => Flip("Fly", State.Fly = !State.Fly, PlayerCheats.ApplyFly) },
            new Bind { Name = "Ghost mode", Entry = MenuConfig.GhostKey, Run = () => Flip("Ghost mode", State.GhostMode = !State.GhostMode, PlayerCheats.ApplyGhostMode) },
            new Bind { Name = "Speed hack", Entry = MenuConfig.SpeedKey, Run = () => Flip("Speed hack", State.SpeedHack = !State.SpeedHack, null) },
            new Bind { Name = "Infinite stamina", Entry = MenuConfig.StaminaKey, Run = () => Flip("Infinite stamina", State.InfiniteStamina = !State.InfiniteStamina, null) },
            new Bind { Name = "ESP", Entry = MenuConfig.EspKey, Run = ToggleEsp },
            new Bind { Name = "Map click teleport", Entry = MenuConfig.MapTeleportKey, Run = () => Flip("Map click teleport", State.MapClickTeleport = !State.MapClickTeleport, null) },
            new Bind { Name = "Panic reset", Entry = MenuConfig.PanicKey, Run = () => { State.ResetAll(); Notify("All cheats OFF"); } },
        };

        /// <summary>Set by the Settings tab while it waits for a key; every hotkey is muted meanwhile.</summary>
        public static Bind? Rebinding;

        public static void Update()
        {
            if (Rebinding != null) return;

            var menu = All[0];
            if (menu.Entry.Value.IsDown()) menu.Run();

            // Skip quick-toggles while console or chat is eating keyboard input.
            if (global::Console.IsVisible() || (Chat.instance != null && Chat.instance.HasFocus()))
                return;

            for (int i = 1; i < All.Count; i++)
                if (All[i].Entry.Value.IsDown()) All[i].Run();
        }

        private static void Flip(string name, bool on, Action? apply)
        {
            apply?.Invoke();
            Notify(L.F("{0}: {1}", L.T(name), L.T(on ? "ON" : "OFF")));
        }

        private static void ToggleEsp()
        {
            bool on = !(State.EspPlayers || State.EspCreatures || State.EspResources);
            State.EspPlayers = State.EspCreatures = on;
            if (!on) State.EspResources = false;
            Notify(L.F("{0}: {1}", "ESP", L.T(on ? "ON" : "OFF")));
        }

        /// <summary>
        /// Open/close the menu and hand the cursor back correctly. In a world, GameCamera owns the
        /// cursor (our UpdateMouseCapture patch keeps it free while the menu is open); at the main
        /// menu nothing re-shows it, so never hide it there.
        /// </summary>
        public static void SetMenuOpen(bool open)
        {
            State.MenuOpen = open;
            if (!open) Rebinding = null;
            bool inWorld = Player.m_localPlayer != null && GameCamera.instance != null;
            if (open || !inWorld)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        /// <summary>Top-left game message. Plain English strings are translated; pre-formatted ones pass through.</summary>
        public static void Notify(string msg)
        {
            msg = L.T(msg);
            var p = Player.m_localPlayer;
            if (p != null)
                p.Message(MessageHud.MessageType.TopLeft, $"[Mjolnir] {msg}");
            else
                Plugin.Log.LogInfo(msg);
        }
    }
}
