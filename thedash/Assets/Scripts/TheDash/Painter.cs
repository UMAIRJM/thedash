using UnityEngine;

namespace TheDash
{
    /// <summary>
    /// Tiny anti-aliased software rasteriser built on signed distance functions.
    /// Used to draw every sprite in the game at startup, so the art style is fully consistent.
    /// Coordinates are in pixels, origin bottom-left, y up.
    /// </summary>
    public class Painter
    {
        public delegate float Sdf(float x, float y);

        public readonly int W, H;
        readonly Color[] px;

        public Painter(int w, int h)
        {
            W = w;
            H = h;
            px = new Color[w * h];
        }

        /// <summary>Fill the inside of a shape (d &lt; 0) with anti-aliased edges.</summary>
        public void Fill(Sdf sdf, Color c)
        {
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float d = sdf(x + 0.5f, y + 0.5f);
                float a = Mathf.Clamp01(0.5f - d);
                if (a > 0f) Blend(y * W + x, c, a * c.a);
            }
        }

        /// <summary>Fill with a vertical gradient (bottom colour -> top colour) across the given pixel rows.</summary>
        public void FillGradient(Sdf sdf, Color bottom, Color top, float y0, float y1)
        {
            for (int y = 0; y < H; y++)
            {
                Color c = Color.Lerp(bottom, top, Mathf.InverseLerp(y0, y1, y));
                for (int x = 0; x < W; x++)
                {
                    float d = sdf(x + 0.5f, y + 0.5f);
                    float a = Mathf.Clamp01(0.5f - d);
                    if (a > 0f) Blend(y * W + x, c, a * c.a);
                }
            }
        }

        /// <summary>Soft falloff outside a shape - used for neon glows.</summary>
        public void Glow(Sdf sdf, Color c, float radius)
        {
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float d = sdf(x + 0.5f, y + 0.5f);
                float a = d <= 0 ? 1f : Mathf.Clamp01(1f - d / radius);
                a *= a;
                if (a > 0f) Blend(y * W + x, c, a * c.a);
            }
        }

        public void Set(int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            px[y * W + x] = c;
        }

        public void Blend(int x, int y, Color c, float a)
        {
            x = ((x % W) + W) % W; // wraps horizontally so tiles stay seamless
            if (y < 0 || y >= H) return;
            Blend(y * W + x, c, a);
        }

        void Blend(int i, Color src, float a)
        {
            Color dst = px[i];
            float outA = a + dst.a * (1f - a);
            if (outA <= 0.0001f) return;
            float k = dst.a * (1f - a);
            px[i] = new Color(
                (src.r * a + dst.r * k) / outA,
                (src.g * a + dst.g * k) / outA,
                (src.b * a + dst.b * k) / outA,
                outA);
        }

        public Texture2D ToTexture(bool repeat = false)
        {
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false)
            {
                wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            t.SetPixels(px);
            t.Apply(false, true);
            return t;
        }

        public Sprite ToSprite(float ppu, Vector2 pivot, Vector4 border = default, bool repeat = false)
        {
            var t = ToTexture(repeat);
            return Sprite.Create(t, new Rect(0, 0, W, H), pivot, ppu, 0, SpriteMeshType.FullRect, border);
        }
    }

    /// <summary>Signed distance primitives (negative inside).</summary>
    public static class Sd
    {
        public static float Circle(float x, float y, float cx, float cy, float r)
        {
            float dx = x - cx, dy = y - cy;
            return Mathf.Sqrt(dx * dx + dy * dy) - r;
        }

        public static float Ring(float x, float y, float cx, float cy, float r, float thickness)
        {
            return Mathf.Abs(Circle(x, y, cx, cy, r)) - thickness * 0.5f;
        }

        public static float Box(float x, float y, float cx, float cy, float hw, float hh, float radius = 0)
        {
            float qx = Mathf.Abs(x - cx) - hw + radius;
            float qy = Mathf.Abs(y - cy) - hh + radius;
            float ox = Mathf.Max(qx, 0), oy = Mathf.Max(qy, 0);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0) - radius;
        }

        public static float Segment(float x, float y, float ax, float ay, float bx, float by, float r)
        {
            float pax = x - ax, pay = y - ay, bax = bx - ax, bay = by - ay;
            float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
            float dx = pax - bax * h, dy = pay - bay * h;
            return Mathf.Sqrt(dx * dx + dy * dy) - r;
        }

        public static float Polygon(float x, float y, Vector2[] v)
        {
            float d = (x - v[0].x) * (x - v[0].x) + (y - v[0].y) * (y - v[0].y);
            float s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                float ex = v[j].x - v[i].x, ey = v[j].y - v[i].y;
                float wx = x - v[i].x, wy = y - v[i].y;
                float h = Mathf.Clamp01((wx * ex + wy * ey) / (ex * ex + ey * ey));
                float bx = wx - ex * h, by = wy - ey * h;
                d = Mathf.Min(d, bx * bx + by * by);
                bool c1 = y >= v[i].y, c2 = y < v[j].y, c3 = ex * wy > ey * wx;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }

        /// <summary>Rotate a point around (cx, cy) by -angle (so shapes appear rotated by +angle).</summary>
        public static void Rotate(ref float x, ref float y, float cx, float cy, float angleDeg)
        {
            float a = -angleDeg * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            float dx = x - cx, dy = y - cy;
            x = cx + dx * c - dy * s;
            y = cy + dx * s + dy * c;
        }

        public static float Union(float a, float b) => Mathf.Min(a, b);
        public static float Subtract(float a, float b) => Mathf.Max(a, -b);
        public static float Intersect(float a, float b) => Mathf.Max(a, b);
    }
}
