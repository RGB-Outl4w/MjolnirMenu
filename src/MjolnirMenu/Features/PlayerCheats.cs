using MjolnirMenu.Core;
using UnityEngine;

namespace MjolnirMenu.Features
{
    /// <summary>
    /// Player-side cheats. Game fields we touch (god flag, fly flag, jump force) are
    /// reconciled every frame against <see cref="State"/> so a respawn or a new Player
    /// instance never drifts out of sync, and turning a toggle off restores the original.
    /// </summary>
    public static class PlayerCheats
    {
        private static Player? _trackedPlayer;
        private static float _origJumpForce;

        public static void Tick()
        {
            var p = Player.m_localPlayer;
            if (p == null)
            {
                _trackedPlayer = null;
                return;
            }

            if (!ReferenceEquals(p, _trackedPlayer))
            {
                // Fresh Player object (login / respawn): snapshot vanilla values before we scale them.
                _trackedPlayer = p;
                _origJumpForce = p.m_jumpForce;
            }

            ApplyGodMode();
            ApplyGhostMode();
            ApplyFly();
            ApplyJump();

            if (State.InfiniteStamina && p.m_stamina < p.GetMaxStamina())
                p.m_stamina = p.GetMaxStamina();

            if (State.InfiniteEitr && p.GetMaxEitr() > 0f && p.m_eitr < p.GetMaxEitr())
                p.m_eitr = p.GetMaxEitr();
        }

        public static void ApplyAll()
        {
            ApplyGodMode();
            ApplyGhostMode();
            ApplyFly();
            ApplyJump();
        }

        public static void ApplyGodMode()
        {
            var p = Player.m_localPlayer;
            if (p != null && p.m_godMode != State.GodMode)
                p.SetGodMode(State.GodMode);
        }

        public static void ApplyGhostMode()
        {
            var p = Player.m_localPlayer;
            if (p != null && p.InGhostMode() != State.GhostMode)
                p.SetGhostMode(State.GhostMode);
        }

        public static void ApplyFly()
        {
            var p = Player.m_localPlayer;
            if (p != null && p.m_debugFly != State.Fly)
                p.ToggleDebugFly();
        }

        public static void ApplyJump()
        {
            var p = Player.m_localPlayer;
            if (p == null || !ReferenceEquals(p, _trackedPlayer)) return;
            float target = State.JumpHack ? _origJumpForce * State.JumpMultiplier : _origJumpForce;
            if (!Mathf.Approximately(p.m_jumpForce, target))
                p.m_jumpForce = target;
        }

        public static void HealFull()
        {
            var p = Player.m_localPlayer;
            if (p == null) return;
            p.SetHealth(p.GetMaxHealth());
            p.AddStamina(p.GetMaxStamina());
            if (p.GetMaxEitr() > 0f) p.AddEitr(p.GetMaxEitr());
        }
    }
}
