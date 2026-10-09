using System.Collections.Generic;
using UnityEngine;

namespace TheDash
{
    /// <summary>
    /// Endless track. Obstacle "patterns" are generated just ahead of the camera and recycled once they
    /// scroll off-screen, so the path never ends and memory stays flat. Pattern sizes scale with the
    /// current jump length, so every obstacle is always clearable no matter how fast you're going.
    /// </summary>
    public class World : MonoBehaviour
    {
        // ------------------------------------------------------------ tuning
        public const float StartSpeed = 9f, MaxSpeed = 16.5f;
        public static float SpeedAt(float distance) => StartSpeed + (MaxSpeed - StartSpeed) * (1f - Mathf.Exp(-distance / 1800f));
        public static float JumpLengthAt(float distance) => SpeedAt(distance) * Player.AirTime;
        public static float DifficultyAt(float distance) => Mathf.Clamp01(distance / 2500f);

        const float GroundDepth = 14f;

        // ------------------------------------------------------------ entities
        class Solid
        {
            public float x0, x1, y0, y1;
            public bool ground;
            public GameObject go;
            public readonly List<SpriteRenderer> tint = new List<SpriteRenderer>();
        }

        class Spike
        {
            public float x, y;
            public GameObject go;
            public SpriteRenderer sr, glow;
        }

        class Coin
        {
            public Vector2 pos;
            public int arc;
            public float phase;
            public bool magnetised;
            public GameObject go;
        }

        public enum PickupType { Shield, Magnet }

        class Pickup
        {
            public PickupType type;
            public Vector2 pos;
            public GameObject go;
        }

        readonly List<Solid> solids = new List<Solid>();
        readonly List<Spike> spikes = new List<Spike>();
        readonly List<Coin> coins = new List<Coin>();
        readonly List<Pickup> pickups = new List<Pickup>();
        readonly Dictionary<int, int> arcRemaining = new Dictionary<int, int>();

        readonly Stack<GameObject> groundPool = new Stack<GameObject>(), blockPool = new Stack<GameObject>(),
                                   spikePool = new Stack<GameObject>(), coinPool = new Stack<GameObject>();

        Theme theme = Theme.All[0];
        float cursor, groundOpenFrom, lastPickupAt;
        bool groundOpen, groundCapLeft;
        int nextArc, lastPattern = -1;
        System.Random rnd = new System.Random();
        SpriteRenderer bestFlag, bestFlagGlow;

        public float MagnetTime { get; set; }
        public Fx fx;

        // ------------------------------------------------------------ lifecycle

        public void Build(Fx fx)
        {
            this.fx = fx;
            var flag = new GameObject("BestFlag");
            flag.transform.SetParent(transform, false);
            bestFlag = flag.AddComponent<SpriteRenderer>();
            bestFlag.sprite = Art.Flag;
            bestFlag.sortingOrder = 1;
            bestFlag.transform.localScale = Vector3.one * 0.9f;
            bestFlagGlow = new GameObject("glow").AddComponent<SpriteRenderer>();
            bestFlagGlow.transform.SetParent(flag.transform, false);
            bestFlagGlow.transform.localPosition = new Vector3(0.6f, 3.5f, 0);
            bestFlagGlow.transform.localScale = Vector3.one * 2.5f;
            bestFlagGlow.sprite = Art.SoftDot;
            bestFlagGlow.sortingOrder = 0;
        }

        public void ResetWorld()
        {
            DespawnAll();
            rnd = new System.Random();
            nextArc = 0;
            lastPattern = -1;
            lastPickupAt = 0;
            MagnetTime = 0;
            cursor = -20;
            OpenGround(cursor);
            cursor = 30;              // safe flat intro
            for (int i = 0; i < 6; i++) AddCoin(14 + i * 1.2f, 0.6f, -1);
            FlushGround();

            int best = SaveData.Best;
            bestFlag.gameObject.SetActive(best > 40);
            bestFlag.transform.position = new Vector3(best, 0, 0);
        }

        /// <summary>After a revive: wipe everything near the player and give them a clean runway.</summary>
        public void ClearForRevive(float playerX)
        {
            DespawnAll();
            cursor = Mathf.Floor(playerX) - 14;
            OpenGround(cursor);
            cursor += 34;
            FlushGround();
        }

        void DespawnAll()
        {
            for (int i = solids.Count - 1; i >= 0; i--) RemoveSolid(i);
            for (int i = spikes.Count - 1; i >= 0; i--) RemoveSpike(i);
            for (int i = coins.Count - 1; i >= 0; i--) RemoveCoin(i);
            for (int i = pickups.Count - 1; i >= 0; i--) RemovePickup(i);
            arcRemaining.Clear();
            groundOpen = false;
        }

        /// <summary>Generate ahead of <paramref name="viewRight"/> and recycle behind <paramref name="viewLeft"/>.</summary>
        public void Tick(float viewLeft, float viewRight, Player player, float dt)
        {
            while (cursor < viewRight + 30f) NextPattern();

            float cut = viewLeft - 3f;
            for (int i = solids.Count - 1; i >= 0; i--) if (solids[i].x1 < cut) RemoveSolid(i);
            for (int i = spikes.Count - 1; i >= 0; i--) if (spikes[i].x + 1 < cut) RemoveSpike(i);
            for (int i = coins.Count - 1; i >= 0; i--) if (coins[i].pos.x < cut) RemoveCoin(i);
            for (int i = pickups.Count - 1; i >= 0; i--) if (pickups[i].pos.x < cut) RemovePickup(i);

            AnimateCollectibles(player, dt);
        }

        // ------------------------------------------------------------ pattern generation

        void NextPattern()
        {
            float d = DifficultyAt(cursor);
            float speed = SpeedAt(cursor);
            float L = speed * Player.AirTime;

            // weights: flat, spike1, spike2, spike3, gap, step, stepSpike, tall, island, rhythm
            float[] w =
            {
                Mathf.Lerp(2.0f, 0.6f, d),
                3f,
                2f + 2f * d,
                d > 0.2f ? 1f + 2f * d : 0f,
                d > 0.08f ? 2f : 0f,
                2f,
                d > 0.3f ? 2f : 0f,
                d > 0.35f ? 1.5f : 0f,
                d > 0.18f ? 1.6f : 0f,
                d > 0.22f ? 2f : 0f,
            };
            if (lastPattern >= 0) w[lastPattern] *= 0.25f;   // avoid repeats
            int pick = WeightedPick(w);
            lastPattern = pick;

            float x = cursor;
            float len;
            switch (pick)
            {
                case 0: len = PatFlat(x); break;
                case 1: len = PatSpikes(x, 1, L); break;
                case 2: len = PatSpikes(x, 2, L); break;
                case 3: len = PatSpikes(x, 3, L); break;
                case 4: len = PatGap(x, L); break;
                case 5: len = PatStep(x, L, false); break;
                case 6: len = PatStep(x, L, true); break;
                case 7: len = PatTall(x, L); break;
                case 8: len = PatIsland(x, L); break;
                default: len = PatRhythm(x, L, speed); break;
            }

            // Breathing room after every pattern: one full jump length plus a reaction window, so a jump
            // over this obstacle never lands you straight into the next one. The window tightens with difficulty.
            float reaction = Mathf.Lerp(0.42f, 0.22f, d);
            float rest = Mathf.Ceil(Mathf.Max(3f, L + speed * reaction - 2.5f));
            float restStart = x + len;
            if (restStart - lastPickupAt > 260f && rnd.NextDouble() < 0.3 && cursor > 120)
            {
                lastPickupAt = restStart;
                rest += 3;
                AddPickup(rnd.NextDouble() < 0.5 ? PickupType.Shield : PickupType.Magnet, restStart + rest * 0.5f, 1.4f);
            }
            cursor = Mathf.Ceil(restStart + rest);
            FlushGround();
        }

        int WeightedPick(float[] w)
        {
            float total = 0;
            foreach (var v in w) total += v;
            float r = (float)rnd.NextDouble() * total;
            for (int i = 0; i < w.Length; i++)
            {
                r -= w[i];
                if (r <= 0) return i;
            }
            return 0;
        }

        float PatFlat(float x)
        {
            int len = rnd.Next(4, 9);
            if (rnd.NextDouble() < 0.6)
                for (int i = 1; i < len - 1; i++) AddCoin(x + i + 0.5f, 0.6f, -1);
            return len;
        }

        float PatSpikes(float x, int n, float L)
        {
            for (int i = 0; i < n; i++) AddSpike(x + 1 + i, 0);
            AddArc(x + 1 + n * 0.5f, 0, L);
            return n + 2;
        }

        float PatGap(float x, float L)
        {
            int w = rnd.Next(2, Mathf.Clamp(Mathf.FloorToInt(L * 0.5f), 2, 5) + 1);
            CloseGround(x + 1);
            OpenGround(x + 1 + w);
            AddArc(x + 1 + w * 0.5f, 0, L);
            return w + 2;
        }

        float PatStep(float x, float L, bool withSpike)
        {
            float bx = x + 1;
            if (!withSpike)
            {
                int m = rnd.Next(3, 7);
                AddBlocks(bx, m, 0, 1);
                for (int i = 0; i < m; i++) AddCoin(bx + i + 0.5f, 1.6f, -1);
                return m + 1;
            }
            // Player can land anywhere up to ~0.8 jump lengths onto the block, then needs time to react.
            float speed = L / Player.AirTime;
            int s = Mathf.CeilToInt(0.8f * L + 0.3f * speed);
            int len = s + 3;
            AddBlocks(bx, len, 0, 1);
            AddSpike(bx + s, 1);
            AddArc(bx + s + 0.5f, 1, L);
            return len + 1;
        }

        float PatTall(float x, float L)
        {
            int m = rnd.Next(2, 4);
            AddBlocks(x + 1, m, 0, 2);
            for (int i = 0; i < m; i++) AddCoin(x + 1 + i + 0.5f, 2.6f, -1);
            return m + 1;
        }

        float PatIsland(float x, float L)
        {
            int g1 = Mathf.Max(2, Mathf.RoundToInt(L * 0.4f));
            int island = Mathf.Max(3, Mathf.RoundToInt(L * 0.6f) + 1);
            int g2 = g1;
            CloseGround(x + 1);
            AddBlocks(x + 1 + g1, island, -0.0f, 1);
            OpenGround(x + 1 + g1 + island + g2);
            for (int i = 1; i < island - 1; i++) AddCoin(x + 1 + g1 + i + 0.5f, 1.6f, -1);
            return 1 + g1 + island + g2 + 1;
        }

        float PatRhythm(float x, float L, float speed)
        {
            int n = rnd.Next(2, 4);
            int spacing = Mathf.CeilToInt(L + speed * 0.22f);
            for (int i = 0; i < n; i++)
            {
                AddSpike(x + 1 + i * spacing, 0);
                AddArc(x + 1.5f + i * spacing, 0, L);
            }
            return 2 + (n - 1) * spacing + 1;
        }

        void AddArc(float centerX, float surface, float L)
        {
            int arc = nextArc++;
            const int n = 5;
            arcRemaining[arc] = n;
            for (int i = 0; i < n; i++)
            {
                float u = 0.15f + 0.7f * i / (n - 1);
                float x = centerX + (u - 0.5f) * L;
                float y = surface + 4f * Player.JumpHeight * u * (1 - u) + 0.1f;
                AddCoin(x, y, arc);
            }
        }

        // ------------------------------------------------------------ building blocks

        void OpenGround(float at)
        {
            groundOpen = true;
            groundOpenFrom = at;
            groundCapLeft = true;   // this segment starts at the far side of a pit
        }

        void CloseGround(float at)
        {
            if (!groundOpen) return;
            AddGround(groundOpenFrom, at, groundCapLeft, true);
            groundOpen = false;
        }

        void FlushGround()
        {
            if (!groundOpen || cursor <= groundOpenFrom) return;
            AddGround(groundOpenFrom, cursor, groundCapLeft, false);
            groundOpenFrom = cursor;
            groundCapLeft = false;
        }

        void AddGround(float x0, float x1, bool capLeft, bool capRight)
        {
            if (x1 - x0 < 0.01f) return;
            var go = groundPool.Count > 0 ? groundPool.Pop() : NewGround();
            go.SetActive(true);
            float w = x1 - x0;
            go.transform.position = new Vector3(x0, 0, 0);
            var fill = go.transform.GetChild(0).GetComponent<SpriteRenderer>();
            var grid = go.transform.GetChild(1).GetComponent<SpriteRenderer>();
            var edge = go.transform.GetChild(2).GetComponent<SpriteRenderer>();
            fill.transform.localPosition = new Vector3(w * 0.5f, -GroundDepth * 0.5f, 0);
            fill.transform.localScale = new Vector3(w, GroundDepth, 1);
            grid.size = new Vector2(w, GroundDepth);
            edge.size = new Vector2(w, 1f);
            edge.transform.localPosition = new Vector3(w * 0.5f, 0.06f, 0);
            var capL = go.transform.GetChild(3).GetComponent<SpriteRenderer>();
            var capR = go.transform.GetChild(4).GetComponent<SpriteRenderer>();
            capL.gameObject.SetActive(capLeft);
            capR.gameObject.SetActive(capRight);
            capL.transform.localPosition = new Vector3(0.05f, -GroundDepth * 0.5f + 0.06f, 0);
            capR.transform.localPosition = new Vector3(w - 0.05f, -GroundDepth * 0.5f + 0.06f, 0);
            var s = new Solid { x0 = x0, x1 = x1, y0 = -GroundDepth, y1 = 0, ground = true, go = go };
            s.tint.Add(fill);
            s.tint.Add(grid);
            s.tint.Add(edge);
            s.tint.Add(capL);
            s.tint.Add(capR);
            TintSolid(s);
            solids.Add(s);
        }

        GameObject NewGround()
        {
            var go = new GameObject("Ground");
            go.transform.SetParent(transform, false);
            var fill = new GameObject("fill").AddComponent<SpriteRenderer>();
            fill.transform.SetParent(go.transform, false);
            fill.sprite = Art.Pixel;
            fill.sortingOrder = 0;
            var grid = new GameObject("grid").AddComponent<SpriteRenderer>();
            grid.transform.SetParent(go.transform, false);
            grid.sprite = Art.GroundGrid;
            grid.drawMode = SpriteDrawMode.Tiled;
            grid.sortingOrder = 1;
            var edge = new GameObject("edge").AddComponent<SpriteRenderer>();
            edge.transform.SetParent(go.transform, false);
            edge.sprite = Art.GroundEdge;
            edge.drawMode = SpriteDrawMode.Tiled;
            edge.sortingOrder = 2;
            for (int i = 0; i < 2; i++)
            {
                var cap = new GameObject("cap").AddComponent<SpriteRenderer>();
                cap.transform.SetParent(go.transform, false);
                cap.sprite = Art.Pixel;
                cap.transform.localScale = new Vector3(0.1f, GroundDepth, 1);
                cap.sortingOrder = 2;
            }
            return go;
        }

        void AddBlocks(float x0, int w, float y0, int h)
        {
            var root = new GameObject("Blocks");
            root.transform.SetParent(transform, false);
            root.transform.position = new Vector3(x0, y0, 0);
            var s = new Solid { x0 = x0, x1 = x0 + w, y0 = y0, y1 = y0 + h, ground = false, go = root };
            for (int ix = 0; ix < w; ix++)
            for (int iy = 0; iy < h; iy++)
            {
                var b = blockPool.Count > 0 ? blockPool.Pop() : NewBlock();
                b.SetActive(true);
                b.transform.SetParent(root.transform, false);
                b.transform.localPosition = new Vector3(ix + 0.5f, iy + 0.5f, 0);
                s.tint.Add(b.GetComponent<SpriteRenderer>());
            }
            TintSolid(s);
            solids.Add(s);
        }

        GameObject NewBlock()
        {
            var go = new GameObject("block");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Art.Block;
            sr.sortingOrder = 3;
            return go;
        }

        void AddSpike(float x, float y)
        {
            var go = spikePool.Count > 0 ? spikePool.Pop() : NewSpike();
            go.SetActive(true);
            go.transform.position = new Vector3(x + 0.5f, y, 0);
            var sp = new Spike { x = x, y = y, go = go, sr = go.GetComponent<SpriteRenderer>(), glow = go.transform.GetChild(0).GetComponent<SpriteRenderer>() };
            TintSpike(sp);
            spikes.Add(sp);
        }

        GameObject NewSpike()
        {
            var go = new GameObject("spike");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Art.Spike;
            sr.sortingOrder = 5;
            var glow = new GameObject("glow").AddComponent<SpriteRenderer>();
            glow.transform.SetParent(go.transform, false);
            glow.transform.localPosition = new Vector3(0, 0.4f, 0);
            glow.transform.localScale = Vector3.one * 2.2f;
            glow.sprite = Art.SoftDot;
            glow.sortingOrder = 4;
            return go;
        }

        void AddCoin(float x, float y, int arc)
        {
            var go = coinPool.Count > 0 ? coinPool.Pop() : NewCoin();
            go.SetActive(true);
            go.transform.position = new Vector3(x, y, 0);
            coins.Add(new Coin { pos = new Vector2(x, y), arc = arc, go = go, phase = x * 0.7f });
        }

        GameObject NewCoin()
        {
            var go = new GameObject("coin");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Art.Coin;
            sr.sortingOrder = 8;
            return go;
        }

        void AddPickup(PickupType type, float x, float y)
        {
            var go = new GameObject(type.ToString());
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = type == PickupType.Shield ? Art.ShieldPickup : Art.MagnetPickup;
            sr.sortingOrder = 9;
            go.transform.position = new Vector3(x, y, 0);
            pickups.Add(new Pickup { type = type, pos = new Vector2(x, y), go = go });
        }

        void RemoveSolid(int i)
        {
            var s = solids[i];
            if (s.ground)
            {
                s.go.SetActive(false);
                groundPool.Push(s.go);
            }
            else
            {
                foreach (var sr in s.tint)
                {
                    sr.gameObject.SetActive(false);
                    sr.transform.SetParent(transform, false);
                    blockPool.Push(sr.gameObject);
                }
                Destroy(s.go);
            }
            solids.RemoveAt(i);
        }

        void RemoveSpike(int i)
        {
            spikes[i].go.SetActive(false);
            spikePool.Push(spikes[i].go);
            spikes.RemoveAt(i);
        }

        void RemoveCoin(int i)
        {
            coins[i].go.SetActive(false);
            coinPool.Push(coins[i].go);
            coins.RemoveAt(i);
        }

        void RemovePickup(int i)
        {
            Destroy(pickups[i].go);
            pickups.RemoveAt(i);
        }

        // ------------------------------------------------------------ theme

        public void ApplyTheme(Theme t)
        {
            theme = t;
            foreach (var s in solids) TintSolid(s);
            foreach (var s in spikes) TintSpike(s);
            var gold = new Color(1f, 0.85f, 0.3f);
            bestFlag.color = gold;
            bestFlagGlow.color = new Color(gold.r, gold.g, gold.b, 0.35f);
        }

        void TintSolid(Solid s)
        {
            if (s.ground)
            {
                s.tint[0].color = theme.groundFill;
                s.tint[1].color = theme.groundEdge;
                s.tint[2].color = theme.groundEdge;
                s.tint[3].color = theme.groundEdge;
                s.tint[4].color = theme.groundEdge;
            }
            else
            {
                foreach (var sr in s.tint) sr.color = theme.groundEdge;
            }
        }

        void TintSpike(Spike s)
        {
            s.sr.color = theme.hazard;
            s.glow.color = new Color(theme.hazard.r, theme.hazard.g, theme.hazard.b, 0.35f);
        }

        // ------------------------------------------------------------ collision queries (all AABB)

        public bool SideHit(Vector2 p, float half)
        {
            float l = p.x - half, r = p.x + half, b = p.y - half, t = p.y + half;
            foreach (var s in solids)
            {
                if (r <= s.x0 || l >= s.x1) continue;
                if (b >= s.y1 - 0.22f) continue;     // standing on / above it
                if (t <= s.y0 + 0.15f) continue;     // passing below it
                return true;
            }
            return false;
        }

        public bool TryLand(float x, float half, float oldBottom, float newBottom, out float top)
        {
            top = float.MinValue;
            float l = x - half + 0.02f, r = x + half - 0.02f;
            bool found = false;
            foreach (var s in solids)
            {
                if (r <= s.x0 || l >= s.x1) continue;
                if (s.y1 <= oldBottom + 0.25f && s.y1 >= newBottom - 0.001f && s.y1 > top)
                {
                    top = s.y1;
                    found = true;
                }
            }
            return found;
        }

        public bool TryHead(float x, float half, float headY, out float bottom)
        {
            bottom = 0;
            float l = x - half + 0.05f, r = x + half - 0.05f;
            foreach (var s in solids)
            {
                if (s.ground || r <= s.x0 || l >= s.x1) continue;
                if (headY > s.y0 && headY < s.y0 + 0.4f)
                {
                    bottom = s.y0;
                    return true;
                }
            }
            return false;
        }

        public bool HazardHit(Vector2 p, float half)
        {
            float h = half - 0.08f;   // forgiving hitbox
            float l = p.x - h, r = p.x + h, b = p.y - h, t = p.y + h;
            foreach (var s in spikes)
            {
                if (Overlap(l, r, b, t, s.x + 0.2f, s.x + 0.8f, s.y, s.y + 0.42f)) return true;
                if (Overlap(l, r, b, t, s.x + 0.36f, s.x + 0.64f, s.y + 0.42f, s.y + 0.82f)) return true;
            }
            return false;
        }

        static bool Overlap(float l, float r, float b, float t, float l2, float r2, float b2, float t2)
            => r > l2 && l < r2 && t > b2 && b < t2;

        // ------------------------------------------------------------ death, shield and pickups

        public void Kill(Player player, bool byObstacle)
        {
            if (!player.alive) return;
            if (byObstacle && (player.invulnerable > 0 || player.hasShield))
            {
                if (player.invulnerable <= 0)
                {
                    player.hasShield = false;
                    fx.Explode(player.pos, new Color(0.3f, 0.9f, 1f), Color.white, 18);
                    Sfx.I.Play("shield");
                    Game.I.OnShieldBroken();
                }
                player.invulnerable = Mathf.Max(player.invulnerable, 0.7f);
                Smash(player.pos);
                return;
            }
            player.alive = false;
            Game.I.OnPlayerDied(byObstacle);
        }

        void Smash(Vector2 p)
        {
            for (int i = spikes.Count - 1; i >= 0; i--)
                if (Mathf.Abs(spikes[i].x + 0.5f - p.x) < 2f)
                {
                    fx.Burst(new Vector2(spikes[i].x + 0.5f, spikes[i].y + 0.4f), theme.hazard, 10, 6f, 0.2f, 0.6f, 12f);
                    RemoveSpike(i);
                }
            for (int i = solids.Count - 1; i >= 0; i--)
            {
                var s = solids[i];
                if (s.ground || p.x + 1.5f < s.x0 || p.x - 1.5f > s.x1) continue;
                if (p.y - Player.Half >= s.y1 - 0.22f) continue; // we're on top of it, keep it
                fx.Burst(new Vector2((s.x0 + s.x1) * 0.5f, (s.y0 + s.y1) * 0.5f), theme.groundEdge, 14, 7f, 0.25f, 0.7f, 12f);
                RemoveSolid(i);
            }
        }

        public void Collect(Player player)
        {
            Vector2 p = player.pos;
            for (int i = coins.Count - 1; i >= 0; i--)
            {
                var c = coins[i];
                if ((c.pos - p).sqrMagnitude > 0.8f * 0.8f) continue;
                fx.Burst(c.pos, new Color(1f, 0.85f, 0.3f), 5, 3f, 0.1f, 0.3f, 2f);
                bool perfect = false;
                if (c.arc >= 0 && arcRemaining.TryGetValue(c.arc, out int left))
                {
                    left--;
                    arcRemaining[c.arc] = left;
                    perfect = left == 0;
                }
                RemoveCoin(i);
                Game.I.OnCoin(perfect);
            }
            for (int i = pickups.Count - 1; i >= 0; i--)
            {
                var pk = pickups[i];
                if ((pk.pos - p).sqrMagnitude > 1.1f * 1.1f) continue;
                fx.Ring(pk.pos, pk.type == PickupType.Shield ? new Color(0.3f, 0.9f, 1f) : new Color(1f, 0.35f, 0.45f), 4f);
                RemovePickup(i);
                Game.I.OnPickup(pk.type);
            }
        }

        void AnimateCollectibles(Player player, float dt)
        {
            float time = Time.time;
            if (MagnetTime > 0) MagnetTime -= dt;
            foreach (var c in coins)
            {
                if (MagnetTime > 0 && player.alive)
                {
                    Vector2 to = player.pos - c.pos;
                    if (c.magnetised || to.sqrMagnitude < 6f * 6f)
                    {
                        c.magnetised = true;
                        c.pos += to.normalized * Mathf.Min(to.magnitude, (World.SpeedAt(player.pos.x) + 14f) * dt);
                    }
                }
                float spin = Mathf.Abs(Mathf.Cos(time * 3f + c.phase));
                c.go.transform.position = new Vector3(c.pos.x, c.pos.y + Mathf.Sin(time * 4f + c.phase) * 0.06f, 0);
                c.go.transform.localScale = new Vector3(0.62f * Mathf.Max(0.15f, spin), 0.62f, 1);
            }
            foreach (var pk in pickups)
            {
                pk.go.transform.position = new Vector3(pk.pos.x, pk.pos.y + Mathf.Sin(time * 3f) * 0.15f, 0);
                float s = 1f + Mathf.Sin(time * 6f) * 0.06f;
                pk.go.transform.localScale = new Vector3(s, s, 1);
            }
        }
    }
}
