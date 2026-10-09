using System.Collections.Generic;
using UnityEngine;

namespace TheDash
{
    /// <summary>Lightweight pooled sprite particles (dust, sparks, afterimages, floating text pops).</summary>
    public class Fx : MonoBehaviour
    {
        class P
        {
            public SpriteRenderer sr;
            public Vector2 v;
            public float life, maxLife, size, gravity, spin, stretch = 1f;
            public Color color;
            public bool shrink, fixedSize;
        }

        readonly List<P> live = new List<P>();
        readonly Stack<P> pool = new Stack<P>();

        P Get(Sprite sprite, int order)
        {
            P p = pool.Count > 0 ? pool.Pop() : null;
            if (p == null)
            {
                var go = new GameObject("fx");
                go.transform.SetParent(transform, false);
                p = new P { sr = go.AddComponent<SpriteRenderer>() };
            }
            p.sr.gameObject.SetActive(true);
            p.sr.sprite = sprite;
            p.sr.sortingOrder = order;
            p.sr.transform.rotation = Quaternion.identity;
            p.stretch = 1f;
            p.fixedSize = false;
            live.Add(p);
            return p;
        }

        public void Burst(Vector2 at, Color color, int count, float speed, float size, float life, float gravity, float vxBias = 0f)
        {
            for (int i = 0; i < count; i++)
            {
                var p = Get(Art.Pixel, 15);
                float a = Random.Range(0f, Mathf.PI * 2f);
                float s = Random.Range(0.3f, 1f) * speed;
                p.v = new Vector2(Mathf.Cos(a) * s + vxBias, Mathf.Abs(Mathf.Sin(a)) * s);
                p.life = p.maxLife = life * Random.Range(0.7f, 1.2f);
                p.size = size * Random.Range(0.7f, 1.3f);
                p.gravity = gravity;
                p.color = color;
                p.spin = Random.Range(-360f, 360f);
                p.shrink = true;
                p.sr.transform.position = new Vector3(at.x, at.y, 0);
            }
        }

        /// <summary>Explosion of coloured squares, flies in all directions.</summary>
        public void Explode(Vector2 at, Color a, Color b, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var p = Get(Art.Pixel, 25);
                float ang = Random.Range(0f, Mathf.PI * 2f);
                float s = Random.Range(3f, 11f);
                p.v = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * s;
                p.life = p.maxLife = Random.Range(0.5f, 1.1f);
                p.size = Random.Range(0.12f, 0.32f);
                p.gravity = 14f;
                p.color = i % 2 == 0 ? a : b;
                p.spin = Random.Range(-720f, 720f);
                p.shrink = true;
                p.sr.transform.position = new Vector3(at.x, at.y, 0);
            }
            Ring(at, a, 3.2f);
        }

        public void Ring(Vector2 at, Color c, float size)
        {
            var p = Get(Art.SoftDot, 24);
            p.v = Vector2.zero;
            p.life = p.maxLife = 0.35f;
            p.size = size;
            p.gravity = 0;
            p.spin = 0;
            p.color = c;
            p.shrink = false;
            p.sr.transform.position = new Vector3(at.x, at.y, 0);
        }

        public void Afterimage(Vector3 at, Quaternion rot, Sprite sprite, Color c)
        {
            var p = Get(sprite, 18);
            p.v = Vector2.zero;
            p.life = p.maxLife = 0.18f;
            p.size = 0.9f;
            p.gravity = 0;
            p.spin = 0;
            p.color = c;
            p.shrink = true;
            p.sr.transform.position = at;
            p.sr.transform.rotation = rot;
        }

        /// <summary>A thin horizontal speed line flying past the camera.</summary>
        public void Streak(Vector2 at, float length, float vx, float alpha)
        {
            var p = Get(Art.Pixel, 30);
            p.v = new Vector2(vx, 0);
            p.life = p.maxLife = Random.Range(0.25f, 0.45f);
            p.size = Random.Range(0.035f, 0.06f);
            p.stretch = length / p.size;
            p.gravity = 0;
            p.spin = 0;
            p.color = new Color(1, 1, 1, alpha);
            p.shrink = false;
            p.fixedSize = true;
            p.sr.transform.position = new Vector3(at.x, at.y, 0);
        }

        public void Clear()
        {
            foreach (var p in live)
            {
                p.sr.gameObject.SetActive(false);
                pool.Push(p);
            }
            live.Clear();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = live.Count - 1; i >= 0; i--)
            {
                var p = live[i];
                p.life -= dt;
                if (p.life <= 0)
                {
                    p.sr.gameObject.SetActive(false);
                    pool.Push(p);
                    live.RemoveAt(i);
                    continue;
                }
                float t = p.life / p.maxLife;
                p.v.y -= p.gravity * dt;
                var tr = p.sr.transform;
                tr.position += (Vector3)(p.v * dt);
                if (p.spin != 0) tr.Rotate(0, 0, p.spin * dt);
                float s = p.fixedSize ? p.size : p.shrink ? p.size * t : p.size * (1.6f - t * 0.6f);
                tr.localScale = new Vector3(s * p.stretch, s, 1);
                var c = p.color;
                c.a *= p.shrink ? Mathf.Min(1f, t * 1.5f) : t;
                p.sr.color = c;
            }
        }
    }
}
