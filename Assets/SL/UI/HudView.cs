using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SL.Core;
using SL.Gameplay;
using SL.Net;

namespace SL.UI
{
    /// <summary>In-world HUD: joystick, combat buttons, 8-slot skill wheel, bars, minimap, quest tracker, status button.</summary>
    public sealed class HudView : MonoBehaviour
    {
        public static HudView I;
        RectTransform root; Joystick joy; Image hpFill, mpFill, xpFill; Text nameT, hpT, mpT, trackerT, promptT, deadT, zoneT;
        GameObject interactBtn, deadPanel; HoldButton atkHold;
        readonly Button[] skillBtn = new Button[8]; readonly Text[] skillTxt = new Text[8]; readonly Image[] skillCd = new Image[8];
        readonly Dictionary<string, float> cdUntil = new Dictionary<string, float>();
        int page; float nextAuto, nextMini, trackerT2;
        Camera miniCam; RenderTexture miniRt;

        public static HudView Create(Transform parent)
        {
            GameObject g = new GameObject("SL_Hud"); DontDestroyOnLoad(g);
            I = g.AddComponent<HudView>();
            I.Build(parent);
            PlayerNet.InteractEvent += I.OnInteract;
            return I;
        }

        public void Destroy()
        {
            PlayerNet.InteractEvent -= OnInteract;
            Controls.HudVisible = false; Controls.Move = Vector2.zero; Controls.Block = false;
            if (root != null) UnityEngine.Object.Destroy(root.gameObject);
            if (miniCam != null) UnityEngine.Object.Destroy(miniCam.gameObject);
            if (miniRt != null) miniRt.Release();
            I = null; Destroy(gameObject);
        }

        void OnInteract(int kind, string id, string extra)
        {
            if (kind == 1) Panels.OpenDialog(id);
            else if (kind == 3) Panels.OpenChestResult(id, extra);
        }

        RectTransform Round(string name, RectTransform r, Color bg, string text, int size, Action cb)
        {
            UIKit.Btn(r, text, size, bg, Color.white, cb); r.name = name; return r;
        }

        void Build(Transform parent)
        {
            Controls.HudVisible = true;
            root = UIKit.Fill("HUD", parent);
            RectTransform look = UIKit.Fill("look", root); UIKit.Img(look, new Color(0f, 0f, 0f, 0.001f), false); look.gameObject.AddComponent<LookPad>();
            // portrait + bars
            RectTransform pf = UIKit.At("pf", root, 8f, 8f, 330f, 112f); UIKit.Img(pf, new Color(0.03f, 0.06f, 0.15f, 0.7f), true); pf.GetComponent<Image>().raycastTarget = false;
            nameT = UIKit.Label(UIKit.At("n", pf, 12f, 4f, 200f, 28f), "", 22, Color.white, TextAnchor.MiddleLeft);
            Image b1; UIKit.Bar(pf, "hp", 12f, 36f, 220f, 20f, new Color(0f, 0f, 0f, 0.6f), new Color(0.85f, 0.2f, 0.2f), out b1); hpFill = b1;
            hpT = UIKit.Label(UIKit.At("ht", pf, 14f, 36f, 216f, 20f), "", 14, Color.white, TextAnchor.MiddleCenter);
            Image b2; UIKit.Bar(pf, "mp", 12f, 60f, 220f, 16f, new Color(0f, 0f, 0f, 0.6f), new Color(0.25f, 0.5f, 0.95f), out b2); mpFill = b2;
            mpT = UIKit.Label(UIKit.At("mt", pf, 14f, 60f, 216f, 16f), "", 12, Color.white, TextAnchor.MiddleCenter);
            Image b3; UIKit.Bar(pf, "xp", 12f, 82f, 220f, 10f, new Color(0f, 0f, 0f, 0.6f), UIKit.Gold, out b3); xpFill = b3;
            UIKit.Btn(UIKit.At("status", pf, 242f, 34f, 78f, 58f), "Status", 17, UIKit.Gold, new Color(0.15f, 0.08f, 0f), () => Panels.OpenById("status"));
            trackerT = UIKit.Label(UIKit.At("trk", root, 10f, 126f, 330f, 120f), "", 16, new Color(1f, 0.95f, 0.8f), TextAnchor.UpperLeft);
            zoneT = UIKit.Label(UIKit.At("zone", root, 350f, 8f, 300f, 24f), "", 16, UIKit.Dim, TextAnchor.MiddleLeft);
            // menu row
            string[] ml = { "Bag", "Quests", "Skills", "Gear", "World", "Menu" }; string[] mi = { "inventory", "quests", "skills", "equipment", "map", "settings" };
            for (int i = 0; i < ml.Length; i++)
            {
                string id = mi[i];
                RectTransform r = UIKit.AtRight("m" + i, root, 8f + i * 70f, 8f, 64f, 44f);
                UIKit.Btn(r, ml[i], 16, new Color(0.04f, 0.08f, 0.2f, 0.85f), Color.white, () => Panels.OpenById(id));
            }
            // minimap
            miniRt = new RenderTexture(256, 256, 16); miniRt.Create();
            GameObject mc = new GameObject("SL_MiniCam"); DontDestroyOnLoad(mc);
            miniCam = mc.AddComponent<Camera>(); miniCam.orthographic = true; miniCam.orthographicSize = 38f; miniCam.targetTexture = miniRt; miniCam.clearFlags = CameraClearFlags.SolidColor; miniCam.backgroundColor = new Color(0.05f, 0.08f, 0.15f); miniCam.farClipPlane = 300f; miniCam.enabled = false;
            RectTransform mm = UIKit.AtRight("mini", root, 8f, 58f, 168f, 168f); UIKit.Img(mm, new Color(0f, 0f, 0f, 0.6f), true);
            RectTransform mi2 = UIKit.Rect("map", mm, 0, 0, 1, 1, 4, 4, -4, -4); RawImage ri = mi2.gameObject.AddComponent<RawImage>(); ri.texture = miniRt; ri.raycastTarget = false;
            RectTransform dot = UIKit.Rect("dot", mm, 0.5f, 0.5f, 0.5f, 0.5f, -5, -5, 5, 5); UIKit.Img(dot, UIKit.Gold, true); dot.GetComponent<Image>().raycastTarget = false;
            // joystick
            RectTransform jr = UIKit.AtBottomLeft("joy", root, 30f, 30f, 190f, 190f); UIKit.Img(jr, new Color(1f, 1f, 1f, 0.12f), true);
            RectTransform knob = UIKit.Rect("knob", jr, 0.5f, 0.5f, 0.5f, 0.5f, -40f, -40f, 40f, 40f); UIKit.Img(knob, new Color(1f, 1f, 1f, 0.45f), true); knob.GetComponent<Image>().raycastTarget = false;
            joy = jr.gameObject.AddComponent<Joystick>(); joy.Knob = knob; joy.Radius = 70f;
            // combat buttons
            RectTransform at = UIKit.AtBottomRight("atk", root, 28f, 28f, 104f, 104f);
            UIKit.Btn(at, "ATTACK", 20, new Color(0.85f, 0.2f, 0.2f, 0.92f), Color.white, null);
            atkHold = at.gameObject.AddComponent<HoldButton>(); atkHold.Changed = h => { if (h) { nextAuto = 0f; } };
            Round("heavy", UIKit.AtBottomRight("heavy", root, 150f, 18f, 66f, 66f), new Color(0.7f, 0.3f, 0.1f, 0.92f), "Heavy", 16, () => Act(1));
            Round("dodge", UIKit.AtBottomRight("dodge", root, 18f, 150f, 66f, 66f), new Color(0.2f, 0.5f, 0.8f, 0.92f), "Dodge", 16, () => Act(2));
            Round("jump", UIKit.AtBottomRight("jump", root, 100f, 150f, 56f, 56f), new Color(0.3f, 0.6f, 0.4f, 0.92f), "Jump", 14, () => Act(3));
            RectTransform bl = UIKit.AtBottomRight("block", root, 150f, 94f, 56f, 56f);
            UIKit.Btn(bl, "Block", 14, new Color(0.4f, 0.4f, 0.6f, 0.92f), Color.white, null);
            HoldButton hb = bl.gameObject.AddComponent<HoldButton>(); hb.Changed = h => Controls.Block = h;
            RectTransform ib = UIKit.AtBottomRight("interact", root, 430f, 36f, 96f, 70f);
            UIKit.Btn(ib, "Interact", 20, UIKit.Gold, new Color(0.15f, 0.08f, 0f), () => { if (PlayerNet.Local != null) PlayerNet.Local.InteractServerRpc(); });
            interactBtn = ib.gameObject; interactBtn.SetActive(false);
            promptT = UIKit.Label(UIKit.Rect("prompt", root, 0.5f, 0f, 0.5f, 0f, -240f, 240f, 240f, 280f), "", 22, UIKit.Gold, TextAnchor.MiddleCenter);
            // skill wheel (8 slots) + page swap
            for (int i = 0; i < 8; i++)
            {
                float th = (95f + i * (80f / 7f)) * Mathf.Deg2Rad; float rad = 232f;
                float xr = 80f - rad * Mathf.Cos(th) - 29f, yb = 80f + rad * Mathf.Sin(th) - 29f;
                int slot = i;
                RectTransform sr = UIKit.AtBottomRight("sk" + i, root, xr, yb, 58f, 58f);
                skillBtn[i] = UIKit.Btn(sr, "", 13, new Color(0.08f, 0.14f, 0.32f, 0.92f), Color.white, () => Cast(slot));
                skillTxt[i] = UIKit.Label(UIKit.Rect("n", sr, 0, 0, 1, 1, 2, 2, -2, -2), "", 12, Color.white, TextAnchor.MiddleCenter);
                RectTransform cd = UIKit.Rect("cd", sr, 0, 0, 1, 1, 0, 0, 0, 0); skillCd[i] = UIKit.Img(cd, new Color(0f, 0f, 0f, 0.6f), true); skillCd[i].type = Image.Type.Filled; skillCd[i].fillMethod = Image.FillMethod.Vertical; skillCd[i].fillAmount = 0f; skillCd[i].raycastTarget = false;
            }
            UIKit.Btn(UIKit.AtBottomRight("page", root, 36f, 250f, 50f, 40f), "1/2", 14, new Color(0.04f, 0.08f, 0.2f, 0.85f), Color.white, () => { page = 1 - page; RefreshSkills(); });
            // death overlay
            RectTransform dp = UIKit.Fill("dead", root); UIKit.Img(dp, new Color(0.4f, 0f, 0f, 0.5f), false); dp.GetComponent<Image>().raycastTarget = false;
            deadT = UIKit.Label(UIKit.Rect("t", dp, 0, 0.4f, 1, 0.6f, 0, 0, 0, 0), "<b>YOU WERE DEFEATED</b>\nRespawning...", 40, Color.white, TextAnchor.MiddleCenter);
            deadPanel = dp.gameObject; deadPanel.SetActive(false);
            PlayerNet.ProfileEvent += RefreshSkills;
        }

        void OnDestroy() { PlayerNet.ProfileEvent -= RefreshSkills; }

        void Act(int a) { PlayerNet me = PlayerNet.Local; if (me == null || me.NDead.Value) return; me.ActionServerRpc(a, ClientWorld.I != null ? ClientWorld.I.AimYaw() : 0f); }

        void Cast(int slot)
        {
            PlayerNet me = PlayerNet.Local; if (me == null || me.Prof == null || me.NDead.Value) return;
            int idx = page * 8 + slot; if (idx >= me.Prof.loadout.Count) { UIRoot.I.Toast("Empty slot. Equip skills in the Skills menu."); return; }
            string id = me.Prof.loadout[idx]; SkillDef d = Skills.Get(id); if (d == null) return;
            float until; if (cdUntil.TryGetValue(id, out until) && Time.time < until) return;
            if (me.NMp.Value < d.Mp) { UIRoot.I.Toast("Not enough mana."); return; }
            cdUntil[id] = Time.time + d.Cd;
            me.CastServerRpc(id, ClientWorld.I != null ? ClientWorld.I.AimYaw() : 0f);
        }

        void RefreshSkills()
        {
            PlayerNet me = PlayerNet.Local; Profile p = me != null ? me.Prof : null;
            for (int i = 0; i < 8; i++)
            {
                int idx = page * 8 + i; SkillDef d = p != null && idx < p.loadout.Count ? Skills.Get(p.loadout[idx]) : null;
                skillTxt[i].text = d == null ? "" : d.Name.Replace(" ", "\n") + "\n<size=10>" + d.Mp + "mp</size>";
                skillBtn[i].targetGraphic.color = d == null ? new Color(0.05f, 0.08f, 0.16f, 0.55f) : (d.Element >= 0 ? Color.Lerp(new Color(0.08f, 0.14f, 0.32f, 0.95f), ElementInfo.Col(d.Element), 0.45f) : new Color(0.15f, 0.2f, 0.36f, 0.95f));
            }
        }

        void Update()
        {
            PlayerNet me = PlayerNet.Local; if (me == null || root == null) return;
            Controls.Move = joy != null ? joy.Value : Vector2.zero;
            Controls.Sprint = Controls.Move.magnitude > 0.92f;
            Profile p = me.Prof;
            int lvl = me.NLevel.Value;
            nameT.text = "<b>" + me.DisplayName + "</b>   Lv." + lvl;
            hpFill.fillAmount = me.NMaxHp.Value > 0 ? me.NHp.Value / (float)me.NMaxHp.Value : 0f; hpT.text = me.NHp.Value + " / " + me.NMaxHp.Value + (me.NShield.Value > 0 ? "  (+" + me.NShield.Value + ")" : "");
            mpFill.fillAmount = me.NMaxMp.Value > 0 ? me.NMp.Value / (float)me.NMaxMp.Value : 0f; mpT.text = me.NMp.Value + " / " + me.NMaxMp.Value;
            if (p != null) xpFill.fillAmount = lvl >= Rules.MaxLevel ? 1f : Mathf.Clamp01(p.xp / (float)Rules.XpToNext(p.level));
            string pr = Controls.Prompt; promptT.text = string.IsNullOrEmpty(pr) ? "" : pr; interactBtn.SetActive(!string.IsNullOrEmpty(pr));
            deadPanel.SetActive(me.NDead.Value);
            if (ClientWorld.I != null && ClientWorld.I.View != null) zoneT.text = ClientWorld.I.View.Layout.Def.Name + "  (instance " + ClientWorld.I.InstId + ")";
            if (atkHold != null && atkHold.Held && Time.time >= nextAuto) { nextAuto = Time.time + 0.38f; Act(0); }
            for (int i = 0; i < 8; i++)
            {
                int idx = page * 8 + i; float f = 0f;
                if (p != null && idx < p.loadout.Count) { string id = p.loadout[idx]; float u; SkillDef d = Skills.Get(id); if (d != null && cdUntil.TryGetValue(id, out u) && u > Time.time) f = Mathf.Clamp01((u - Time.time) / Mathf.Max(0.1f, d.Cd)); }
                skillCd[i].fillAmount = f;
            }
            trackerT2 -= Time.deltaTime;
            if (trackerT2 <= 0f && p != null) { trackerT2 = 0.5f; trackerT.text = TrackerText(p); }
            nextMini -= Time.deltaTime;
            if (nextMini <= 0f && miniCam != null) { nextMini = 0.3f; miniCam.transform.position = me.transform.position + new Vector3(0f, 90f, 0f); miniCam.transform.rotation = Quaternion.Euler(90f, Controls.CamYaw, 0f); miniCam.Render(); }
        }

        static string TrackerText(Profile p)
        {
            string s = ""; int n = 0;
            for (int i = 0; i < Quests.All.Count && n < 2; i++)
            {
                QuestDef q = Quests.All[i]; QuestProg qp = p.Q(q.Id);
                if (qp == null || !qp.accepted || qp.done) continue; n++;
                s += "<b>" + q.Title + "</b>\n";
                for (int k = 0; k < q.Objs.Count; k++) s += "  " + q.Objs[k].Text + " " + Rules.ObjProgress(p, q, k) + "/" + q.Objs[k].Count + "\n";
            }
            return s;
        }
    }
}
