using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheDash
{
    /// <summary>Helpers to build a consistent, chunky neon UI from code.</summary>
    public static class UIKit
    {
        static Font font;
        public static Font Font => font ? font : font = LoadFont();

        static Font LoadFont()
        {
            var f = Resources.Load<Font>("Fonts/LilitaOne");
            return f ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>Anchor to a single point of the parent (0..1), with matching pivot.</summary>
        public static RectTransform At(this RectTransform rt, float ax, float ay, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = new Vector2(ax, ay);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        /// <summary>Stretch to fill the parent with insets.</summary>
        public static RectTransform Fill(this RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        public static Image Image(Transform parent, Sprite sprite, Color color, string name = "Image", float cornerScale = 1f)
        {
            var rt = Node(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            if (sprite != null && sprite.border != Vector4.zero)
            {
                img.type = UnityEngine.UI.Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = cornerScale;
            }
            return img;
        }

        public static Image Panel(Transform parent, Color color, float cornerScale = 1f, string name = "Panel")
            => Image(parent, Art.Rounded, color, name, cornerScale);

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter, string name = "Label")
        {
            var rt = Node(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var sh = rt.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0.04f, 0.0f, 0.12f, 0.55f);
            sh.effectDistance = new Vector2(0, -Mathf.Max(2, size / 14f));
            return t;
        }

        public static Outline Glow(this Text t, Color c, float size = 3f)
        {
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = c;
            o.effectDistance = new Vector2(size, -size);
            return o;
        }

        public class ButtonRefs
        {
            public Button button;
            public Image bg, icon;
            public Text label;
            public RectTransform rt;
            public GameObject badge;
        }

        /// <summary>Chunky 3D-looking button: coloured face over a darker "lip".</summary>
        public static ButtonRefs Button(Transform parent, string label, string icon, Color color, float w, float h, Action onClick, int fontSize = 52)
        {
            var refs = new ButtonRefs();
            var rt = Node(string.IsNullOrEmpty(label) ? icon : label, parent);
            rt.sizeDelta = new Vector2(w, h);
            refs.rt = rt;

            float corner = h < 110 ? 1.6f : 1f;
            var lip = Image(rt, Art.Rounded, Color.Lerp(color, Color.black, 0.45f), "Lip", corner);
            lip.rectTransform.Fill(0, 0, 0, 0);

            var face = Image(rt, Art.Rounded, color, "Face", corner);
            face.rectTransform.Fill(0, 9, 0, 0);
            face.raycastTarget = true;
            refs.bg = face;

            var shine = Image(face.transform, Art.Rounded, new Color(1, 1, 1, 0.14f), "Shine", corner * 1.4f);
            shine.rectTransform.anchorMin = new Vector2(0, 0.52f);
            shine.rectTransform.anchorMax = new Vector2(1, 1);
            shine.rectTransform.offsetMin = new Vector2(8, 0);
            shine.rectTransform.offsetMax = new Vector2(-8, -6);

            var content = Node("Content", face.transform).Fill();
            var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 16;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            if (!string.IsNullOrEmpty(icon))
            {
                refs.icon = Image(content, icon == "coin" ? Art.Coin : Art.Icon(icon), Color.white, "Icon");
                var le = refs.icon.gameObject.AddComponent<LayoutElement>();
                float s = string.IsNullOrEmpty(label) ? h * 0.52f : h * 0.46f;
                if (icon == "coin") s *= 1.3f;
                le.preferredWidth = le.preferredHeight = s;
            }
            if (!string.IsNullOrEmpty(label))
                refs.label = Label(content, label, fontSize, Color.white);

            var btn = rt.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = face;
            btn.onClick.AddListener(() =>
            {
                Sfx.I?.Play("click");
                onClick?.Invoke();
            });
            refs.button = btn;
            rt.gameObject.AddComponent<Pressable>().target = face.rectTransform;
            return refs;
        }

        public static GameObject Badge(RectTransform parent)
        {
            var dot = Image(parent, Art.Circle, UIColors.Red, "Badge");
            dot.rectTransform.At(1, 1, 10, 10, 36, 36);
            return dot.gameObject;
        }

        public static (Image bg, Image fill) Bar(Transform parent, Color back, Color front, float h)
        {
            var bg = Image(parent, Art.RoundedSmall, back, "Bar", 1.5f);
            var fill = Image(bg.transform, Art.RoundedSmall, front, "Fill", 1.5f);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0.5f, 1);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
            return (bg, fill);
        }

        public static void SetBar(Image fill, float t)
        {
            t = Mathf.Clamp01(t);
            fill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.04f, t), 1);
            fill.enabled = t > 0.001f;
        }

        /// <summary>A rounded "pill" with an icon and a number - used for coins / best score.</summary>
        public static Text Pill(Transform parent, Sprite icon, Color iconColor, string text, float w, out RectTransform rt)
        {
            var bg = Panel(parent, new Color(0.05f, 0.02f, 0.14f, 0.65f), 1.6f, "Pill");
            rt = bg.rectTransform;
            rt.sizeDelta = new Vector2(w, 92);
            var ic = Image(bg.transform, icon, iconColor, "Icon");
            ic.rectTransform.At(0, 0.5f, 14, 0, 72, 72);
            var t = Label(bg.transform, text, 52, Color.white, TextAnchor.MiddleLeft);
            t.rectTransform.Fill(100, 0, 20, 4);
            return t;
        }

        public static Image Dim(Transform parent)
        {
            var d = Image(parent, Art.Pixel, UIColors.Dim, "Dim");
            d.rectTransform.Fill();
            d.raycastTarget = true; // block taps from reaching the game
            return d;
        }

        // ------------------------------------------------------------------ easing
        public static float OutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            t -= 1f;
            return 1f + c3 * t * t * t + c1 * t * t;
        }

        public static float OutCubic(float t)
        {
            t = 1f - t;
            return 1f - t * t * t;
        }
    }

    /// <summary>Squish-on-press feedback for buttons.</summary>
    public class Pressable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public RectTransform target;
        float scale = 1f, goal = 1f;
        bool pressed;

        public void OnPointerDown(PointerEventData e)
        {
            goal = 0.94f;
            pressed = true;
        }

        public void OnPointerUp(PointerEventData e) => Release();
        public void OnPointerExit(PointerEventData e) => Release();

        void Release()
        {
            goal = 1f;
            pressed = false;
        }

        void Update()
        {
            scale = Mathf.MoveTowards(scale, goal, Time.unscaledDeltaTime * 2.5f);
            target.localScale = new Vector3(scale, scale, 1);
            target.offsetMin = new Vector2(target.offsetMin.x, pressed ? 3 : 9);
        }
    }

    /// <summary>Minimal tween runner (unscaled time) for UI animation.</summary>
    public class Tweener : MonoBehaviour
    {
        class T
        {
            public float time, duration, delay;
            public Action<float> step;
            public Action done;
            public object owner;
        }

        static Tweener inst;
        readonly List<T> list = new List<T>();

        public static void Run(object owner, float duration, Action<float> step, Action done = null, float delay = 0f)
        {
            if (inst == null) inst = new GameObject("Tweener").AddComponent<Tweener>();
            inst.list.RemoveAll(t => owner != null && t.owner == owner);
            var tw = new T { duration = Mathf.Max(0.0001f, duration), step = step, done = done, owner = owner, delay = delay };
            inst.list.Add(tw);
            if (delay <= 0) step(0f);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (i >= list.Count) continue;
                var t = list[i];
                if (t.delay > 0)
                {
                    t.delay -= dt;
                    continue;
                }
                t.time += dt;
                float k = Mathf.Clamp01(t.time / t.duration);
                t.step(k);
                if (k >= 1f)
                {
                    list.Remove(t);
                    t.done?.Invoke();
                }
            }
        }
    }
}
