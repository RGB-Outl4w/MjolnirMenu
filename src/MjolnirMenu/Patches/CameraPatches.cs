using HarmonyLib;
using MjolnirMenu.Core;
using UnityEngine;

namespace MjolnirMenu.Patches
{
    [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateMouseCapture))]
    internal static class MouseCapturePatch
    {
        /// <summary>
        /// The game re-locks and hides the cursor every frame unless one of its own GUIs is open.
        /// Our OnGUI unlock ran after it, so the cursor flickered. Own the cursor while the menu is open.
        /// </summary>
        private static bool Prefix()
        {
            if (!State.MenuOpen) return true;
            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible) Cursor.visible = true;
            return false;
        }
    }
}
