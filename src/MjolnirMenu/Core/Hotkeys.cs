using MjolnirMenu.Features;
using UnityEngine;

namespace MjolnirMenu.Core
{
    public static class Hotkeys
    {
        public static void Update()
        {
            if (MenuConfig.MenuKey.Value.IsDown())
                SetMenuOpen(!State.MenuOpen);

            // Skip quick-toggles while console or chat is eating keyboard input.
            if (Console.IsVisible() || (Chat.instance != null && Chat.instance.HasFocus()))
                return;

            if (MenuConfig.GodKey.Value.IsDown())
            {
                State.GodMode = !State.GodMode;
                PlayerCheats.ApplyGodMode();
                Notify($"God mode {(State.GodMode ? "ON" : "OFF")}");
            }

            if (MenuConfig.FlyKey.Value.IsDown())
            {
                State.Fly = !State.Fly;
                PlayerCheats.ApplyFly();
                Notify($"Fly {(State.Fly ? "ON" : "OFF")}");
            }

            if (MenuConfig.PanicKey.Value.IsDown())
            {
                State.ResetAll();
                Notify("All cheats OFF");
            }
        }

        /// <summary>
        /// Open/close the menu and hand the cursor back correctly. In a world, GameCamera owns the
        /// cursor (our UpdateMouseCapture patch keeps it free while the menu is open); at the main
        /// menu nothing re-shows it, so never hide it there.
        /// </summary>
        public static void SetMenuOpen(bool open)
        {
            State.MenuOpen = open;
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

        public static void Notify(string msg)
        {
            var p = Player.m_localPlayer;
            if (p != null)
                p.Message(MessageHud.MessageType.TopLeft, $"[Mjolnir] {msg}");
            else
                Plugin.Log.LogInfo(msg);
        }
    }
}
