using UnityEngine;

namespace TheDash
{
    /// <summary>
    /// Kinematic auto-runner. No physics engine: movement is integrated exactly each frame, which gives
    /// perfectly smooth, frame-rate independent motion and a jump that always feels the same.
    /// </summary>
    public class Player : MonoBehaviour
    {
        public const float Half = 0.42f;          // collision half-size (sprite is drawn slightly larger)
        public const float Gravity = 50f;
        public const float JumpHeight = 2.55f;
        public static readonly float JumpVelocity = Mathf.Sqrt(2f * Gravity * JumpHeight);
        public static float AirTime => 2f * JumpVelocity / Gravity;

        const float CoyoteTime = 0.08f, BufferTime = 0.13f;

        public Vector2 pos;
        public float vy;
        public bool grounded;
        public bool alive = true;
        public bool hasShield;
        public float invulnerable;     // seconds of post-revive protection
        public int jumps;

        SpriteRenderer body, shieldBubble;
        Transform visual;
        float angle, coyote, buffer, squash = 1f, idleTime, trailTimer;
        bool wasHeld;

        public void Build()
        {
            visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);
            body = visual.gameObject.AddComponent<SpriteRenderer>();
            body.sortingOrder = 20;

            var glow = new GameObject("Glow").AddComponent<SpriteRenderer>();
            glow.transform.SetParent(transform, false);
            glow.sprite = Art.SoftDot;
            glow.transform.localScale = Vector3.one * 2.4f;
            glow.color = new Color(1, 1, 1, 0.18f);
            glow.sortingOrder = 19;

            shieldBubble = new GameObject("Shield").AddComponent<SpriteRenderer>();
            shieldBubble.transform.SetParent(transform, false);
            shieldBubble.sprite = Art.Circle;
            shieldBubble.sortingOrder = 21;
            shieldBubble.enabled = false;
            ApplySkin();
        }

        public void ApplySkin()
        {
            body.sprite = Art.Player(SaveData.SelectedSkin);
            var glow = transform.Find("Glow").GetComponent<SpriteRenderer>();
            var c = Skin.All[SaveData.SelectedSkin].body;
            glow.color = new Color(c.r, c.g, c.b, 0.22f);
        }

        public Color SkinColor => Skin.All[SaveData.SelectedSkin].body;
        public Color SkinAccent => Skin.All[SaveData.SelectedSkin].accent;

        public void ResetAt(Vector2 p)
        {
            pos = p;
            vy = 0;
            grounded = true;
            alive = true;
            angle = 0;
            hasShield = false;
            invulnerable = 0;
            jumps = 0;
            buffer = coyote = 0;
            squash = 1f;
            wasHeld = false;
            gameObject.SetActive(true);
            SyncVisual(0);
        }

        /// <summary>Idle bounce used on the main menu.</summary>
        public void Idle(float dt)
        {
            idleTime += dt;
            visual.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(idleTime * 3f)) * 0.35f, 0);
            float s = 1f + Mathf.Sin(idleTime * 6f) * 0.04f;
            visual.localScale = new Vector3(0.95f * s, 0.95f / s, 1);
            visual.localRotation = Quaternion.identity;
            transform.position = new Vector3(pos.x, pos.y, 0);
            shieldBubble.enabled = false;
        }

        /// <summary>Advance one frame. Returns false if the player died this frame.</summary>
        public void Step(float dt, float speed, bool pressed, bool held, World world, Fx fx)
        {
            if (!alive) return;

            if (pressed) buffer = BufferTime;
            else buffer -= dt;
            coyote = grounded ? CoyoteTime : coyote - dt;
            if (invulnerable > 0) invulnerable -= dt;

            // Holding the screen keeps jumping each time you land (like Geometry Dash).
            bool wantJump = buffer > 0 || (held && wasHeld && grounded);
            wasHeld = held;
            if (wantJump && coyote > 0)
            {
                vy = JumpVelocity;
                grounded = false;
                coyote = 0;
                buffer = 0;
                jumps++;
                squash = 1.25f;
                Sfx.I.Play("jump", Random.Range(0.95f, 1.05f));
                fx.Burst(new Vector2(pos.x - 0.2f, pos.y - Half), new Color(1, 1, 1, 0.6f), 6, 2.5f, 0.12f, 0.35f, 4f);
            }

            // Sub-step so that fast movement can never tunnel through a 1-unit obstacle.
            float dx = speed * dt;
            int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(dx, Mathf.Abs(vy * dt)) / 0.2f));
            float h = dt / steps;
            for (int i = 0; i < steps && alive; i++)
                SubStep(h, speed, world, fx);

            if (pos.y < -4f) world.Kill(this, false);

            SyncVisual(dt);
            if (grounded && alive)
            {
                trailTimer -= dt;
                if (trailTimer <= 0)
                {
                    trailTimer = 0.05f;
                    fx.Burst(new Vector2(pos.x - Half, pos.y - Half + 0.05f), new Color(1, 1, 1, 0.35f), 1, 1.2f, 0.1f, 0.3f, 0f, -speed * 0.2f);
                }
            }
            else if (alive)
            {
                trailTimer -= dt;
                if (trailTimer <= 0)
                {
                    trailTimer = 0.03f;
                    fx.Afterimage(transform.position, visual.rotation, body.sprite, new Color(SkinColor.r, SkinColor.g, SkinColor.b, 0.35f));
                }
            }
        }

        void SubStep(float h, float speed, World world, Fx fx)
        {
            // ---- horizontal: running into the side of a block is a crash
            pos.x += speed * h;
            if (world.SideHit(pos, Half))
            {
                world.Kill(this, true);
                if (!alive) return;
            }

            // ---- vertical
            float oldBottom = pos.y - Half;
            pos.y += vy * h - 0.5f * Gravity * h * h;
            vy -= Gravity * h;
            bool wasGrounded = grounded;
            grounded = false;

            if (vy <= 0 && world.TryLand(pos.x, Half, oldBottom, pos.y - Half, out float top))
            {
                pos.y = top + Half;
                if (!wasGrounded && vy < -6f)
                {
                    squash = 0.72f;
                    fx.Burst(new Vector2(pos.x, top), new Color(1, 1, 1, 0.5f), 8, 3f, 0.12f, 0.35f, 6f);
                }
                vy = 0;
                grounded = true;
            }
            else if (vy > 0 && world.TryHead(pos.x, Half, pos.y + Half, out float bottom))
            {
                pos.y = bottom - Half;
                vy = 0;
            }

            if (world.HazardHit(pos, Half))
                world.Kill(this, true);

            world.Collect(this);
        }

        void SyncVisual(float dt)
        {
            transform.position = new Vector3(pos.x, pos.y, 0);
            if (grounded)
            {
                // Roll on to land upright (face forward): finish the spin, or undo a small tilt.
                float spun = Mathf.Repeat(-angle, 360f);
                float target = spun < 100f ? angle + spun : angle - (360f - spun);
                angle = Mathf.MoveTowards(angle, target, 1000f * dt);
                if (Mathf.Abs(angle - target) < 0.01f) angle = 0f;
            }
            else
            {
                angle -= 360f / AirTime * dt;   // one full flip per jump
            }
            squash = Mathf.MoveTowards(squash, 1f, 3.5f * dt);
            visual.localRotation = Quaternion.Euler(0, 0, angle);
            visual.localScale = new Vector3(0.95f / squash, 0.95f * squash, 1);
            visual.localPosition = Vector3.zero;

            shieldBubble.enabled = hasShield || invulnerable > 0;
            if (shieldBubble.enabled)
            {
                float pulse = 1.55f + Mathf.Sin(Time.time * 8f) * 0.06f;
                shieldBubble.transform.localScale = Vector3.one * pulse;
                shieldBubble.color = hasShield ? new Color(0.3f, 0.9f, 1f, 0.28f) : new Color(1f, 1f, 1f, 0.18f);
            }
            body.enabled = invulnerable <= 0 || Mathf.Repeat(invulnerable, 0.2f) > 0.08f;
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
