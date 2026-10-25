using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SL.Core;

namespace SL.UI
{
    /// <summary>Code-built uGUI helpers. Everything is laid out with anchors on a 1280x720 reference canvas.</summary>
    public static class UIKit
    {
        public static Font Font;
        public static readonly Color Navy = new Color(0.035f, 0.07f, 0.16f, 0.92f), Panel = new Color(0.06f, 0.11f, 0.24f, 0.96f), PanelLight = new Color(0.1f, 0.17f, 0.34f, 0.96f);
        public static readonly Color Gold = new Color(0.97f, 0.77f, 0.26f, 1f), GoldDark = new Color(0.66f, 0.46f, 0.1f, 1f), Cyan = new Color(0.35f, 0.72f, 1f, 1f), White = Color.white, Dim = new Color(1f, 1f, 1f, 0.55f), Red = new Color(0.95f, 0.3f, 0.3f), Green = new Color(0.4f, 0.9f, 0.5f);
        static Sprite round, roundSmall;

        public static void Init()
        {
            if (Font == null) Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (round == null) round = MakeRound(40, 14); if (roundSmall == null) roundSmall = MakeRound(24, 7);
        }

        static Sprite MakeRound(int size, int r)
        {
            Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] px = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x, r, size - 1 - r), cy = Mathf.Clamp(y, r, size - 1 - r);
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(r - d + 0.5f));
                }
            t.SetPixels(px); t.Apply(); t.filterMode = FilterMode.Bilinear;
            return Sprite.Create(t, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        }

        public static RectTransform Rect(string name, Transform parent, float ax0, float ay0, float ax1, float ay1, float ox0, float oy0, float ox1, float oy1)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(ax0, ay0); rt.anchorMax = new Vector2(ax1, ay1);
            rt.offsetMin = new Vector2(ox0, oy0); rt.offsetMax = new Vector2(ox1, oy1);
            return rt;
        }

        public static RectTransform Fill(string name, Transform parent) { return Rect(name, parent, 0, 0, 1, 1, 0, 0, 0, 0); }

        /// <summary>Rect from top-left pixel coordinates (x, y from top) in the parent, size w x h.</summary>
        public static RectTransform At(string name, Transform parent, float x, float y, float w, float h)
        {
            return Rect(name, parent, 0, 1, 0, 1, x, -y - h, x + w, -y);
        }
        public static RectTransform AtRight(string name, Transform parent, float xFromRight, float y, float w, float h)
        {
            return Rect(name, parent, 1, 1, 1, 1, -xFromRight - w, -y - h, -xFromRight, -y);
        }
        public static RectTransform AtBottomLeft(string name, Transform parent, float x, float yFromBottom, float w, float h)
        {
            return Rect(name, parent, 0, 0, 0, 0, x, yFromBottom, x + w, yFromBottom + h);
        }
        public static RectTransform AtBottomRight(string name, Transform parent, float xFromRight, float yFromBottom, float w, float h)
        {
            return Rect(name, parent, 1, 0, 1, 0, -xFromRight - w, yFromBottom, -xFromRight, yFromBottom + h);
        }

        public static Image Img(Transform parent, Color c, bool rounded)
        {
            Init();
            Image im = parent.gameObject.GetComponent<Image>();
            if (im == null) im = parent.gameObject.AddComponent<Image>();
            im.color = c;
            if (rounded) { im.sprite = round; im.type = Image.Type.Sliced; }
            return im;
        }

        public static Image Box(Transform parent, string name, RectTransform r, Color c, bool rounded)
        {
            r.name = name; return Img(r, c, rounded);
        }

        public static Text Label(Transform parent, string text, int size, Color col, TextAnchor a)
        {
            Init();
            Text t = parent.gameObject.AddComponent<Text>();
            t.font = Font; t.text = text; t.fontSize = size; t.color = col; t.alignment = a;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false; t.supportRichText = true;
            return t;
        }

        public static Text Txt(Transform parent, string text, int size, Color col, TextAnchor a, float x, float y, float w, float h)
        {
            RectTransform r = At("txt", parent, x, y, w, h);
            return Label(r, text, size, col, a);
        }

        public static Button Btn(Transform parent, string text, int size, Color bg, Color fg, Action cb)
        {
            Init();
            Image im = Img(parent, bg, true);
            Button b = parent.gameObject.AddComponent<Button>();
            b.targetGraphic = im;
            if (cb != null) b.onClick.AddListener(() => cb());
            if (!string.IsNullOrEmpty(text))
            {
                RectTransform tr = Fill("t", parent);
                Text t = Label(tr, text, size, fg, TextAnchor.MiddleCenter);
                t.raycastTarget = false;
            }
            return b;
        }

        public static Button GoldBtn(Transform parent, string text, int size, Action cb) { return Btn(parent, text, size, Gold, new Color(0.15f, 0.08f, 0f), cb); }
        public static Button DarkBtn(Transform parent, string text, int size, Action cb) { return Btn(parent, text, size, PanelLight, White, cb); }

        public static RectTransform Scroll(Transform parent, string name, float x, float y, float w, float h, out RectTransform content)
        {
            RectTransform root = At(name, parent, x, y, w, h);
            RectTransform vp = Fill("viewport", root);
            Img(vp, new Color(0f, 0f, 0f, 0.001f), false);
            vp.gameObject.AddComponent<RectMask2D>();
            content = Rect("content", vp, 0, 1, 1, 1, 0, -10, 0, 0);
            content.pivot = new Vector2(0.5f, 1f);
            ScrollRect sr = root.gameObject.AddComponent<ScrollRect>();
            sr.content = content; sr.viewport = vp; sr.horizontal = false; sr.vertical = true; sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 30f; sr.inertia = true;
            return root;
        }

        public static void SetContentHeight(RectTransform content, float h) { content.sizeDelta = new Vector2(0f, h); }

        public static InputField Input(Transform parent, string initial, string hint, int limit)
        {
            Init();
            Image im = Img(parent, new Color(0.02f, 0.04f, 0.1f, 1f), true);
            InputField f = parent.gameObject.AddComponent<InputField>();
            Text t = Label(Rect("text", parent, 0, 0, 1, 1, 10, 4, -10, -4), "", 22, White, TextAnchor.MiddleLeft);
            Text ph = Label(Rect("hint", parent, 0, 0, 1, 1, 10, 4, -10, -4), hint, 20, Dim, TextAnchor.MiddleLeft);
            f.textComponent = t; f.placeholder = ph; f.targetGraphic = im; f.characterLimit = limit; f.text = initial;
            t.raycastTarget = false;
            return f;
        }

        public static Image Bar(Transform parent, string name, float x, float y, float w, float h, Color bg, Color fill, out Image fillImg)
        {
            RectTransform r = At(name, parent, x, y, w, h);
            Image b = Img(r, bg, true);
            RectTransform f = Rect("fill", r, 0, 0, 1, 1, 2, 2, -2, -2);
            fillImg = Img(f, fill, true); fillImg.type = Image.Type.Filled; fillImg.fillMethod = Image.FillMethod.Horizontal; fillImg.fillAmount = 1f;
            fillImg.type = Image.Type.Filled;
            return b;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }

        public static Color Hex(int c) { return SLMath.Hex(c); }
    }

    /// <summary>Virtual joystick for movement.</summary>
    public sealed class Joystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public RectTransform Knob; public float Radius = 70f; public Vector2 Value;
        RectTransform rt;
        void Awake() { rt = GetComponent<RectTransform>(); }
        public void OnPointerDown(PointerEventData e) { Set(e); }
        public void OnDrag(PointerEventData e) { Set(e); }
        public void OnPointerUp(PointerEventData e) { Value = Vector2.zero; if (Knob != null) Knob.anchoredPosition = Vector2.zero; }
        void Set(PointerEventData e)
        {
            if (rt == null) rt = GetComponent<RectTransform>();
            Vector2 centre = rt.position;
            float scale = Mathf.Max(0.01f, rt.lossyScale.x);
            Vector2 d = (e.position - centre) / scale;
            if (d.magnitude > Radius) d = d.normalized * Radius;
            Value = d / Radius;
            if (Knob != null) Knob.anchoredPosition = d;
        }
    }

    /// <summary>Drag anywhere on the free area to rotate the camera.</summary>
    public sealed class LookPad : MonoBehaviour, IDragHandler
    {
        public void OnDrag(PointerEventData e)
        {
            SL.Net.Controls.CamYaw += e.delta.x * 0.22f;
            SL.Net.Controls.CamPitch = Mathf.Clamp(SL.Net.Controls.CamPitch - e.delta.y * 0.18f, 4f, 60f);
        }
    }

    /// <summary>Button that reports held state (for Block).</summary>
    public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public bool Held; public Action<bool> Changed;
        public void OnPointerDown(PointerEventData e) { Held = true; if (Changed != null) Changed(true); }
        public void OnPointerUp(PointerEventData e) { Held = false; if (Changed != null) Changed(false); }
    }

    /// <summary>Rotates a character preview when dragged.</summary>
    public sealed class SpinPad : MonoBehaviour, IDragHandler
    {
        public Transform Target;
        public void OnDrag(PointerEventData e) { if (Target != null) Target.Rotate(0f, -e.delta.x * 0.5f, 0f); }
    }
}
