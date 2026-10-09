using UnityEngine;

namespace TheDash
{
    /// <summary>
    /// Infinite multi-layer parallax. Each layer is one tiled sprite that is re-positioned every frame,
    /// so it scrolls forever with no seams, popping or extra objects.
    /// </summary>
    public class Backdrop : MonoBehaviour
    {
        class Layer
        {
            public SpriteRenderer sr;
            public float follow;   // 1 = moves with camera (infinitely far), 0 = fixed to world
            public float y;
            public float tileW;
        }

        SpriteRenderer sky, skyGradient, sun, haze, haze2, abyss;
        Layer stars, mountains, city, hills;
        double scroll;   // horizontal "distance" used for parallax (keeps moving on the menu)

        public void Build()
        {
            sky = Make("Sky", Art.Pixel, -100);
            skyGradient = Make("SkyGradient", Art.SkyGradient, -99);
            stars = MakeLayer("Stars", Art.Stars, -98, 0.985f, 1.5f);
            sun = Make("Sun", Art.Sun, -97);
            mountains = MakeLayer("Mountains", Art.Mountains, -90, 0.9f, -0.6f);
            haze = Make("Haze", Art.Horizon, -85);
            city = MakeLayer("City", Art.City, -80, 0.72f, -0.6f);
            haze2 = Make("Haze2", Art.Horizon, -75);
            hills = MakeLayer("Hills", Art.Hills, -70, 0.45f, -0.7f);
            abyss = Make("Abyss", Art.Pixel, -60);
        }

        SpriteRenderer Make(string name, Sprite s, int order)
        {
            var sr = new GameObject(name).AddComponent<SpriteRenderer>();
            sr.transform.SetParent(transform, false);
            sr.sprite = s;
            sr.sortingOrder = order;
            return sr;
        }

        Layer MakeLayer(string name, Sprite s, int order, float follow, float y)
        {
            var sr = Make(name, s, order);
            sr.drawMode = SpriteDrawMode.Tiled;
            return new Layer { sr = sr, follow = follow, y = y, tileW = s.bounds.size.x };
        }

        float lastCamX;

        public void AddDrift(float amount) => scroll += amount;

        public void Apply(Theme t)
        {
            sky.color = t.skyBottom;
            skyGradient.color = t.skyTop;
            stars.sr.color = t.stars;
            sun.color = t.sun;
            mountains.sr.color = t.far;
            city.sr.color = t.mid;
            hills.sr.color = t.near;
            haze.color = new Color(t.skyBottom.r, t.skyBottom.g, t.skyBottom.b, 0.55f);
            haze2.color = new Color(t.skyBottom.r, t.skyBottom.g, t.skyBottom.b, 0.30f);
            abyss.color = Color.Lerp(t.groundFill, Color.black, 0.45f);
        }

        /// <summary>Call after the camera has moved this frame.</summary>
        public void Follow(Camera cam)
        {
            Vector3 c = cam.transform.position;
            // Accumulate camera movement (ignoring snaps back to the start) so parallax never jumps.
            float delta = c.x - lastCamX;
            if (Mathf.Abs(delta) < 3f) scroll += delta;
            lastCamX = c.x;
            float h = cam.orthographicSize * 2f, w = h * cam.aspect;
            float left = c.x - w * 0.5f;

            sky.transform.position = new Vector3(c.x, c.y, 0);
            Fit(sky, w + 2, h + 2);
            skyGradient.transform.position = new Vector3(c.x, c.y + h * 0.15f, 0);
            Fit(skyGradient, w + 2, h * 0.75f);

            sun.transform.position = new Vector3(c.x + w * 0.16f, 3.3f + (c.y - 3.2f) * 0.8f, 0);
            sun.transform.localScale = Vector3.one * 1.05f;

            Place(stars, left, w, c.y - 0.5f);
            Place(mountains, left, w, mountains.y + (c.y - 3.2f) * 0.6f);
            Place(city, left, w, city.y + (c.y - 3.2f) * 0.35f);
            Place(hills, left, w, hills.y + (c.y - 3.2f) * 0.15f);

            haze.transform.position = new Vector3(c.x, 0.9f, 0);
            Fit(haze, w + 2, 3.2f);
            haze2.transform.position = new Vector3(c.x, 0.2f, 0);
            Fit(haze2, w + 2, 2.4f);

            abyss.transform.position = new Vector3(c.x, -8f, 0);
            Fit(abyss, w + 2, 15.6f);
        }

        static void Fit(SpriteRenderer sr, float width, float height)
        {
            Vector2 b = sr.sprite.bounds.size;
            sr.transform.localScale = new Vector3(width / b.x, height / b.y, 1);
        }

        void Place(Layer l, float left, float viewW, float y)
        {
            float tiles = Mathf.Ceil(viewW / l.tileW) + 2;
            l.sr.size = new Vector2(tiles * l.tileW, l.sr.sprite.bounds.size.y);
            double offset = (scroll * (1.0 - l.follow)) % l.tileW;
            l.sr.transform.position = new Vector3(left - (float)offset - 0.01f, y, 0);
        }
    }

    public class CameraRig : MonoBehaviour
    {
        public Camera cam;
        float shake, y = 3.2f, zoom, zoomPulse;
        const float BaseSize = 6f;
        public const float BaseY = 3.2f;
        public const float PlayerScreenX = 0.27f;   // player sits at 27% of screen width

        public float ViewWidth => cam.orthographicSize * 2f * cam.aspect;
        public float Left => cam.transform.position.x - ViewWidth * 0.5f;
        public float Right => cam.transform.position.x + ViewWidth * 0.5f;

        public void Build()
        {
            cam = gameObject.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.nearClipPlane = -50;
            cam.farClipPlane = 50;
            gameObject.tag = "MainCamera";
            gameObject.AddComponent<AudioListener>();
        }

        public void Shake(float amount) => shake = Mathf.Max(shake, amount);

        /// <summary>Zoom out slightly as speed rises (more look-ahead), plus a short pulse on each boost.</summary>
        public void SetSpeedZoom(float multiplier) => zoom = (multiplier - 1f) * 0.6f;
        public void BoostPulse() => zoomPulse = 0.45f;

        public void Snap(float playerX)
        {
            y = BaseY;
            zoom = zoomPulse = 0f;
            cam.orthographicSize = BaseSize;
            Follow(playerX, 0.5f, 0f);
        }

        public void Follow(float playerX, float playerY, float dt)
        {
            zoomPulse = Mathf.MoveTowards(zoomPulse, 0f, dt * 0.35f);
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, BaseSize + zoom + zoomPulse, dt > 0 ? 1f - Mathf.Exp(-3f * dt) : 1f);
            float targetY = BaseY + Mathf.Max(0f, playerY - 2.2f) * 0.35f;
            y = dt > 0 ? Mathf.Lerp(y, targetY, 1f - Mathf.Exp(-4f * dt)) : targetY;
            float x = playerX + ViewWidth * (0.5f - PlayerScreenX);

            Vector2 off = Vector2.zero;
            if (shake > 0)
            {
                off = Random.insideUnitCircle * shake;
                shake = Mathf.MoveTowards(shake, 0, dt * 2.5f);
            }
            transform.position = new Vector3(x + off.x, y + off.y, -10f);
        }
    }
}
