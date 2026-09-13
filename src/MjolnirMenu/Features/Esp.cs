using System.Collections.Generic;
using MjolnirMenu.Core;
using UnityEngine;

namespace MjolnirMenu.Features
{
    /// <summary>
    /// Wallhack-style labels. Characters are cheap (game keeps a list); resources need a
    /// FindObjectsOfType sweep so they are rescanned only every couple of seconds.
    /// </summary>
    public static class Esp
    {
        private enum Kind { Player, Creature, Boss, Resource }

        private struct Target
        {
            public Transform Tf;
            public string Label;
            public Kind Kind;
        }

        private static readonly List<Target> _resources = new List<Target>();
        private static float _nextResourceScan;
        private static GUIStyle? _style;

        public static void Tick()
        {
            if (!State.EspResources)
            {
                _resources.Clear();
                return;
            }
            if (Time.unscaledTime < _nextResourceScan) return;
            _nextResourceScan = Time.unscaledTime + 2.5f;
            ScanResources();
        }

        private static void ScanResources()
        {
            _resources.Clear();
            var p = Player.m_localPlayer;
            if (p == null) return;
            var origin = p.transform.position;
            float range = MenuConfig.EspRange.Value;
            float rangeSq = range * range;

            foreach (var pk in Object.FindObjectsByType<Pickable>(FindObjectsSortMode.None))
            {
                if (pk == null || pk.m_picked) continue;
                if ((pk.transform.position - origin).sqrMagnitude > rangeSq) continue;
                _resources.Add(new Target { Tf = pk.transform, Label = pk.GetHoverName(), Kind = Kind.Resource });
            }
            foreach (var rock in Object.FindObjectsByType<MineRock5>(FindObjectsSortMode.None))
            {
                if (rock == null) continue;
                if ((rock.transform.position - origin).sqrMagnitude > rangeSq) continue;
                _resources.Add(new Target { Tf = rock.transform, Label = Localization.instance.Localize(rock.m_name), Kind = Kind.Resource });
            }
            foreach (var tree in Object.FindObjectsByType<TreeBase>(FindObjectsSortMode.None))
            {
                if (tree == null) continue;
                if ((tree.transform.position - origin).sqrMagnitude > rangeSq) continue;
                _resources.Add(new Target { Tf = tree.transform, Label = Utils.GetPrefabName(tree.gameObject), Kind = Kind.Resource });
            }
        }

        public static void Draw()
        {
            if (!State.EspPlayers && !State.EspCreatures && !State.EspResources) return;
            var p = Player.m_localPlayer;
            var cam = Utils.GetMainCamera();
            if (p == null || cam == null) return;

            // The game's skin label wraps by default; a wrapped label grows past the rect we
            // measured and gets clipped. Force single-line, overflow instead of clip, no padding.
            _style ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                clipping = TextClipping.Overflow,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                stretchWidth = false,
                stretchHeight = false,
            };

            var origin = p.transform.position;
            float range = MenuConfig.EspRange.Value;
            int budget = MenuConfig.EspMaxEntities.Value;
            var screenCenter = new Vector2(Screen.width / 2f, Screen.height);

            if (State.EspPlayers || State.EspCreatures)
            {
                foreach (var c in Character.GetAllCharacters())
                {
                    if (budget <= 0) break;
                    if (c == null || ReferenceEquals(c, p) || c.IsDead()) continue;
                    bool isPlayer = c.IsPlayer();
                    if (isPlayer && !State.EspPlayers) continue;
                    if (!isPlayer && !State.EspCreatures) continue;

                    float dist = Vector3.Distance(origin, c.transform.position);
                    if (dist > range) continue;

                    var kind = isPlayer ? Kind.Player : (c.IsBoss() ? Kind.Boss : Kind.Creature);
                    string label = isPlayer ? c.GetHoverName() : $"{Localization.instance.Localize(c.m_name)} ★{c.GetLevel()}";
                    if (!isPlayer) label += $" [{Mathf.CeilToInt(c.GetHealth())}/{Mathf.CeilToInt(c.GetMaxHealth())}]";
                    DrawLabel(cam, c.GetCenterPoint() + Vector3.up * 1.2f, label, dist, kind, screenCenter);
                    budget--;
                }
            }

            if (State.EspResources)
            {
                foreach (var t in _resources)
                {
                    if (budget <= 0) break;
                    if (t.Tf == null) continue;
                    float dist = Vector3.Distance(origin, t.Tf.position);
                    if (dist > range) continue;
                    DrawLabel(cam, t.Tf.position + Vector3.up * 1f, t.Label, dist, t.Kind, screenCenter);
                    budget--;
                }
            }
        }

        private static void DrawLabel(Camera cam, Vector3 world, string text, float dist, Kind kind, Vector2 screenBottom)
        {
            var sp = cam.WorldToScreenPoint(world);
            if (sp.z <= 0f) return; // behind camera
            float x = sp.x;
            float y = Screen.height - sp.y;
            if (x < -50f || x > Screen.width + 50f || y < -20f || y > Screen.height + 20f) return;

            if (State.EspDistance) text += $" {dist:0}m";

            var color = kind switch
            {
                Kind.Player => MenuConfig.EspPlayerColor.Value,
                Kind.Boss => MenuConfig.EspBossColor.Value,
                Kind.Creature => MenuConfig.EspCreatureColor.Value,
                _ => MenuConfig.EspResourceColor.Value,
            };

            var size = _style!.CalcSize(new GUIContent(text));
            // Generous box: width + 8, height + 6, so descenders/★ never touch the edge.
            float w = size.x + 8f, h = Mathf.Max(size.y, 16f) + 6f;
            var rect = new Rect(x - w / 2f, y - h / 2f, w, h);

            // Shadow then colored text: readable on snow and in the dark.
            _style.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
            GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), text, _style);
            _style.normal.textColor = color;
            GUI.Label(rect, text, _style);

            if (State.EspLines)
                DrawLine(screenBottom, new Vector2(x, y), color);
        }

        private static Texture2D? _lineTex;

        private static void DrawLine(Vector2 a, Vector2 b, Color color)
        {
            if (_lineTex == null)
            {
                _lineTex = new Texture2D(1, 1);
                _lineTex.SetPixel(0, 0, Color.white);
                _lineTex.Apply();
            }
            var saved = GUI.matrix;
            var savedColor = GUI.color;
            var delta = b - a;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            GUI.color = new Color(color.r, color.g, color.b, 0.6f);
            GUIUtility.RotateAroundPivot(angle, a);
            GUI.DrawTexture(new Rect(a.x, a.y - 0.5f, delta.magnitude, 1f), _lineTex);
            GUI.matrix = saved;
            GUI.color = savedColor;
        }
    }
}
