using System.Collections.Generic;
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

        /// <summary>Private wind for the ship you steer; null = vanilla. Never written to EnvMan, so waves stay natural.</summary>
        public static Vector3? TailwindDir;

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

            // Ship sank / unloaded / changed owner while carrying a cart: let it go.
            List<Vagon>? gone = null;
            foreach (var kv in _riders)
                if (kv.Key == null || kv.Value.Ship == null || kv.Value.Ship.m_body == null) (gone ??= new List<Vagon>()).Add(kv.Key!);
            if (gone != null) foreach (var c in gone) Release(c);
        }

        private sealed class Rider
        {
            public Ship Ship = null!;
            public Rigidbody[] Bodies = null!;
            public Pose[] Local = null!;
            public Collider[] Cols = null!, ShipCols = null!;
        }

        private static readonly Dictionary<Vagon, Rider> _riders = new Dictionary<Vagon, Rider>();

        /// <summary>
        /// Carts ride ships: a loose cart on the deck turns kinematic, stops colliding with the hull and is moved to its
        /// deck-relative pose every physics step. No contacts means it can't drag the ship. Hitching it releases it.
        /// </summary>
        public static void RideShip(Ship ship)
        {
            var sb = ship.m_body;
            var box = ship.m_floatCollider;
            if (sb == null || box == null) return;

            if (State.CartRidesShips)
                foreach (var cart in Vagon.m_instances)
                {
                    if (cart == null || _riders.ContainsKey(cart) || !Loose(cart)) continue;
                    var local = box.transform.InverseTransformPoint(cart.transform.position) - box.center;
                    if (Mathf.Abs(local.x) > box.size.x * 0.5f || Mathf.Abs(local.z) > box.size.z * 0.5f || local.y < -2f || local.y > 6f) continue;
                    Capture(cart, ship);
                }

            // Ship pose at the end of this step, so the cart doesn't trail one frame behind at speed.
            float dt = Time.fixedDeltaTime;
            var w = sb.angularVelocity;
            var rot = w.sqrMagnitude > 1e-6f ? Quaternion.AngleAxis(w.magnitude * Mathf.Rad2Deg * dt, w.normalized) * sb.rotation : sb.rotation;
            var pos = sb.position + sb.linearVelocity * dt;

            List<Vagon>? drop = null;
            foreach (var kv in _riders)
            {
                if (!ReferenceEquals(kv.Value.Ship, ship)) continue;
                if (!State.CartRidesShips || !Loose(kv.Key)) { (drop ??= new List<Vagon>()).Add(kv.Key); continue; }
                var r = kv.Value;
                for (int i = 0; i < r.Bodies.Length; i++)
                {
                    if (r.Bodies[i] == null) continue;
                    r.Bodies[i].MovePosition(pos + rot * r.Local[i].position);
                    r.Bodies[i].MoveRotation(rot * r.Local[i].rotation);
                }
            }
            if (drop != null) foreach (var c in drop) Release(c);
        }

        private static bool Loose(Vagon c) => c != null && !c.IsAttached() && c.m_nview != null && c.m_nview.IsValid() && c.m_nview.IsOwner();

        private static void Capture(Vagon cart, Ship ship)
        {
            var sb = ship.m_body;
            var inv = Quaternion.Inverse(sb.rotation);
            var r = new Rider
            {
                Ship = ship,
                Bodies = cart.m_bodies,
                Local = new Pose[cart.m_bodies.Length],
                Cols = cart.GetComponentsInChildren<Collider>(),
                ShipCols = ship.GetComponentsInChildren<Collider>(),
            };
            for (int i = 0; i < r.Bodies.Length; i++)
            {
                var b = r.Bodies[i];
                r.Local[i] = new Pose(inv * (b.position - sb.position), inv * b.rotation);
                b.isKinematic = true;
            }
            SetIgnore(r, true);
            _riders[cart] = r;
        }

        private static void Release(Vagon cart)
        {
            if (!_riders.TryGetValue(cart, out var r)) return;
            _riders.Remove(cart);
            SetIgnore(r, false);
            var sb = r.Ship != null ? r.Ship.m_body : null;
            foreach (var b in r.Bodies)
            {
                if (b == null) continue;
                b.isKinematic = false;
                if (sb != null) b.linearVelocity = sb.GetPointVelocity(b.worldCenterOfMass);
            }
        }

        private static void SetIgnore(Rider r, bool ignore)
        {
            foreach (var a in r.Cols)
                foreach (var b in r.ShipCols)
                    if (a != null && b != null) Physics.IgnoreCollision(a, b, ignore);
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

            // Only while you're at the helm: wind follows your camera. Passengers get vanilla wind.
            TailwindDir = null;
            if (State.ShipTailwind && ship != null && p != null && ReferenceEquals(p.GetControlledShip(), ship) && GameCamera.instance != null)
            {
                var f = GameCamera.instance.transform.forward;
                f.y = 0f;
                TailwindDir = f.sqrMagnitude > 0.01f ? f.normalized : ship.transform.forward;
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
