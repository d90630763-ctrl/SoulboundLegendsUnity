using System;
using UnityEngine;
using UnityEngine.UI;
using SL.Core;
using SL.Gameplay;
using SL.Net;

namespace SL.UI
{
    /// <summary>Main lobby, laid out like the reference: left menu, top bar, right online list, START at bottom right.</summary>
    public sealed class LobbyView
    {
        public RectTransform Root; public Action OnStart; Text goldT, lvT, aeT, tokT; Image xpFill; RectTransform right; float refresh;

        public static LobbyView Build(Transform parent, Action onStart)
        {
            LobbyView v = new LobbyView { OnStart = onStart };
            v.Root = UIKit.Fill("Lobby", parent);
            v.Create();
            return v;
        }

        Button Menu(Transform p, string label, string id, float x, float y, bool locked)
        {
            RectTransform r = UIKit.At("m_" + id, p, x, y, 236f, 50f);
            Button b = UIKit.Btn(r, "", 22, locked ? new Color(0.06f, 0.1f, 0.2f, 0.8f) : new Color(0.06f, 0.12f, 0.28f, 0.9f), Color.white, () =>
            {
                if (locked) UIRoot.I.Toast(label + " unlocks at Level 10.");
                else Panels.OpenById(id);
            });
            UIKit.Label(UIKit.Rect("l", r, 0, 0, 1, 1, 56, 0, -8, 0), "<b>" + label + "</b>" + (locked ? "  (Lv.10)" : ""), 21, locked ? new Color(1f, 1f, 1f, 0.4f) : (id == "luck" || id == "store" ? UIKit.Gold : Color.white), TextAnchor.MiddleLeft);
            RectTransform ic = UIKit.Rect("ic", r, 0, 0.5f, 0, 0.5f, 10, -16, 42, 16); UIKit.Img(ic, locked ? new Color(0.4f, 0.4f, 0.5f) : UIKit.Gold, true);
            UIKit.Label(UIKit.Rect("i", ic, 0, 0, 1, 1, 0, 0, 0, 0), label.Substring(0, 1), 18, new Color(0.1f, 0.06f, 0f), TextAnchor.MiddleCenter);
            return b;
        }

        void Create()
        {
            Profile p = UIRoot.Prof; bool unlocked = Rules.ShopUnlocked(p);
            // top bar
            RectTransform top = UIKit.Rect("top", Root, 0, 1, 1, 1, 0, -66, 0, 0); UIKit.Img(top, new Color(0.03f, 0.06f, 0.15f, 0.85f), false);
            RectTransform av = UIKit.At("avatar", top, 10f, 6f, 54f, 54f); UIKit.Img(av, UIKit.Gold, true);
            UIKit.Label(UIKit.Rect("i", av, 0, 0, 1, 1, 0, 0, 0, 0), "<b>" + (string.IsNullOrEmpty(p.name) ? "?" : p.name.Substring(0, 1).ToUpper()) + "</b>", 30, new Color(0.1f, 0.06f, 0f), TextAnchor.MiddleCenter);
            UIKit.Label(UIKit.At("nm", top, 72f, 4f, 240f, 30f), "<b>" + p.name + "</b>", 24, Color.white, TextAnchor.MiddleLeft);
            lvT = UIKit.Label(UIKit.At("lv", top, 72f, 34f, 120f, 24f), "Lv." + p.level, 19, UIKit.Gold, TextAnchor.MiddleLeft);
            Image xpBack = UIKit.Bar(top, "xp", 130f, 42f, 150f, 10f, new Color(0f, 0f, 0f, 0.6f), UIKit.Gold, out xpFill);
            goldT = UIKit.Label(UIKit.At("g", top, 330f, 14f, 160f, 38f), "Gold  " + p.gold, 22, UIKit.Gold, TextAnchor.MiddleLeft);
            aeT = UIKit.Label(UIKit.At("a", top, 500f, 14f, 170f, 38f), "Aether  " + p.aether, 22, UIKit.Cyan, TextAnchor.MiddleLeft);
            RectTransform plus = UIKit.At("plus", top, 650f, 16f, 36f, 34f); UIKit.Btn(plus, "+", 24, UIKit.PanelLight, Color.white, () => { if (Rules.ShopUnlocked(UIRoot.Prof)) Panels.OpenById("topup"); else UIRoot.I.Toast("Top-Up unlocks at Level 10."); });
            tokT = UIKit.Label(UIKit.At("t", top, 700f, 14f, 170f, 38f), "Tokens  " + p.Count("kingdom_token"), 22, new Color(0.8f, 0.6f, 1f), TextAnchor.MiddleLeft);
            UIKit.Btn(UIKit.AtRight("set", top, 10f, 10f, 100f, 46f), "Settings", 17, UIKit.PanelLight, Color.white, () => Panels.OpenById("settings"));
            UIKit.Btn(UIKit.AtRight("mail", top, 118f, 10f, 80f, 46f), "Mail", 17, UIKit.PanelLight, Color.white, () => Panels.OpenById("mail"));
            UIKit.Btn(UIKit.AtRight("fr", top, 206f, 10f, 100f, 46f), "Friends", 17, UIKit.PanelLight, Color.white, () => Panels.OpenById("friends"));
            // event banner
            RectTransform ban = UIKit.At("banner", Root, 10f, 76f, 236f, 86f); UIKit.Img(ban, new Color(0.18f, 0.12f, 0.4f, 0.92f), true);
            UIKit.Label(UIKit.Rect("t", ban, 0, 0, 1, 1, 10, 4, -10, -4), "<b>WELCOME EVENT</b>\nFinish the starter quests in Dawnrest Village!", 17, Color.white, TextAnchor.MiddleLeft);
            // left menu
            string[] labels = { "Store", "Luck Royale", "Character", "Inventory", "Skills", "Equipment", "Quests", "Events" };
            string[] ids = { "store", "luck", "status", "inventory", "skills", "equipment", "quests", "events" };
            for (int i = 0; i < labels.Length; i++) Menu(Root, labels[i], ids[i], 10f, 172f + i * 54f, (i < 2) && !unlocked);
            // bottom row
            string[] bl = { "Vault", "Pet", "Collection", "Rank", "Emotes" }; string[] bi = { "vault", "pet", "collection", "rank", "emotes" };
            for (int i = 0; i < bl.Length; i++)
            {
                string id = bi[i]; bool lk = id == "rank" && !unlocked; string lab = bl[i];
                RectTransform r = UIKit.AtBottomLeft("b" + i, Root, 10f + i * 122f, 12f, 116f, 46f);
                UIKit.Btn(r, lab, 18, new Color(0.06f, 0.12f, 0.28f, 0.9f), lk ? new Color(1f, 1f, 1f, 0.4f) : Color.white, () => { if (lk) UIRoot.I.Toast("Rank unlocks at Level 10."); else Panels.OpenById(id); });
            }
            RectTransform chat = UIKit.AtBottomLeft("chat", Root, 10f, 66f, 420f, 38f); UIKit.Img(chat, new Color(0f, 0f, 0f, 0.5f), true);
            UIKit.Label(UIKit.Rect("t", chat, 0, 0, 1, 1, 10, 0, -10, 0), "<color=#ffd24a>System</color>:  Welcome to Soulbound Legends, " + p.name + "!", 17, Color.white, TextAnchor.MiddleLeft);
            // right: online + inheritance + start
            right = UIKit.AtRight("online", Root, 10f, 76f, 290f, 250f); UIKit.Img(right, new Color(0.04f, 0.08f, 0.2f, 0.88f), true);
            BuildOnline();
            RectTransform inh = UIKit.AtRight("inh", Root, 10f, 336f, 290f, 84f); UIKit.Img(inh, new Color(0.04f, 0.1f, 0.26f, 0.92f), true);
            UIKit.Label(UIKit.At("a", inh, 14f, 6f, 260f, 22f), "YOUR INHERITANCE", 15, UIKit.Dim, TextAnchor.MiddleLeft);
            UIKit.Label(UIKit.At("b", inh, 14f, 28f, 262f, 32f), "<b>" + (p.element < 0 ? "Not awakened" : ElementInfo.God(p.element)) + "</b>", 26, p.element < 0 ? Color.white : ElementInfo.Col(p.element), TextAnchor.MiddleLeft);
            UIKit.Label(UIKit.At("c", inh, 14f, 58f, 262f, 22f), p.element < 0 ? "Awaken it at Level 3 with Elder Maren" : "(Chosen)", 15, UIKit.Dim, TextAnchor.MiddleLeft);
            RectTransform mode = UIKit.AtBottomRight("mode", Root, 10f, 94f, 290f, 100f); UIKit.Img(mode, new Color(0.05f, 0.14f, 0.12f, 0.92f), true);
            UIKit.Label(UIKit.Rect("t", mode, 0, 0, 1, 1, 14, 6, -14, -6), "<b>ADVENTURE WORLD</b>\nDawnrest Village\nStart here. Rank up to Level 10.", 19, Color.white, TextAnchor.MiddleLeft);
            RectTransform st = UIKit.AtBottomRight("start", Root, 10f, 12f, 290f, 74f);
            UIKit.GoldBtn(st, "<b>START</b>", 36, () => { if (OnStart != null) OnStart(); });
            Refresh();
        }

        void BuildOnline()
        {
            UIKit.Clear(right);
            UIKit.Label(UIKit.At("h", right, 14f, 8f, 260f, 28f), "<b>ONLINE</b>", 22, Color.white, TextAnchor.MiddleLeft);
            Profile p = UIRoot.Prof;
            UIKit.Label(UIKit.At("y", right, 14f, 50f, 262f, 34f), "<b>" + p.name + "</b>   (you)", 22, UIKit.Gold, TextAnchor.MiddleLeft);
            UIKit.Label(UIKit.At("n", right, 14f, 96f, 262f, 130f), "Press START to enter a village and meet other adventurers. Friends, parties and guilds arrive with the online service.", 17, UIKit.Dim, TextAnchor.UpperLeft);
        }

        public void Refresh()
        {
            Profile p = UIRoot.Prof; if (p == null || goldT == null) return;
            goldT.text = "Gold  " + p.gold; aeT.text = "Aether  " + p.aether; lvT.text = "Lv." + p.level; tokT.text = "Tokens  " + p.Count("kingdom_token");
            xpFill.fillAmount = p.level >= Rules.MaxLevel ? 1f : Mathf.Clamp01(p.xp / (float)Rules.XpToNext(p.level));
        }

        public void Destroy() { if (Root != null) UnityEngine.Object.Destroy(Root.gameObject); }
    }

    /// <summary>Mode selection (five cards). Only Adventure World is playable in Version 1; others show why they are locked.</summary>
    public sealed class ModeView
    {
        public RectTransform Root; public Action OnBack, OnPlay; int selected;
        readonly Button[] selBtn = new Button[5];

        static readonly string[] Titles = { "ADVENTURE WORLD", "BATTLE ROYALE", "CLASH / CS", "SIEGE", "BOSS / WORLD EVENT" };
        static readonly string[] Sub = { "Explore the World", "Fight Until the Last Warrior", "Team Combat", "Attack or Defend", "Face World Threats" };
        static readonly string[] Players = { "1-4 party", "100 players", "4 rounds", "10-20 players", "20-50 players" };
        static readonly string[] Diff = { "Easy", "Medium", "Medium", "Hard", "Very Hard" };
        static readonly string[] Info = { "Quests, monsters, crafting and loot. Villages are instanced so many players fit.", "Dropped from the void by a god's hand. Only Adventure gear, skills and potions.", "Round-robin squad fights. Adventure gear only.", "Two teams, defend or destroy the crystal core. Best of 3 rounds.", "Large timed threats and rifts." };
        static readonly int[] Cols = { 0x2a7a4a, 0x2a4a9a, 0x7a2a3a, 0x7a4a1a, 0x5a2a8a };

        public static ModeView Build(Transform parent, Action onBack, Action onPlay)
        {
            ModeView v = new ModeView { OnBack = onBack, OnPlay = onPlay };
            v.Root = UIKit.Fill("Modes", parent);
            v.Create();
            return v;
        }

        void Create()
        {
            Profile p = UIRoot.Prof; bool unlocked = Rules.ModesUnlocked(p);
            RectTransform top = UIKit.Rect("top", Root, 0, 1, 1, 1, 0, -66, 0, 0); UIKit.Img(top, new Color(0.03f, 0.06f, 0.15f, 0.85f), false);
            UIKit.Btn(UIKit.At("back", top, 10f, 8f, 130f, 50f), "< BACK", 22, UIKit.PanelLight, Color.white, () => { if (OnBack != null) OnBack(); });
            UIKit.Label(UIKit.At("nm", top, 160f, 6f, 300f, 30f), "<b>" + p.name + "</b>", 22, Color.white, TextAnchor.MiddleLeft);
            UIKit.Label(UIKit.At("lv", top, 160f, 34f, 300f, 26f), "Lv." + p.level + "   " + (unlocked ? "All modes unlocked" : "Reach Lv.10 to unlock modes"), 17, UIKit.Gold, TextAnchor.MiddleLeft);
            UIKit.Label(UIKit.Rect("logo", top, 0.5f, 0, 0.5f, 1, -240, 0, 240, 0), "<b>SOULBOUND LEGENDS</b>", 30, UIKit.Gold, TextAnchor.MiddleCenter);
            UIKit.Btn(UIKit.AtRight("set", top, 10f, 10f, 100f, 46f), "Settings", 17, UIKit.PanelLight, Color.white, () => Panels.OpenById("settings"));
            for (int i = 0; i < 5; i++)
            {
                int idx = i; bool open = i == 0 || false;
                bool lockedMode = i != 0;
                float x0 = 0.045f + i * 0.158f;
                RectTransform card = UIKit.Rect("card" + i, Root, x0, 0.2f, x0 + 0.146f, 0.86f, 0, 0, 0, 0);
                UIKit.Img(card, new Color(0.05f, 0.1f, 0.26f, 0.95f), true);
                RectTransform art = UIKit.Rect("art", card, 0, 0.5f, 1, 1, 6, 0, -6, -6); UIKit.Img(art, UIKit.Hex(Cols[i]), true);
                UIKit.Label(UIKit.Rect("p", art, 0, 0, 1, 0, 8, 6, -8, 28), Players[i], 17, Color.white, TextAnchor.LowerLeft);
                UIKit.Label(UIKit.Rect("t", card, 0, 0.3f, 1, 0.5f, 6, 0, -6, 0), "<b>" + Titles[i] + "</b>", 21, UIKit.Gold, TextAnchor.MiddleCenter);
                UIKit.Label(UIKit.Rect("s", card, 0, 0.22f, 1, 0.32f, 6, 0, -6, 0), Sub[i], 15, Color.white, TextAnchor.MiddleCenter);
                UIKit.Label(UIKit.Rect("d", card, 0, 0.12f, 1, 0.24f, 6, 0, -6, 0), Diff[i], 16, i == 0 ? UIKit.Green : (i < 3 ? UIKit.Gold : UIKit.Red), TextAnchor.MiddleCenter);
                RectTransform sb = UIKit.Rect("sel", card, 0.08f, 0.015f, 0.92f, 0.115f, 0, 0, 0, 0);
                selBtn[i] = UIKit.GoldBtn(sb, i == 0 ? "SELECTED" : "SELECT", 19, () =>
                {
                    if (lockedMode) UIRoot.I.Toast(Titles[idx] + (unlocked ? " is not playable yet in this version." : " unlocks at Level 10."));
                    else { selected = idx; UpdateSel(); }
                });
                if (lockedMode)
                {
                    RectTransform lk = UIKit.Rect("lock", card, 0, 0, 1, 1, 0, 0, 0, 0); UIKit.Img(lk, new Color(0f, 0f, 0f, 0.55f), true); lk.GetComponent<Image>().raycastTarget = false;
                    UIKit.Label(UIKit.Rect("l", lk, 0, 0.5f, 1, 0.75f, 6, 0, -6, 0), unlocked ? "<b>COMING SOON</b>" : "<b>LOCKED</b>\nReach Level 10", 22, unlocked ? UIKit.Cyan : UIKit.Red, TextAnchor.MiddleCenter);
                }
            }
            RectTransform info = UIKit.AtBottomLeft("info", Root, 10f, 12f, 520f, 70f); UIKit.Img(info, new Color(0f, 0f, 0f, 0.55f), true);
            UIKit.Label(UIKit.Rect("t", info, 0, 0, 1, 1, 12, 0, -12, 0), Info[0], 17, Color.white, TextAnchor.MiddleLeft);
            RectTransform pl = UIKit.AtBottomRight("play", Root, 10f, 12f, 300f, 74f);
            UIKit.GoldBtn(pl, "<b>PLAY</b>", 38, () => { if (selected == 0 && OnPlay != null) OnPlay(); });
            UIKit.Btn(UIKit.AtBottomLeft("em", Root, 540f, 12f, 150f, 52f), "Emote", 20, UIKit.PanelLight, Color.white, () => Panels.OpenById("emotes"));
            UIKit.Btn(UIKit.AtBottomLeft("tm", Root, 700f, 12f, 170f, 52f), "Team / Party", 19, UIKit.PanelLight, Color.white, () => UIRoot.I.Toast("Parties arrive with the online service."));
            UIKit.Btn(UIKit.AtBottomLeft("iv", Root, 880f, 12f, 170f, 52f), "Invite Friends", 19, UIKit.PanelLight, Color.white, () => UIRoot.I.Toast("Friends arrive with the online service."));
            string[] rl = { "Events", "Missions", "Inventory", "Character", "Skills", "Shop" }; string[] ri = { "events", "quests", "inventory", "status", "skills", "store" };
            for (int i = 0; i < rl.Length; i++)
            {
                string id = ri[i]; bool shopLock = id == "store" && !unlocked;
                RectTransform r = UIKit.AtRight("r" + i, Root, 6f, 74f + i * 68f, 96f, 60f);
                UIKit.Btn(r, rl[i], 15, new Color(0.04f, 0.08f, 0.2f, 0.9f), shopLock ? new Color(1f, 1f, 1f, 0.4f) : Color.white, () => { if (shopLock) UIRoot.I.Toast("The Shop unlocks at Level 10."); else Panels.OpenById(id); });
            }
        }

        void UpdateSel() { for (int i = 0; i < selBtn.Length; i++) { Text t = selBtn[i].GetComponentInChildren<Text>(); if (t != null) t.text = i == selected ? "SELECTED" : "SELECT"; } }
        public void Destroy() { if (Root != null) UnityEngine.Object.Destroy(Root.gameObject); }
    }

    /// <summary>Choose how to enter the world: host a village on this phone, or join one by address.</summary>
    public sealed class ConnectView
    {
        public RectTransform Root; Text status; InputField ip;
        public Action OnHost, OnBack; public Action<string> OnJoin;

        public static ConnectView Build(Transform parent, Action onHost, Action<string> onJoin, Action onBack)
        {
            ConnectView v = new ConnectView { OnHost = onHost, OnJoin = onJoin, OnBack = onBack };
            v.Root = UIKit.Fill("Connect", parent); UIKit.Img(v.Root, new Color(0f, 0f, 0f, 0.7f), false);
            RectTransform box = UIKit.Rect("box", v.Root, 0.5f, 0.5f, 0.5f, 0.5f, -330f, -230f, 330f, 230f); UIKit.Img(box, UIKit.Panel, true);
            UIKit.Label(UIKit.At("t", box, 0f, 12f, 660f, 40f), "<b>Enter Adventure World</b>", 30, UIKit.Gold, TextAnchor.MiddleCenter);
            UIKit.Label(UIKit.At("h1", box, 30f, 62f, 600f, 26f), "Host a village on this phone (others can join you):", 18, UIKit.Dim, TextAnchor.MiddleLeft);
            UIKit.GoldBtn(UIKit.At("host", box, 30f, 92f, 600f, 58f), "Host Village", 26, () => { v.SetStatus("Starting village..."); if (v.OnHost != null) v.OnHost(); });
            UIKit.Label(UIKit.At("h2", box, 30f, 168f, 600f, 26f), "Or join a village / game server by address:", 18, UIKit.Dim, TextAnchor.MiddleLeft);
            v.ip = UIKit.Input(UIKit.At("ip", box, 30f, 198f, 400f, 54f), SLSave.GetStr("last_ip", NetBootstrap.Address), "Server address", 40);
            UIKit.GoldBtn(UIKit.At("join", box, 446f, 198f, 184f, 54f), "Join", 24, () => { string a = v.ip.text.Trim(); SLSave.SetStr("last_ip", a); v.SetStatus("Connecting to " + a + " ..."); if (v.OnJoin != null) v.OnJoin(a); });
            v.status = UIKit.Label(UIKit.At("st", box, 30f, 270f, 600f, 80f), "", 20, UIKit.Cyan, TextAnchor.UpperLeft);
            UIKit.Btn(UIKit.At("back", box, 30f, 372f, 200f, 56f), "Back", 22, UIKit.PanelLight, Color.white, () => { if (v.OnBack != null) v.OnBack(); });
            return v;
        }
        public void SetStatus(string s) { if (status != null) status.text = s; }
        public void Destroy() { if (Root != null) UnityEngine.Object.Destroy(Root.gameObject); }
    }
}
