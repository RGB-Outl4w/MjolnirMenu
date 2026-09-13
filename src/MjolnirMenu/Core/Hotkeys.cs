using MjolnirMenu.Features;
using UnityEngine;

namespace MjolnirMenu.Core
{
    public static class Hotkeys
    {
        public static void Update()
        {
            if (MenuConfig.MenuKey.Value.IsDown())
            {
                State.MenuOpen = !State.MenuOpen;
                Cursor.visible = State.MenuOpen;
                Cursor.lockState = State.MenuOpen ? CursorLockMode.None : CursorLockMode.Locked;
            }

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
