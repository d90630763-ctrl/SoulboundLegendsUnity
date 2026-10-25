using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SL.Core;
using SL.Gameplay;
using SL.Net;

namespace SL.UI
{
    /// <summary>Canvas, safe area, toasts and the profile cache every screen reads from.</summary>
    public sealed class UIRoot : MonoBehaviour
    {
        public static UIRoot I;
        public Canvas Canvas; public RectTransform Safe, ScreenLayer, PanelLayer, ToastLayer;
        readonly List<Text> toasts = new List<Text>(); readonly List<float> toastBorn = new List<float>();
        static Profile cache;

        public static UIRoot Create()
        {
            if (I != null) return I;
            UIKit.Init();
            GameObject g = new GameObject("SL_UI");
            DontDestroyOnLoad(g);
            I = g.AddComponent<UIRoot>();
            Canvas c = g.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 10;
            CanvasScaler cs = g.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution = new Vector2(1280f, 720f);
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; cs.matchWidthOrHeight = 0.5f;
            g.AddComponent<GraphicRaycaster>();
            I.Canvas = c;
            if (EventSystem.current == null)
            {
                GameObject es = new GameObject("EventSystem"); DontDestroyOnLoad(es);
                es.AddComponent<EventSystem>(); es.AddComponent<StandaloneInputModule>();
            }
            RectTransform full = g.GetComponent<RectTransform>();
            Rect sa = Screen.safeArea; float sw = Mathf.Max(1, Screen.width), sh = Mathf.Max(1, Screen.height);
            I.Safe = UIKit.Rect("Safe", g.transform, sa.x / sw, sa.y / sh, (sa.x + sa.width) / sw, (sa.y + sa.height) / sh, 0, 0, 0, 0);
            if (sa.width < 10f) { I.Safe.anchorMin = Vector2.zero; I.Safe.anchorMax = Vector2.one; }
            I.ScreenLayer = UIKit.Fill("Screens", I.Safe);
            I.PanelLayer = UIKit.Fill("Panels", I.Safe);
            I.ToastLayer = UIKit.Fill("Toasts", I.Safe);
            PlayerNet.ToastEvent += I.Toast;
            PlayerNet.ProfileEvent += I.OnProfile;
            return I;
        }

        // ---------------------------------------------------------------- profile cache
        public static Profile Prof
        {
            get
            {
                if (PlayerNet.Local != null && PlayerNet.Local.Prof != null) return PlayerNet.Local.Prof;
                if (cache == null)
                {
                    string j = SLSave.GetStr("profile_cache", "");
                    if (!string.IsNullOrEmpty(j)) cache = Profile.FromJson(j);
                    if (cache == null || cache.name != ClientIdentity.Name) cache = Profile.Fresh(ClientIdentity.Name, ClientIdentity.Look);
                }
                return cache;
            }
        }

        void OnProfile()
        {
            if (PlayerNet.Local != null && PlayerNet.Local.Prof != null)
            {
                cache = PlayerNet.Local.Prof;
                SLSave.SetStr("profile_cache", cache.ToJson());
            }
        }

        public static void ResetCache() { cache = null; }

        // ---------------------------------------------------------------- toasts
        public void Toast(string msg)
        {
            RectTransform r = UIKit.Rect("toast", ToastLayer, 0.5f, 1f, 0.5f, 1f, -380f, -100f - toasts.Count * 44f, 380f, -60f - toasts.Count * 44f);
            UIKit.Img(r, new Color(0f, 0f, 0f, 0.65f), true);
            Text t = UIKit.Label(UIKit.Rect("t", r, 0, 0, 1, 1, 10, 2, -10, -2), msg, 19, Color.white, TextAnchor.MiddleCenter);
            toasts.Add(t); toastBorn.Add(Time.time);
        }

        void Update()
        {
            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                if (Time.time - toastBorn[i] > 4.2f || toasts.Count > 5 && i == 0)
                {
                    if (toasts[i] != null) Destroy(toasts[i].transform.parent.gameObject);
                    toasts.RemoveAt(i); toastBorn.RemoveAt(i);
                    for (int k = 0; k < toasts.Count; k++) { RectTransform rr = toasts[k].transform.parent as RectTransform; rr.offsetMin = new Vector2(-380f, -100f - k * 44f); rr.offsetMax = new Vector2(380f, -60f - k * 44f); }
                }
            }
        }

        public static string NpcName(string id)
        {
            for (int z = 0; z < 2; z++)
            {
                SL.World.ZoneLayout l = SL.World.ZoneLayout.Get(z);
                for (int i = 0; i < l.Npcs.Count; i++) if (l.Npcs[i].Id == id) return l.Npcs[i].Name;
            }
            return id;
        }
    }
}
