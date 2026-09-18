using MjolnirMenu.Core;
using UnityEngine;

namespace MjolnirMenu.Features
{
    /// <summary>Ships and carts. Same reconcile-every-frame + per-instance snapshot pattern as PlayerCheats.</summary>
    public static class Vehicles
    {
        /// <summary>Cart hitched to the local player; maintained by the Vagon patches.</summary>
        public static Vagon? Cart;

        private static Ship? _ship;
        private static float _origSailForce, _origBackwardForce;
        private static bool _windForced;

        private static Vagon? _cart;
        private static bool _cartLight, _cartNoClip;

        /// <summary>No-clip wheels still ride terrain and built floors; everything else is passed through.</summary>
        private static readonly int NoClipExclude = ~LayerMask.GetMask("terrain", "piece");

        public static void Tick()
        {
            var p = Player.m_localPlayer;
            TickShip(p);
            TickCart(p);
        }

        private static void TickShip(Player? p)
        {
            var ship = p != null ? Ship.GetLocalShip() : null;
            if (!ReferenceEquals(ship, _ship))
            {
                RestoreShip();
                _ship = ship;
                if (ship != null) { _origSailForce = ship.m_sailForceFactor; _origBackwardForce = ship.m_backwardForce; }
            }

            var env = EnvMan.instance;
            if (State.ShipTailwind && ship != null && env != null)
            {
                // Game's own debug wind: angle in degrees, dir = (sin, 0, cos) → the bow heading.
                var f = ship.transform.forward;
                env.SetDebugWind(Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg, 1f);
                _windForced = true;
            }
            else if (_windForced)
            {
                if (env != null) env.ResetDebugWind();
                _windForced = false;
            }

            if (ship == null) return;
            float mul = State.ShipSpeedHack ? Mathf.Max(1f, State.ShipSpeedMultiplier) : 1f;
            ship.m_sailForceFactor = _origSailForce * mul;
            ship.m_backwardForce = _origBackwardForce * mul;
        }

        private static void RestoreShip()
        {
            if (_ship == null) return;
            _ship.m_sailForceFactor = _origSailForce;
            _ship.m_backwardForce = _origBackwardForce;
        }

        private static void TickCart(Player? p)
        {
            var cart = Cart != null && p != null && Cart.m_attachedObject == p.gameObject ? Cart : null;
            if (!ReferenceEquals(cart, _cart)) { RestoreCart(p); _cart = cart; }
            if (cart == null || p == null) return;

            // Light cart: the game re-applies mass on inventory changes and pull mass on attach, so reconcile each frame.
            if (State.CartSpeedHack)
            {
                cart.SetMass(1f);
                p.SetExtraMass(0f);
                _cartLight = true;
            }
            else if (_cartLight)
            {
                cart.UpdateMass();
                p.SetExtraMass(cart.m_playerExtraPullMass);
                _cartLight = false;
            }

            if (State.CartNoClip != _cartNoClip)
            {
                _cartNoClip = State.CartNoClip;
                foreach (var b in cart.m_bodies) b.excludeLayers = _cartNoClip ? NoClipExclude : 0;
            }
        }

        private static void RestoreCart(Player? p)
        {
            if (_cart != null)
            {
                if (_cartLight)
                {
                    _cart.UpdateMass();
                    if (p != null && _cart.m_attachedObject == p.gameObject) p.SetExtraMass(_cart.m_playerExtraPullMass);
                }
                if (_cartNoClip) foreach (var b in _cart.m_bodies) b.excludeLayers = 0;
            }
            _cartLight = _cartNoClip = false;
        }
    }
}
