using System.Collections.Generic;
using UnityEngine;

namespace TheDash
{
    /// <summary>
    /// Every sprite in the game, generated once at startup. Most are drawn in white/grey so they can
    /// be tinted by the active <see cref="Theme"/>; things that should keep their own colours
    /// (coins, power-ups, the player) are drawn in colour.
    /// </summary>
    public static class Art
    {
        static readonly Color W = Color.white;
        static Color Grey(float v, float a = 1f) => new Color(v, v, v, a);

        static Sprite pixel, rounded, roundedSmall, outline, softDot, circle, sky, edge, grid, spike, block, coin,
                      shield, magnet, sun, far, mid, near, stars, horizon, flag, radial;

        public static Sprite Pixel => pixel ? pixel : pixel = MakePixel();
        /// <summary>9-sliced rounded rectangle for UI panels/buttons.</summary>
        public static Sprite Rounded => rounded ? rounded : rounded = MakeRounded(128, 40, false);
        public static Sprite RoundedSmall => roundedSmall ? roundedSmall : roundedSmall = MakeRounded(64, 16, false);
        public static Sprite Outline => outline ? outline : outline = MakeRounded(128, 40, true);
        public static Sprite SoftDot => softDot ? softDot : softDot = MakeSoftDot();
        public static Sprite Circle => circle ? circle : circle = MakeCircle();
        public static Sprite Radial => radial ? radial : radial = MakeCircle();
        public static Sprite SkyGradient => sky ? sky : sky = MakeSkyGradient();
        public static Sprite GroundEdge => edge ? edge : edge = MakeGroundEdge();
        public static Sprite GroundGrid => grid ? grid : grid = MakeGroundGrid();
        public static Sprite Spike => spike ? spike : spike = MakeSpike();
        public static Sprite Block => block ? block : block = MakeBlock();
        public static Sprite Coin => coin ? coin : coin = MakeCoin();
        public static Sprite ShieldPickup => shield ? shield : shield = MakePickup(true);
        public static Sprite MagnetPickup => magnet ? magnet : magnet = MakePickup(false);
        public static Sprite Sun => sun ? sun : sun = MakeSun();
        public static Sprite Mountains => far ? far : far = MakeMountains();
        public static Sprite City => mid ? mid : mid = MakeCity();
        public static Sprite Hills => near ? near : near = MakeHills();
        public static Sprite Stars => stars ? stars : stars = MakeStars();
        public static Sprite Horizon => horizon ? horizon : horizon = MakeHorizon();
        public static Sprite Flag => flag ? flag : flag = MakeFlag();

        static readonly Dictionary<int, Sprite> players = new Dictionary<int, Sprite>();
        static readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();

        // ------------------------------------------------------------------ basics

        static Sprite MakePixel()
        {
            var p = new Painter(4, 4);
            p.Fill((x, y) => -1, W);
            return p.ToSprite(4, new Vector2(0.5f, 0.5f));
        }

        static Sprite MakeRounded(int size, float radius, bool outlineOnly)
        {
            var p = new Painter(size, size);
            float h = size * 0.5f;
            if (outlineOnly)
                p.Fill((x, y) => Mathf.Abs(Sd.Box(x, y, h, h, h - 2, h - 2, radius - 2) + 3f) - 3f, W);
            else
                p.Fill((x, y) => Sd.Box(x, y, h, h, h - 1, h - 1, radius - 1), W);
            float b = radius + 2;
            return p.ToSprite(100, new Vector2(0.5f, 0.5f), new Vector4(b, b, b, b));
        }

        static Sprite MakeSoftDot()
        {
            var p = new Painter(64, 64);
            p.Glow((x, y) => Sd.Circle(x, y, 32, 32, 0), W, 31);
            return p.ToSprite(64, new Vector2(0.5f, 0.5f));
        }

        static Sprite MakeCircle()
        {
            var p = new Painter(128, 128);
            p.Fill((x, y) => Sd.Circle(x, y, 64, 64, 62), W);
            return p.ToSprite(128, new Vector2(0.5f, 0.5f));
        }

        static Sprite MakeSkyGradient()
        {
            // White with alpha 1 at the top fading to 0 at the bottom - overlaid on the bottom sky colour.
            var p = new Painter(4, 256);
            for (int y = 0; y < 256; y++)
            {
                float t = y / 255f;
                for (int x = 0; x < 4; x++) p.Set(x, y, new Color(1, 1, 1, Mathf.SmoothStep(0, 1, t)));
            }
            return p.ToSprite(256, new Vector2(0.5f, 0.5f));
        }

        static Sprite MakeHorizon()
        {
            // Soft band, opaque in the middle - a haze between parallax layers.
            var p = new Painter(4, 128);
            for (int y = 0; y < 128; y++)
            {
                float t = 1f - Mathf.Abs(y - 64) / 64f;
                for (int x = 0; x < 4; x++) p.Set(x, y, new Color(1, 1, 1, t * t));
            }
            return p.ToSprite(128, new Vector2(0.5f, 0.5f));
        }

        // ------------------------------------------------------------------ world

        static Sprite MakeGroundEdge()
        {
            // 1 unit tall: a bright neon line at the top, glowing down into the ground.
            var p = new Painter(4, 64);
            for (int y = 0; y < 64; y++)
            {
                float fromTop = 63 - y;
                float a = fromTop < 5 ? 1f : 0.55f * Mathf.Exp(-(fromTop - 5) / 9f);
                for (int x = 0; x < 4; x++) p.Set(x, y, new Color(1, 1, 1, a));
            }
            return p.ToSprite(64, new Vector2(0.5f, 1f), default, true);
        }

        static Sprite MakeGroundGrid()
        {
            var p = new Painter(64, 64);
            p.Fill((x, y) => Mathf.Min(Mathf.Abs(x - 1), Mathf.Abs(y - 63)) - 1.2f, Grey(1, 0.16f));
            p.Fill((x, y) => Sd.Box(x, y, 32, 32, 3, 3, 1.5f), Grey(1, 0.10f));
            return p.ToSprite(64, new Vector2(0f, 1f), default, true);
        }

        static Sprite MakeSpike()
        {
            // Bright edge + darker core, tinted by the theme's hazard colour.
            var p = new Painter(128, 128);
            var outer = new[] { new Vector2(6, 2), new Vector2(122, 2), new Vector2(64, 124) };
            var inner = new[] { new Vector2(26, 12), new Vector2(102, 12), new Vector2(64, 92) };
            p.Fill((x, y) => Sd.Polygon(x, y, outer), W);
            p.FillGradient((x, y) => Sd.Polygon(x, y, inner), Grey(0.35f), Grey(0.62f), 12, 92);
            return p.ToSprite(128, new Vector2(0.5f, 0f));
        }

        static Sprite MakeBlock()
        {
            var p = new Painter(128, 128);
            p.Fill((x, y) => Sd.Box(x, y, 64, 64, 63, 63, 14), W);
            p.FillGradient((x, y) => Sd.Box(x, y, 64, 64, 53, 53, 8), Grey(0.18f), Grey(0.32f), 11, 117);
            // diagonal hatching
            p.Fill((x, y) => Sd.Intersect(Sd.Box(x, y, 64, 64, 53, 53, 8),
                Mathf.Abs(((x + y) % 32f) - 16f) - 3f), Grey(1, 0.10f));
            p.Fill((x, y) => Sd.Box(x, y, 64, 64, 18, 18, 4), Grey(1, 0.30f));
            return p.ToSprite(128, new Vector2(0.5f, 0.5f));
        }

        static Sprite MakeCoin()
        {
            var p = new Painter(96, 96);
            Color dark = new Color(0.86f, 0.52f, 0.05f), gold = new Color(1f, 0.79f, 0.22f), light = new Color(1f, 0.95f, 0.62f);
            p.Glow((x, y) => Sd.Circle(x, y, 48, 48, 30), new Color(1f, 0.85f, 0.3f, 0.45f), 16);
            p.Fill((x, y) => Sd.Circle(x, y, 48, 48, 32), dark);
            p.Fill((x, y) => Sd.Circle(x, y, 48, 48, 27), gold);
            p.Fill((x, y) => Sd.Ring(x, y, 48, 48, 20, 3), dark);
            var diamond = new[] { new Vector2(48, 62), new Vector2(58, 48), new Vector2(48, 34), new Vector2(38, 48) };
            p.Fill((x, y) => Sd.Polygon(x, y, diamond), light);
            p.Fill((x, y) => Sd.Segment(x, y, 34, 66, 40, 72, 2.5f), new Color(1, 1, 1, 0.85f));
            return p.ToSprite(96, new Vector2(0.5f, 0.5f));
        }

        static Sprite MakePickup(bool isShield)
        {
            var p = new Painter(128, 128);
            Color col = isShield ? new Color(0.25f, 0.9f, 1f) : new Color(1f, 0.33f, 0.45f);
            p.Glow((x, y) => Sd.Circle(x, y, 64, 64, 44), new Color(col.r, col.g, col.b, 0.55f), 20);
            p.Fill((x, y) => Sd.Circle(x, y, 64, 64, 46), new Color(0.08f, 0.05f, 0.18f, 0.9f));
            p.Fill((x, y) => Sd.Ring(x, y, 64, 64, 44, 5), col);
            if (isShield) DrawShield(p, 64, 62, 1.15f, col);
            else DrawMagnet(p, 64, 60, 1.1f, col);
            return p.ToSprite(128, new Vector2(0.5f, 0.5f));
        }

        static void DrawShield(Painter p, float cx, float cy, float s, Color c)
        {
            var poly = new[]
            {
                new Vector2(cx - 22 * s, cy + 22 * s), new Vector2(cx, cy + 30 * s), new Vector2(cx + 22 * s, cy + 22 * s),
                new Vector2(cx + 20 * s, cy - 4 * s), new Vector2(cx, cy - 28 * s), new Vector2(cx - 20 * s, cy - 4 * s),
            };
            p.Fill((x, y) => Sd.Polygon(x, y, poly), c);
            p.Fill((x, y) => Sd.Segment(x, y, cx - 9 * s, cy + 1 * s, cx - 2 * s, cy - 7 * s, 3.5f * s), Color.white);
            p.Fill((x, y) => Sd.Segment(x, y, cx - 2 * s, cy - 7 * s, cx + 11 * s, cy + 11 * s, 3.5f * s), Color.white);
        }

        static void DrawMagnet(Painter p, float cx, float cy, float s, Color c)
        {
            float r = 16 * s, t = 11 * s;
            p.Fill((x, y) => Sd.Intersect(Sd.Ring(x, y, cx, cy - 2 * s, r, t), y - (cy - 2 * s)), c);
            p.Fill((x, y) => Sd.Box(x, y, cx - r, cy + 9 * s, t * 0.5f, 11 * s), c);
            p.Fill((x, y) => Sd.Box(x, y, cx + r, cy + 9 * s, t * 0.5f, 11 * s), c);
            p.Fill((x, y) => Sd.Box(x, y, cx - r, cy + 17 * s, t * 0.5f, 4 * s), Color.white);
            p.Fill((x, y) => Sd.Box(x, y, cx + r, cy + 17 * s, t * 0.5f, 4 * s), Color.white);
        }

        static Sprite MakeFlag()
        {
            var p = new Painter(64, 256);
            p.Fill((x, y) => Sd.Box(x, y, 6, 128, 3, 128), W);
            var flagPoly = new[] { new Vector2(8, 252), new Vector2(60, 230), new Vector2(8, 208) };
            p.Fill((x, y) => Sd.Polygon(x, y, flagPoly), W);
            return p.ToSprite(64, new Vector2(0.1f, 0f));
        }

        public static Sprite Player(int skinIndex)
        {
            if (players.TryGetValue(skinIndex, out var s) && s) return s;
            var skin = Skin.All[skinIndex];
            var p = new Painter(128, 128);
            Color body = skin.body, accent = skin.accent;
            Color dark = Color.Lerp(body, Color.black, 0.55f);
            p.Fill((x, y) => Sd.Box(x, y, 64, 64, 62, 62, 22), dark);
            p.FillGradient((x, y) => Sd.Box(x, y, 64, 64, 52, 52, 16), Color.Lerp(body, Color.black, 0.18f), body, 12, 116);
            // accent stripe
            p.Fill((x, y) => Sd.Intersect(Sd.Box(x, y, 64, 64, 52, 52, 16), Sd.Box(x, y, 64, 30, 60, 9)), accent);
            // gloss
            p.Fill((x, y) => Sd.Box(x, y, 40, 100, 20, 6, 6), new Color(1, 1, 1, 0.45f));
            // eyes looking forward (to the right)
            Color eye = Color.white, pupil = new Color(0.07f, 0.05f, 0.15f);
            p.Fill((x, y) => Sd.Box(x, y, 66, 70, 9, 15, 8), eye);
            p.Fill((x, y) => Sd.Box(x, y, 94, 70, 9, 15, 8), eye);
            p.Fill((x, y) => Sd.Box(x, y, 71, 68, 5, 9, 5), pupil);
            p.Fill((x, y) => Sd.Box(x, y, 99, 68, 5, 9, 5), pupil);
            // determined brows
            p.Fill((x, y) => Sd.Segment(x, y, 56, 92, 74, 88, 3), dark);
            p.Fill((x, y) => Sd.Segment(x, y, 86, 88, 104, 92, 3), dark);
            s = p.ToSprite(128, new Vector2(0.5f, 0.5f));
            players[skinIndex] = s;
            return s;
        }

        // ------------------------------------------------------------------ backgrounds

        static Sprite MakeSun()
        {
            var p = new Painter(256, 256);
            p.Glow((x, y) => Sd.Circle(x, y, 128, 128, 96), Grey(1, 0.35f), 30);
            // synthwave sun: gradient disc with horizontal cut-outs in the lower half
            p.FillGradient((x, y) =>
            {
                float d = Sd.Circle(x, y, 128, 128, 96);
                if (y < 128)
                {
                    float band = (128 - y);
                    float gap = 3f + band / 14f;          // stripes get thicker towards the bottom
                    float m = band % 22f;
                    if (m < gap) d = Mathf.Max(d, Mathf.Min(m, gap - m));
                }
                return d;
            }, Grey(0.72f), W, 32, 224);
            return p.ToSprite(40, new Vector2(0.5f, 0.5f));
        }

        static Sprite MakeMountains()
        {
            const int w = 1024, h = 256;
            var p = new Painter(w, h);
            var rnd = new System.Random(7);
            int[] ks = { 2, 3, 5, 7, 11, 13 };
            float[] amp = { 70, 45, 30, 18, 10, 6 };
            float[] ph = new float[ks.Length];
            for (int i = 0; i < ph.Length; i++) ph[i] = (float)rnd.NextDouble() * Mathf.PI;
            float[] height = new float[w];
            for (int x = 0; x < w; x++)
            {
                float v = 40;
                for (int i = 0; i < ks.Length; i++)
                    v += amp[i] * (1f - Mathf.Abs(Mathf.Sin(Mathf.PI * ks[i] * x / w + ph[i])));
                height[x] = v;
            }
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float d = y - height[x];
                float a = Mathf.Clamp01(0.5f - d);
                if (a <= 0) continue;
                float shade = Mathf.Lerp(0.78f, 1f, y / (float)h);
                // lighter snowy ridges
                if (height[x] - y < 10 && height[x] > 180) shade = 1.2f;
                p.Blend(x, y, Grey(Mathf.Min(1, shade)), a);
            }
            return p.ToSprite(40, new Vector2(0f, 0f));
        }

        static Sprite MakeCity()
        {
            const int w = 1024, h = 256;
            var p = new Painter(w, h);
            var rnd = new System.Random(21);
            int x0 = 0;
            while (x0 < w)
            {
                int bw = rnd.Next(34, 86);
                if (w - (x0 + bw) < 34) bw = w - x0;   // last building closes the tile exactly
                int bh = rnd.Next(60, 210);
                int left = x0, width = bw;
                p.Fill((x, y) => Sd.Box(x, y, left + width * 0.5f, bh * 0.5f, width * 0.5f - 2, bh * 0.5f), Grey(0.82f));
                if (rnd.NextDouble() < 0.35)
                {
                    int ax = left + width / 2;
                    int ah = rnd.Next(14, 34);
                    p.Fill((x, y) => Sd.Box(x, y, ax, bh + ah * 0.5f, 1.5f, ah * 0.5f), Grey(0.82f));
                    p.Fill((x, y) => Sd.Circle(x, y, ax, bh + ah, 3), W);
                }
                // windows
                for (int wy = 14; wy < bh - 12; wy += 16)
                for (int wx = left + 8; wx < left + width - 10; wx += 12)
                {
                    if (rnd.NextDouble() < 0.45) continue;
                    for (int yy = 0; yy < 7; yy++)
                    for (int xx = 0; xx < 5; xx++)
                        p.Set(wx + xx, wy + yy, W);
                }
                x0 += bw;
            }
            return p.ToSprite(52, new Vector2(0f, 0f));
        }

        static Sprite MakeHills()
        {
            const int w = 1024, h = 192;
            var p = new Painter(w, h);
            var rnd = new System.Random(3);
            float[] hgt = new float[w];
            for (int x = 0; x < w; x++)
                hgt[x] = 70 + 22 * Mathf.Sin(2 * Mathf.PI * 2 * x / w) + 12 * Mathf.Sin(2 * Mathf.PI * 5 * x / w + 1.3f)
                         + 6 * Mathf.Sin(2 * Mathf.PI * 9 * x / w + 0.4f);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float a = Mathf.Clamp01(0.5f - (y - hgt[x]));
                if (a > 0) p.Blend(x, y, W, a);
            }
            // pine trees along the ridge
            for (int i = 0; i < 26; i++)
            {
                int tx = rnd.Next(0, w);
                float baseY = hgt[tx] - 6;
                float th = rnd.Next(40, 80);
                float tw = th * 0.38f;
                var tri = new[] { new Vector2(tx - tw, baseY + th * 0.15f), new Vector2(tx + tw, baseY + th * 0.15f), new Vector2(tx, baseY + th) };
                var tri2 = new[] { new Vector2(tx - tw * 0.8f, baseY + th * 0.45f), new Vector2(tx + tw * 0.8f, baseY + th * 0.45f), new Vector2(tx, baseY + th * 1.15f) };
                for (int y = 0; y < h; y++)
                for (int x = tx - 50; x < tx + 50; x++)
                {
                    float d = Mathf.Min(Sd.Polygon(x + 0.5f, y + 0.5f, tri), Sd.Polygon(x + 0.5f, y + 0.5f, tri2));
                    d = Mathf.Min(d, Sd.Box(x + 0.5f, y + 0.5f, tx, baseY + 4, 3, 14));
                    float a = Mathf.Clamp01(0.5f - d);
                    if (a > 0) p.Blend(x, y, W, a);
                }
            }
            return p.ToSprite(64, new Vector2(0f, 0f));
        }

        static Sprite MakeStars()
        {
            const int w = 1024, h = 512;
            var p = new Painter(w, h);
            var rnd = new System.Random(11);
            for (int i = 0; i < 260; i++)
            {
                int sx = rnd.Next(0, w), sy = rnd.Next(0, h);
                float r = (float)(0.6 + rnd.NextDouble() * 1.6);
                float bright = (float)(0.35 + rnd.NextDouble() * 0.65);
                for (int y = sy - 4; y <= sy + 4; y++)
                for (int x = sx - 4; x <= sx + 4; x++)
                {
                    float a = Mathf.Clamp01(0.5f - Sd.Circle(x + 0.5f, y + 0.5f, sx + 0.5f, sy + 0.5f, r)) * bright;
                    if (a > 0) p.Blend(x, y, W, a);
                }
                if (r > 1.9f) // sparkle
                    for (int k = -7; k <= 7; k++)
                    {
                        float a = bright * (1f - Mathf.Abs(k) / 8f) * 0.6f;
                        p.Blend(sx + k, sy, W, a);
                        p.Blend(sx, sy + k, W, a);
                    }
            }
            return p.ToSprite(48, new Vector2(0f, 0f));
        }

        // ------------------------------------------------------------------ UI icons (white, tinted by UI)

        public static Sprite Icon(string name)
        {
            if (icons.TryGetValue(name, out var s) && s) return s;
            var p = new Painter(128, 128);
            switch (name)
            {
                case "play":
                    var tri = new[] { new Vector2(42, 26), new Vector2(42, 102), new Vector2(104, 64) };
                    p.Fill((x, y) => Sd.Polygon(x, y, tri) - 6, W);
                    break;
                case "pause":
                    p.Fill((x, y) => Sd.Union(Sd.Box(x, y, 44, 64, 12, 38, 6), Sd.Box(x, y, 84, 64, 12, 38, 6)), W);
                    break;
                case "home":
                    var roof = new[] { new Vector2(14, 64), new Vector2(64, 110), new Vector2(114, 64) };
                    p.Fill((x, y) => Sd.Subtract(Sd.Union(Sd.Polygon(x, y, roof) - 4, Sd.Box(x, y, 64, 44, 34, 30, 6)),
                        Sd.Box(x, y, 64, 28, 11, 18, 4)), W);
                    break;
                case "retry":
                    p.Fill((x, y) =>
                    {
                        float ring = Sd.Ring(x, y, 64, 62, 34, 14);
                        float gap = Sd.Polygon(x, y, new[] { new Vector2(64, 62), new Vector2(128, 128), new Vector2(128, 62) });
                        return Sd.Subtract(ring, gap);
                    }, W);
                    var arrow = new[] { new Vector2(80, 112), new Vector2(110, 84), new Vector2(76, 70) };
                    p.Fill((x, y) => Sd.Polygon(x, y, arrow) - 2, W);
                    break;
                case "gear":
                    p.Fill((x, y) =>
                    {
                        float d = Sd.Circle(x, y, 64, 64, 36);
                        for (int i = 0; i < 8; i++)
                        {
                            float rx = x, ry = y;
                            Sd.Rotate(ref rx, ref ry, 64, 64, i * 45f);
                            d = Mathf.Min(d, Sd.Box(rx, ry, 64, 106, 10, 12, 3));
                        }
                        return Sd.Subtract(d, Sd.Circle(x, y, 64, 64, 15));
                    }, W);
                    break;
                case "music":
                    p.Fill((x, y) => Sd.Union(Sd.Union(Sd.Circle(x, y, 44, 34, 16), Sd.Box(x, y, 56, 70, 4, 40)),
                        Sd.Union(Sd.Box(x, y, 82, 98, 26, 7, 2), Sd.Union(Sd.Box(x, y, 104, 62, 4, 40), Sd.Circle(x, y, 92, 26, 16)))), W);
                    break;
                case "sound":
                    var cone = new[] { new Vector2(22, 48), new Vector2(42, 48), new Vector2(68, 24), new Vector2(68, 104), new Vector2(42, 80), new Vector2(22, 80) };
                    p.Fill((x, y) => Sd.Polygon(x, y, cone) - 2, W);
                    p.Fill((x, y) => Sd.Intersect(Sd.Ring(x, y, 70, 64, 22, 8), 78 - x), W);
                    p.Fill((x, y) => Sd.Intersect(Sd.Ring(x, y, 70, 64, 40, 8), 80 - x), W);
                    break;
                case "vibrate":
                    p.Fill((x, y) => Sd.Subtract(Sd.Box(x, y, 64, 64, 22, 40, 8), Sd.Box(x, y, 64, 66, 14, 28, 3)), W);
                    p.Fill((x, y) => Sd.Union(Sd.Segment(x, y, 26, 44, 26, 84, 4), Sd.Segment(x, y, 102, 44, 102, 84, 4)), W);
                    p.Fill((x, y) => Sd.Union(Sd.Segment(x, y, 12, 54, 12, 74, 4), Sd.Segment(x, y, 116, 54, 116, 74, 4)), W);
                    break;
                case "trophy":
                    var cup = new[] { new Vector2(32, 108), new Vector2(96, 108), new Vector2(90, 70), new Vector2(64, 52), new Vector2(38, 70) };
                    p.Fill((x, y) => Sd.Polygon(x, y, cup) - 3, W);
                    p.Fill((x, y) => Sd.Union(Sd.Intersect(Sd.Ring(x, y, 32, 88, 16, 7), 32 - x), Sd.Intersect(Sd.Ring(x, y, 96, 88, 16, 7), x - 96)), W);
                    p.Fill((x, y) => Sd.Union(Sd.Box(x, y, 64, 40, 7, 14), Sd.Box(x, y, 64, 20, 26, 7, 3)), W);
                    break;
                case "gift":
                    p.Fill((x, y) => Sd.Union(Sd.Box(x, y, 64, 46, 38, 30, 4), Sd.Box(x, y, 64, 84, 44, 10, 4)), W);
                    p.Fill((x, y) => Sd.Box(x, y, 64, 56, 7, 44), Grey(0.25f));
                    p.Fill((x, y) => Sd.Union(Sd.Ring(x, y, 48, 104, 12, 7), Sd.Ring(x, y, 80, 104, 12, 7)), W);
                    break;
                case "shop": // t-shirt = skins
                    var shirt = new[]
                    {
                        new Vector2(12, 86), new Vector2(42, 112), new Vector2(52, 112), new Vector2(64, 100), new Vector2(76, 112),
                        new Vector2(86, 112), new Vector2(116, 86), new Vector2(102, 62), new Vector2(90, 70), new Vector2(90, 16),
                        new Vector2(38, 16), new Vector2(38, 70), new Vector2(26, 62),
                    };
                    p.Fill((x, y) => Sd.Polygon(x, y, shirt) - 3, W);
                    p.Fill((x, y) => Sd.Box(x, y, 64, 44, 26, 5, 2), Grey(0.25f));
                    break;
                case "target":
                    p.Fill((x, y) => Sd.Union(Sd.Ring(x, y, 64, 64, 46, 10), Sd.Union(Sd.Ring(x, y, 64, 64, 26, 10), Sd.Circle(x, y, 64, 64, 9))), W);
                    break;
                case "close":
                    p.Fill((x, y) => Sd.Union(Sd.Segment(x, y, 32, 32, 96, 96, 9), Sd.Segment(x, y, 32, 96, 96, 32, 9)), W);
                    break;
                case "check":
                    p.Fill((x, y) => Sd.Union(Sd.Segment(x, y, 26, 66, 52, 38, 10), Sd.Segment(x, y, 52, 38, 102, 92, 10)), W);
                    break;
                case "lock":
                    p.Fill((x, y) => Sd.Box(x, y, 64, 46, 36, 30, 8), W);
                    p.Fill((x, y) => Sd.Intersect(Sd.Ring(x, y, 64, 78, 22, 10), 70 - y), W);
                    p.Fill((x, y) => Sd.Union(Sd.Box(x, y, 42, 74, 5, 6), Sd.Box(x, y, 86, 74, 5, 6)), W);
                    p.Fill((x, y) => Sd.Circle(x, y, 64, 48, 8), Grey(0.2f));
                    break;
                case "video":
                    p.Fill((x, y) => Sd.Box(x, y, 54, 64, 38, 28, 8), W);
                    var lens = new[] { new Vector2(94, 64), new Vector2(122, 88), new Vector2(122, 40) };
                    p.Fill((x, y) => Sd.Polygon(x, y, lens) - 2, W);
                    break;
                case "star":
                    var star = new Vector2[10];
                    for (int i = 0; i < 10; i++)
                    {
                        float ang = Mathf.PI / 2 + i * Mathf.PI / 5;
                        float r = i % 2 == 0 ? 54 : 22;
                        star[i] = new Vector2(64 + Mathf.Cos(ang) * r, 62 + Mathf.Sin(ang) * r);
                    }
                    p.Fill((x, y) => Sd.Polygon(x, y, star) - 3, W);
                    break;
                case "shield":
                    DrawShield(p, 64, 64, 1.9f, W);
                    break;
                case "magnet":
                    DrawMagnet(p, 64, 56, 2.0f, W);
                    break;
                case "flag":
                    p.Fill((x, y) => Sd.Box(x, y, 34, 64, 5, 50), W);
                    p.Fill((x, y) => Sd.Polygon(x, y, new[] { new Vector2(38, 112), new Vector2(104, 92), new Vector2(38, 70) }), W);
                    break;
                default:
                    p.Fill((x, y) => Sd.Circle(x, y, 64, 64, 40), W);
                    break;
            }
            s = p.ToSprite(128, new Vector2(0.5f, 0.5f));
            icons[name] = s;
            return s;
        }
    }
}
