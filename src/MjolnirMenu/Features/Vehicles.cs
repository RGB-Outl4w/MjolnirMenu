using MjolnirMenu.Core;
using UnityEngine;

namespace MjolnirMenu.Features
{
    /// <summary>Ships and carts. Same reconcile-every-frame + per-instance snapshot pattern as PlayerCheats.</summary>
    public static class Vehicles
    {
        /// <summary>Cart hitched to the local player; maintained by the Vagon patches.</summary>
        public static Vagon? Cart;

        /// <summary>Ship the local player is aboard; read by the Ship patches.</summary>
        public static Ship? CurrentShip => _ship;

        private static Ship? _ship;
        private static float _origBackwardForce, _origStearForce, _origStearVelForce, _origRudderSpeed;
        private static bool _windForced;

        private static Vagon? _cart;
        private static bool _cartLight, _cartNoClip;

        /// <summary>No-clip wheels still ride terrain and built floors; everything else is passed through.</summary>
        private static readonly int NoClipExclude = ~LayerMask.GetMask("terrain", "piece");

        public static float SpeedMul => State.ShipSpeedHack ? Mathf.Max(1f, State.ShipSpeedMultiplier) : 1f;

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
                if (ship != null)
                {
                    _origBackwardForce = ship.m_backwardForce;
                    _origStearForce = ship.m_stearForce;
                    _origStearVelForce = ship.m_stearVelForceFactor;
                    _origRudderSpeed = ship.m_rudderSpeed;
                }
            }

            var env = EnvMan.instance;
            if (State.ShipTailwind && ship != null && p != null && env != null)
            {
                // Driver: wind follows the camera. Passenger: wind follows the bow.
                var f = ReferenceEquals(p.GetControlledShip(), ship) && GameCamera.instance != null
                    ? GameCamera.instance.transform.forward : ship.transform.forward;
                f.y = 0f;
                if (f.sqrMagnitude < 0.01f) f = ship.transform.forward;
                f.Normalize();
                env.SetDebugWind(Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg, 1f);
                // Skip the game's slow wind transition: pin the current wind so SetTargetWind sees nothing to blend.
                var w = new Vector4(f.x, 0f, f.z, 1f);
                env.m_wind = env.m_windDir1 = env.m_windDir2 = w;
                env.m_windTransitionTimer = -1f;
                _windForced = true;
            }
            else if (_windForced)
            {
                if (env != null) env.ResetDebugWind();
                _windForced = false;
            }

            if (ship == null) return;
            // Sail force gets its boost at the centre of mass (ShipPatches); paddles push low at the stern, so scaling is safe.
            ship.m_backwardForce = _origBackwardForce * SpeedMul;
            float steer = State.ShipSteering ? Mathf.Max(1f, State.ShipSteeringMultiplier) : 1f;
            ship.m_stearForce = _origStearForce * steer;
            ship.m_stearVelForceFactor = _origStearVelForce * steer;
            ship.m_rudderSpeed = _origRudderSpeed * steer;
        }

        private static void RestoreShip()
        {
            if (_ship == null) return;
            _ship.m_backwardForce = _origBackwardForce;
            _ship.m_stearForce = _origStearForce;
            _ship.m_stearVelForceFactor = _origStearVelForce;
            _ship.m_rudderSpeed = _origRudderSpeed;
        }

        /// <summary>Nearest ship within 50 m: level it, lift it clear of the water and kill its spin.</summary>
        public static bool FlipNearestShip()
        {
            var p = Player.m_localPlayer;
            if (p == null) return false;
            Ship? best = null;
            float bestD = 50f;
            foreach (var s in Ship.s_currentShips)
            {
                float d = Vector3.Distance(s.transform.position, p.transform.position);
                if (d < bestD) { bestD = d; best = s; }
            }
            if (best == null) return false;
            best.m_nview.ClaimOwnership();
            var body = best.m_body;
            body.rotation = Quaternion.Euler(0f, best.transform.eulerAngles.y, 0f);
            body.position += Vector3.up * 2f;
            body.linearVelocity = body.angularVelocity = Vector3.zero;
            return true;
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

            // Sticky hitch: an unbreakable joint; the distance/angle check is overridden in VagonPatches.CanAttach.
            if (cart.m_attachJoin != null)
                cart.m_attachJoin.breakForce = State.CartSticky ? float.PositiveInfinity : cart.m_breakForce;
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

        /// <summary>Swing and slide the cart so its shafts sit exactly where the game wants them behind the player.</summary>
        public static void SnapCart(Vagon cart, Player p)
        {
            var d = cart.m_attachPoint.position - cart.transform.position;
            d.y = 0f;
            var fwd = p.transform.forward;
            fwd.y = 0f;
            var rot = Quaternion.Euler(0f, cart.transform.eulerAngles.y, 0f);
            if (d.sqrMagnitude > 0.001f && fwd.sqrMagnitude > 0.001f) rot = Quaternion.FromToRotation(d, fwd) * rot;
            cart.transform.rotation = rot;
            cart.transform.position += p.transform.position + cart.m_attachOffset - cart.m_attachPoint.position + Vector3.up * 0.3f;
            foreach (var b in cart.m_bodies) b.linearVelocity = b.angularVelocity = Vector3.zero;
            Physics.SyncTransforms();
        }
    }
}
