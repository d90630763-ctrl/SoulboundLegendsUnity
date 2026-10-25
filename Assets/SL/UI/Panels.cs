using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SL.Core;
using SL.Gameplay;
using SL.Net;
using SL.World;

namespace SL.UI
{
    /// <summary>A modal window with a scrolling list. Builders re-run whenever the profile changes so panels stay live.</summary>
    public sealed class PanelWindow
    {
        public RectTransform Root, Content; public string Id; public Action<PanelWindow> Builder;
        float y; const float W = 840f;

        public void Rebuild()
        {
            UIKit.Clear(Content); y = 6f;
            try { Builder(this); } catch (Exception e) { SLLog.E("Panel '" + Id + "' failed: " + e.Message); Text("This panel could not be drawn.", 20, UIKit.Red); }
            UIKit.SetContentHeight(Content, y + 14f);
        }

        public void Gap(float h) { y += h; }

        public void Header(string t)
        {
            RectTransform r = UIKit.At("h", Content, 8f, y, W - 16f, 34f);
            UIKit.Label(r, "<b>" + t + "</b>", 24, UIKit.Gold, TextAnchor.MiddleLeft);
            y += 40f;
        }

        public void Text(string t, int size, Color c)
        {
            int perLine = Mathf.Max(10, (int)((W - 30f) / (size * 0.52f)));
            int lines = 0; string[] parts = t.Split('\n');
            for (int i = 0; i < parts.Length; i++) lines += Mathf.Max(1, Mathf.CeilToInt(parts[i].Length / (float)perLine));
            float h = lines * size * 1.28f + 8f;
            RectTransform r = UIKit.At("t", Content, 12f, y, W - 24f, h);
            UIKit.Label(r, t, size, c, TextAnchor.UpperLeft);
            y += h + 4f;
        }

        public void Bar(string label, float frac, Color c, string right)
        {
            RectTransform r = UIKit.At("bar", Content, 8f, y, W - 16f, 34f);
            UIKit.Img(r, new Color(0.02f, 0.04f, 0.1f, 1f), true);
            RectTransform f = UIKit.Rect("f", r, 0, 0, Mathf.Clamp01(frac), 1, 3, 3, -3, -3);
            UIKit.Img(f, c, true);
            UIKit.Label(UIKit.Rect("l", r, 0, 0, 1, 1, 12, 0, -12, 0), label, 18, Color.white, TextAnchor.MiddleLeft);
            UIKit.Label(UIKit.Rect("r", r, 0, 0, 1, 1, 12, 0, -12, 0), right, 18, Color.white, TextAnchor.MiddleRight);
            y += 40f;
        }

        public Button Row(string title, string sub, Color tc, string btn, Action cb, bool enabled)
        {
            float h = string.IsNullOrEmpty(sub) ? 52f : 72f;
            if (!string.IsNullOrEmpty(sub)) { int lines = Mathf.CeilToInt(sub.Length / 70f) + sub.Split('\n').Length - 1; h = Mathf.Max(72f, 36f + lines * 20f); }
            RectTransform r = UIKit.At("row", Content, 8f, y, W - 16f, h);
            UIKit.Img(r, new Color(0.1f, 0.17f, 0.34f, 0.95f), true);
            float tw = string.IsNullOrEmpty(btn) ? W - 40f : W - 190f;
            UIKit.Label(UIKit.At("tt", r, 14f, 6f, tw, 28f), title, 22, tc, TextAnchor.MiddleLeft);
            if (!string.IsNullOrEmpty(sub)) UIKit.Label(UIKit.At("ss", r, 14f, 34f, tw, h - 38f), sub, 16, new Color(1f, 1f, 1f, 0.7f), TextAnchor.UpperLeft);
            Button b = null;
            if (!string.IsNullOrEmpty(btn))
            {
                RectTransform br = UIKit.AtRight("b", r, 10f, (h - 44f) * 0.5f, 150f, 44f);
                b = enabled ? UIKit.GoldBtn(br, btn, 20, cb) : UIKit.Btn(br, btn, 18, new Color(0.2f, 0.22f, 0.3f, 1f), new Color(1f, 1f, 1f, 0.4f), null);
            }
            y += h + 8f;
            return b;
        }

        public void ButtonGrid(string[] labels, Action<int> cb, int perRow)
        {
            float bw = (W - 16f - (perRow - 1) * 8f) / perRow;
            for (int i = 0; i < labels.Length; i++)
            {
                int col = i % perRow, row = i / perRow; int idx = i;
                RectTransform r = UIKit.At("g", Content, 8f + col * (bw + 8f), y + row * 54f, bw, 46f);
                UIKit.GoldBtn(r, labels[i], 19, () => cb(idx));
            }
            y += (labels.Length + perRow - 1) / perRow * 54f + 4f;
        }
    }

    public static class Panels
    {
        static RectTransform backdrop; static PanelWindow current;
        public static Action OnReplayIntro, OnEditCharacter, OnLeaveWorld, OnResetAll;
        public static bool IsOpen { get { return current != null; } }

        static PlayerNet Me { get { return PlayerNet.Local; } }
        static bool Online { get { return PlayerNet.Local != null && PlayerNet.Local.IsSpawned; } }
        static Profile P { get { return UIRoot.Prof; } }

        public static void Close()
        {
            if (backdrop != null) UnityEngine.Object.Destroy(backdrop.gameObject);
            backdrop = null; current = null;
            PlayerNet.ProfileEvent -= Refresh;
        }

        static void Refresh() { if (current != null) current.Rebuild(); }

        public static PanelWindow Open(string id, string title, Action<PanelWindow> builder)
        {
            Close();
            UIRoot root = UIRoot.I;
            backdrop = UIKit.Fill("PanelBackdrop", root.PanelLayer);
            UIKit.Img(backdrop, new Color(0f, 0f, 0f, 0.62f), false);
            Button bg = backdrop.gameObject.AddComponent<Button>(); bg.targetGraphic = backdrop.GetComponent<Image>();
            RectTransform win = UIKit.Rect("Window", backdrop, 0.5f, 0.5f, 0.5f, 0.5f, -450f, -300f, 450f, 300f);
            UIKit.Img(win, UIKit.Panel, true);
            RectTransform bar = UIKit.At("bar", win, 0f, 0f, 900f, 58f);
            UIKit.Img(bar, new Color(0.12f, 0.2f, 0.4f, 1f), true);
            UIKit.Label(UIKit.Rect("title", bar, 0, 0, 1, 1, 20, 0, -80, 0), "<b>" + title + "</b>", 28, UIKit.Gold, TextAnchor.MiddleLeft);
            RectTransform cb = UIKit.AtRight("close", bar, 8f, 7f, 64f, 44f);
            UIKit.Btn(cb, "X", 24, new Color(0.7f, 0.2f, 0.2f, 1f), Color.white, Close);
            RectTransform content;
            UIKit.Scroll(win, "scroll", 18f, 68f, 864f, 520f, out content);
            current = new PanelWindow { Root = win, Content = content, Id = id, Builder = builder };
            PlayerNet.ProfileEvent += Refresh;
            current.Rebuild();
            return current;
        }

        public static void OpenById(string id)
        {
            switch (id)
            {
                case "status": Open(id, "Character Status", Status); break;
                case "inventory": Open(id, "Inventory", Inventory); break;
                case "skills": Open(id, "Skills", SkillsPanel); break;
                case "equipment": Open(id, "Equipment", Equipment); break;
                case "quests": Open(id, "Quests", QuestsPanel); break;
                case "events": Open(id, "Events", EventsPanel); break;
                case "store": Open(id, "Store", w => Locked(w, "The Store", "Cosmetics, outfits and bundles will appear here.")); break;
                case "luck": Open(id, "Luck Royale", w => Locked(w, "Luck Royale", "Fate Ticket Draw, Fate Vault and Aether Draw. Every draw is decided by the server.")); break;
                case "topup": Open(id, "Top-Up", w => Locked(w, "Top-Up", "Premium currency packages. Purchases are verified by the server before anything is granted.")); break;
                case "vault": Open(id, "Vault", VaultPanel); break;
                case "pet": Open(id, "Pets", w => { w.Header("Pets"); w.Text("Companions are planned for a later version.", 22, UIKit.Dim); }); break;
                case "collection": Open(id, "Collection", CollectionPanel); break;
                case "rank": Open(id, "Rank", w => Locked(w, "Competitive Rank", "Rank is earned in Clash Strike, Battle Royale and Siege.")); break;
                case "emotes": Open(id, "Emotes", EmotesPanel); break;
                case "friends": Open(id, "Friends & Players", FriendsPanel); break;
                case "mail": Open(id, "Mail & Free Items", MailPanel); break;
                case "settings": Open(id, "Settings", SettingsPanel); break;
                case "map": Open(id, "World", WorldPanel); break;
            }
        }

        static string Lock(string what) { return what + " unlocks at Level 10."; }

        static void Locked(PanelWindow w, string name, string desc)
        {
            Profile p = P; bool unlocked = Rules.ShopUnlocked(p);
            w.Header(name);
            w.Text(desc, 20, Color.white);
            w.Gap(10f);
            if (!unlocked) w.Text("LOCKED - " + Lock(name) + "\nReach Level 10 in Adventure World and finish your promotion quest.", 22, UIKit.Red);
            else w.Text("Unlocked - this feature opens when the online service is connected.", 22, UIKit.Green);
        }

        // ------------------------------------------------------------------ status
        static void Status(PanelWindow w)
        {
            Profile p = P; Stats s = Rules.Compute(p);
            w.Header(p.name + "   Lv." + p.level);
            int need = Rules.XpToNext(p.level);
            w.Bar("Experience", p.level >= Rules.MaxLevel ? 1f : p.xp / (float)need, new Color(0.9f, 0.7f, 0.2f), p.xp + " / " + need);
            PlayerNet me = Me;
            int hp = me != null ? me.NHp.Value : s.MaxHp, mp = me != null ? me.NMp.Value : s.MaxMp;
            w.Bar("Health", hp / (float)s.MaxHp, new Color(0.85f, 0.25f, 0.25f), hp + " / " + s.MaxHp);
            w.Bar("Mana", mp / (float)s.MaxMp, new Color(0.25f, 0.5f, 0.95f), mp + " / " + s.MaxMp);
            w.Gap(6f);
            w.Header("Combat Stats");
            w.Text("Attack " + s.Atk + "     Defense " + s.Def + "\nH " + s.H + "   S " + s.S + "   T " + s.T + "   B " + s.B + "   M " + s.M + "   (equipment bonuses)", 21, Color.white);
            w.Header("Progress");
            string god = p.element < 0 ? "Not awakened (reach Level 3 and speak to Elder Maren)" : ElementInfo.God(p.element);
            w.Text("Inheritance: " + god + "\nGold: " + p.gold + "     Aether: " + p.aether + "\nMonsters defeated: " + p.totalKills + "     Deaths: " + p.deaths + "\nChests opened: " + p.chestsOpened + "     Items crafted: " + p.crafted + "\nMilestones earned: " + p.milestones.Count + " / " + Milestones.All.Count + "\nTime played: " + (p.playSeconds / 60) + " min", 21, Color.white);
            string stage = p.level < 10 ? "Reach Level 10 to begin your promotion." : (p.promoted ? (p.kingdomEntered ? "Citizen of Aurelion Kingdom." : "Promoted. Use a Kingdom Token at the village portal.") : "Level 10 reached. Complete the promotion quest with Elder Maren.");
            w.Text("Journey: " + stage, 21, UIKit.Cyan);
            w.Text(Rules.ModesUnlocked(p) ? "Modes and Top-Up: UNLOCKED" : "Modes and Top-Up: locked until Level 10", 21, Rules.ModesUnlocked(p) ? UIKit.Green : UIKit.Red);
        }

        // ------------------------------------------------------------------ inventory
        static void Inventory(PanelWindow w)
        {
            Profile p = P;
            w.Header("Gold " + p.gold);
            bool any = false;
            ItemKind[] order = { ItemKind.Chest, ItemKind.Potion, ItemKind.Pill, ItemKind.Token, ItemKind.Weapon, ItemKind.Armor, ItemKind.Accessory, ItemKind.Material };
            for (int o = 0; o < order.Length; o++)
                for (int i = 0; i < p.inv.Count; i++)
                {
                    ItemStack st = p.inv[i]; ItemDef d = Items.Get(st.id);
                    if (d == null || d.Kind != order[o] || st.n <= 0) continue;
                    any = true; string id = st.id;
                    Color rc = UIKit.Hex(Items.RarityHex[Mathf.Clamp(d.Rarity, 0, 5)]);
                    string sub = d.Desc + (d.Equippable ? "\n" + d.StatLine() + "   (req Lv." + d.Req + ")" : "");
                    string eq = (id == p.weapon || id == p.armor || id == p.accessory) ? "  [equipped]" : "";
                    if (d.Kind == ItemKind.Potion || d.Kind == ItemKind.Pill || d.Kind == ItemKind.Chest) w.Row(d.Name + "  x" + st.n, sub, rc, d.Kind == ItemKind.Chest ? "Open" : "Use", () => { if (Me != null) Me.UseItemServerRpc(id); }, Online);
                    else if (d.Equippable) w.Row(d.Name + "  x" + st.n + eq, sub, rc, "Equip", () => { if (Me != null) Me.EquipServerRpc(id); }, Online && p.level >= d.Req);
                    else w.Row(d.Name + "  x" + st.n, sub, rc, null, null, false);
                }
            if (!any) w.Text("Your bag is empty.", 22, UIKit.Dim);
            if (!Online) w.Text("Join a village to use or equip items.", 18, UIKit.Dim);
        }

        // ------------------------------------------------------------------ skills
        static void SkillsPanel(PanelWindow w)
        {
            Profile p = P;
            w.Header("Equipped " + p.loadout.Count + " / " + Skills.MaxLoadout);
            if (p.element < 0) w.Text("Reach Level 3 and speak to Elder Maren to awaken your inheritance and unlock elemental skills.", 19, UIKit.Cyan);
            for (int i = 0; i < Skills.All.Count; i++)
            {
                SkillDef d = Skills.All[i];
                if (d.Element >= 0 && d.Element != p.element) continue;
                bool unlocked = p.level >= d.Level; bool eq = p.loadout.Contains(d.Id); string id = d.Id;
                Color col = d.Element < 0 ? Color.white : ElementInfo.Col(d.Element);
                string sub = d.Desc + "\nMana " + d.Mp + "   Cooldown " + d.Cd.ToString("0.#") + "s   " + (unlocked ? "" : "Unlocks at Lv." + d.Level);
                if (!unlocked) col *= 0.5f;
                w.Row(d.Name + (eq ? "   [equipped]" : ""), sub, col, unlocked ? (eq ? "Unequip" : "Equip") : "Locked", () => { if (Me != null) Me.LoadoutServerRpc(id, !eq); }, unlocked && Online);
            }
            if (!Online) w.Text("Join a village to change your equipped skills.", 18, UIKit.Dim);
        }

        // ------------------------------------------------------------------ equipment
        static void Equipment(PanelWindow w)
        {
            Profile p = P; Stats s = Rules.Compute(p);
            w.Header("Attack " + s.Atk + "   Defense " + s.Def + "   HP " + s.MaxHp + "   MP " + s.MaxMp);
            string[] slots = { "Weapon", "Armor", "Accessory" }; string[] cur = { p.weapon, p.armor, p.accessory };
            for (int sl = 0; sl < 3; sl++)
            {
                ItemDef cd = Items.Get(cur[sl]);
                w.Row(slots[sl] + ":  " + (cd == null ? "(empty)" : cd.Name), cd == null ? "Nothing equipped." : cd.StatLine(), cd == null ? UIKit.Dim : UIKit.Hex(Items.RarityHex[Mathf.Clamp(cd.Rarity, 0, 5)]), null, null, false);
                for (int i = 0; i < p.inv.Count; i++)
                {
                    ItemDef d = Items.Get(p.inv[i].id);
                    if (d == null || !d.Equippable || d.Slot != sl || d.Id == cur[sl] || p.inv[i].n <= 0) continue;
                    string id = d.Id;
                    w.Row("   " + d.Name, d.StatLine() + "   (req Lv." + d.Req + ")", UIKit.Hex(Items.RarityHex[Mathf.Clamp(d.Rarity, 0, 5)]), "Equip", () => { if (Me != null) Me.EquipServerRpc(id); }, Online && p.level >= d.Req);
                }
            }
            if (!Online) w.Text("Join a village to change equipment.", 18, UIKit.Dim);
        }

        // ------------------------------------------------------------------ quests
        static string Rewards(QuestDef q)
        {
            string r = "Reward: " + q.Xp + " XP, " + q.Gold + " gold";
            for (int i = 0; i < q.Rewards.Count; i++) r += ", " + Items.Name(q.Rewards[i].id) + " x" + q.Rewards[i].n;
            return r;
        }

        static string Progress(Profile p, QuestDef q)
        {
            string s = "";
            for (int i = 0; i < q.Objs.Count; i++) s += (Rules.ObjProgress(p, q, i) >= q.Objs[i].Count ? "[x] " : "[ ] ") + q.Objs[i].Text + "  (" + Rules.ObjProgress(p, q, i) + "/" + q.Objs[i].Count + ")\n";
            return s.TrimEnd('\n');
        }

        static void QuestsPanel(PanelWindow w)
        {
            Profile p = P; int active = 0, done = 0;
            w.Header("Active");
            for (int i = 0; i < Quests.All.Count; i++)
            {
                QuestDef q = Quests.All[i]; QuestProg qp = p.Q(q.Id);
                if (qp == null) continue; if (qp.done) { done++; continue; }
                if (!qp.accepted && !(q.Hidden && qp.discovered)) continue;
                active++; string id = q.Id; bool ok = Rules.Complete(p, q);
                string from = string.IsNullOrEmpty(q.Giver) ? "Hidden quest" : "From " + UIRoot.NpcName(q.Giver);
                w.Row(q.Title + (ok ? "   (ready)" : ""), from + "\n" + Progress(p, q) + "\n" + Rewards(q), q.Hidden ? new Color(0.8f, 0.6f, 1f) : Color.white, q.Hidden ? (ok ? "Claim" : null) : "Abandon", () => { if (Me != null) Me.QuestServerRpc(id, q.Hidden ? 1 : 2); }, Online);
            }
            if (active == 0) w.Text("No active quests. Talk to villagers marked with !", 20, UIKit.Dim);
            w.Header("Available");
            int avail = 0;
            for (int i = 0; i < Quests.All.Count; i++)
            {
                QuestDef q = Quests.All[i]; if (!Rules.Available(p, q)) continue; avail++;
                w.Row(q.Title, "Speak to " + UIRoot.NpcName(q.Giver) + "  -  " + q.Desc, UIKit.Gold, null, null, false);
            }
            if (avail == 0) w.Text("Nothing new right now.", 20, UIKit.Dim);
            w.Text("Completed quests: " + done, 20, UIKit.Cyan);
            w.Text("Some quests are hidden. Explore, fight, and discover them yourself.", 18, UIKit.Dim);
        }

        // ------------------------------------------------------------------ events / world / vault / collection
        static void EventsPanel(PanelWindow w)
        {
            Profile p = P;
            w.Header("Current Events");
            w.Row("First Steps Festival", "Finish the starter quests in Dawnrest Village for bonus rewards.", UIKit.Gold, null, null, false);
            w.Row("Race to Level 10", "The first adventurer in the world to reach Level 10 wins an extra-grade chest.", UIKit.Gold, null, null, false);
            w.Row("Rift Alerts", "Timed rifts are not open yet. They arrive in a later update.", UIKit.Dim, null, null, false);
            w.Header("Your Milestones");
            for (int i = 0; i < Milestones.All.Count; i++)
            {
                Milestone m = Milestones.All[i]; bool got = p.Has(m.Id);
                w.Row((got ? "[done] " : "[    ] ") + m.Title, "Reward: " + Items.ChestGrades[m.ChestGrade] + " Chest + " + m.Gold + " gold" + (m.WorldFirst ? "   (extra for the world's first)" : ""), got ? UIKit.Green : Color.white, null, null, false);
            }
        }

        static void WorldPanel(PanelWindow w)
        {
            w.Header("World");
            Profile p = P;
            w.Text("Zone: " + (p.zone == 1 ? "Aurelion Kingdom" : "Dawnrest Village"), 22, Color.white);
            if (Online) w.Text("Adventurers nearby: " + CountNearby(), 20, UIKit.Cyan);
            w.Text("Village: herbs near the plaza, slimes and wolves on the outskirts, golems beyond the river, and the Ancient Warden at the Trial Grounds in the north-west.\nKingdom: stronger monsters, castle walls that break, more NPCs with quests.", 19, Color.white);
        }

        static int CountNearby() { int n = 0; for (int i = 0; i < PlayerNet.All.Count; i++) if (PlayerNet.All[i].InMyInstance) n++; return n; }

        static void VaultPanel(PanelWindow w)
        {
            Profile p = P;
            w.Header("Currencies");
            w.Text("Gold: " + p.gold + "\nAether: " + p.aether + "  (premium currency - locked until Level 10)", 22, Color.white);
            w.Header("Materials");
            bool any = false;
            for (int i = 0; i < p.inv.Count; i++) { ItemDef d = Items.Get(p.inv[i].id); if (d != null && d.Kind == ItemKind.Material && p.inv[i].n > 0) { any = true; w.Row(d.Name + "  x" + p.inv[i].n, d.Desc, UIKit.Hex(Items.RarityHex[Mathf.Clamp(d.Rarity, 0, 5)]), null, null, false); } }
            if (!any) w.Text("No materials yet. Gather herbs, ore and wood in the village.", 20, UIKit.Dim);
        }

        static void CollectionPanel(PanelWindow w)
        {
            Profile p = P;
            w.Header("Monsters Defeated");
            for (int i = 0; i < Monsters.All.Count; i++) { MonsterDef d = Monsters.All[i]; w.Text(d.Name + "  (Lv." + d.Level + ")  -  " + p.Kills(d.Id) + " defeated", 20, p.Kills(d.Id) > 0 ? Color.white : UIKit.Dim); }
            w.Header("Chest Grades");
            string g = ""; for (int i = 0; i < Items.ChestGrades.Length; i++) g += (i > 0 ? "  >  " : "") + Items.ChestGrades[i];
            w.Text(g, 19, UIKit.Gold);
        }

        // ------------------------------------------------------------------ emotes / friends / mail / settings
        static void EmotesPanel(PanelWindow w)
        {
            w.Header("Emotes");
            string[] names = { "Wave", "Cheer", "Bow" };
            w.ButtonGrid(names, i => { if (Me != null) Me.EmoteServerRpc(i); else if (LobbyPreview.Rig != null) LobbyPreview.Rig.Play(SL.Actors.Anim.Emote, i); }, 3);
            w.Text("Emote near another adventurer to discover hidden things.", 18, UIKit.Dim);
        }

        static void FriendsPanel(PanelWindow w)
        {
            w.Header("Adventurers in your area");
            if (!Online) { w.Text("Join a village to see other players.", 20, UIKit.Dim); return; }
            for (int i = 0; i < PlayerNet.All.Count; i++)
            {
                PlayerNet o = PlayerNet.All[i]; if (!o.InMyInstance) continue;
                w.Row(o.DisplayName + "   Lv." + o.NLevel.Value, o == Me ? "You" : "Online", o == Me ? UIKit.Gold : Color.white, null, null, false);
            }
            w.Text("Friend requests, parties and guilds arrive with the online service.", 18, UIKit.Dim);
        }

        static void MailPanel(PanelWindow w)
        {
            Profile p = P;
            w.Header("Free Items");
            PlayerNet me = Me;
            bool pill = me != null && me.PillAvail && p.level >= 1 && p.level < 10;
            string pillText = me == null ? "Join a village to check." : (!me.PillAvail ? "Not available yet. It appears once enough adventurers have been promoted." : (p.level >= 10 ? "Only works from level 1 to 9." : "Claim one pill per day. Using it raises your level by 1 (levels 1-9)."));
            w.Row("LV Up Pill", pillText, UIKit.Gold, "Claim", () => { if (Me != null) Me.ClaimServerRpc(0); }, pill);
            w.Header("Mail");
            if (p.mail.Count == 0) w.Text("No messages.", 20, UIKit.Dim);
            for (int i = 0; i < p.mail.Count; i++) w.Row(p.mail[i].subject, p.mail[i].body, Color.white, null, null, false);
        }

        static void SettingsPanel(PanelWindow w)
        {
            w.Header("Graphics");
            string[] q = { "Low", "Medium", "High" };
            w.ButtonGrid(q, i => { QualitySettings.SetQualityLevel(Mathf.Clamp(i * 2, 0, Mathf.Max(0, QualitySettings.names.Length - 1))); SLSave.SetInt("quality", i); }, 3);
            string[] fps = { "30 FPS", "45 FPS", "60 FPS" };
            w.ButtonGrid(fps, i => { int[] f = { 30, 45, 60 }; Application.targetFrameRate = f[i]; SLSave.SetInt("fps", f[i]); }, 3);
            w.Header("Game");
            w.Row("Replay the intro", "Watch the opening story again next time you launch.", Color.white, "Replay", () => { SLSave.SetInt("intro_seen", 0); SLSave.Flush(); if (OnReplayIntro != null) OnReplayIntro(); }, true);
            w.Row("Edit character", "Change your look in the character creator.", Color.white, "Edit", () => { Close(); if (OnEditCharacter != null) OnEditCharacter(); }, !Online);
            if (Online) w.Row("Leave the world", "Return to the lobby.", Color.white, "Leave", () => { Close(); if (OnLeaveWorld != null) OnLeaveWorld(); }, true);
            w.Row("Reset local data", "Deletes this phone's saved character and settings.", UIKit.Red, "Reset", () => { PlayerPrefs.DeleteAll(); PlayerPrefs.Save(); UIRoot.ResetCache(); Close(); if (OnResetAll != null) OnResetAll(); }, !Online);
            w.Text("Soulbound Legends  v0.1  (Version 1 foundation)", 16, UIKit.Dim);
        }

        // ------------------------------------------------------------------ NPC dialogue, crafting, chest result
        static readonly Dictionary<string, string> Greets = new Dictionary<string, string>
        {
            { "elder", "Welcome, young one. Dawnrest has stood at the edge of the wild for generations. Prove yourself and the world will open to you." },
            { "smith", "Steel remembers every blow. Bring me ore and wood and I will forge you something worth carrying." },
            { "alchemist", "Herbs, a little patience, and a steady hand. That is all a potion needs." },
            { "innkeeper", "Sit, rest. They say the ruins to the north-west hum at night, and something old sleeps there." },
            { "guard", "Keep to the road at night. Beyond the river the beasts grow bolder." },
            { "kguide", "Aurelion welcomes you. The Kingdom has work for anyone with courage." },
            { "ksmith", "Kingdom steel is a cut above village iron. Show me what you have." },
            { "kalch", "My brews keep the royal guard alive. Gather the rare herbs and I will teach you." }
        };

        public static void OpenDialog(string npcId)
        {
            string name = UIRoot.NpcName(npcId);
            Open("dialog", name, w => Dialog(w, npcId, name));
        }

        static string RoleOf(string npcId)
        {
            for (int z = 0; z < 2; z++) { ZoneLayout l = ZoneLayout.Get(z); for (int i = 0; i < l.Npcs.Count; i++) if (l.Npcs[i].Id == npcId) return l.Npcs[i].Role; }
            return "";
        }

        static void Dialog(PanelWindow w, string npcId, string name)
        {
            Profile p = P; string role = RoleOf(npcId);
            string g; if (!Greets.TryGetValue(npcId, out g)) g = "Well met, traveller. Times are strange; keep your blade sharp.";
            w.Text("\"" + g + "\"", 21, new Color(1f, 0.95f, 0.8f));
            List<QuestDef> qs = Quests.ForGiver(npcId);
            for (int i = 0; i < qs.Count; i++)
            {
                QuestDef q = qs[i]; QuestProg qp = p.Q(q.Id); string id = q.Id;
                if (q.Hidden) continue;
                if (qp != null && qp.done) continue;
                if (qp != null && qp.accepted)
                {
                    bool ok = Rules.Complete(p, q);
                    w.Row(q.Title + (ok ? "  (complete)" : "  (in progress)"), Progress(p, q) + "\n" + Rewards(q), ok ? UIKit.Green : Color.white, ok ? "Turn in" : null, () => { if (Me != null) Me.QuestServerRpc(id, 1); }, ok);
                }
                else if (Rules.Available(p, q))
                    w.Row(q.Title, q.Desc + "\n" + Progress(p, q) + "\n" + Rewards(q), UIKit.Gold, "Accept", () => { if (Me != null) Me.QuestServerRpc(id, 0); }, true);
                else if (p.level < q.MinLevel)
                    w.Row(q.Title, "Requires level " + q.MinLevel + ".", UIKit.Dim, null, null, false);
            }
            if (npcId == "elder" && p.element < 0 && p.level >= 3)
            {
                w.Header("Awaken your inheritance (permanent)");
                string[] names = new string[10]; for (int i = 0; i < 10; i++) names[i] = ElementInfo.God(i);
                w.ButtonGrid(names, i => { if (Me != null) { Me.ChooseElementServerRpc(i); Close(); } }, 2);
            }
            else if (npcId == "elder" && p.element < 0) w.Text("Return at level 3 to awaken your inheritance.", 18, UIKit.Cyan);
            if (role == "smith") { w.Gap(4f); w.Row("Forge", "Craft weapons and armour from your materials.", UIKit.Cyan, "Open", () => OpenCraft("smith"), true); }
            if (role == "alchemist") { w.Gap(4f); w.Row("Alchemy", "Brew potions from herbs.", UIKit.Cyan, "Open", () => OpenCraft("alchemist"), true); }
            if (role != "guard" && role != "lore") w.Row("Sell items", "Sell spare materials from your bag for gold.", UIKit.Cyan, "Open", OpenSell, true);
        }

        public static void OpenCraft(string station)
        {
            Open("craft", station == "smith" ? "Forge" : "Alchemy", w =>
            {
                Profile p = P; w.Header("Gold " + p.gold);
                for (int i = 0; i < Recipes.All.Count; i++)
                {
                    Recipe r = Recipes.All[i]; if (r.Station != station) continue;
                    string mats = ""; bool ok = p.level >= r.Req && p.gold >= r.Gold;
                    for (int m = 0; m < r.Mats.Length; m++) { int have = p.Count(r.Mats[m]); if (have < r.Counts[m]) ok = false; mats += Items.Name(r.Mats[m]) + " " + have + "/" + r.Counts[m] + "   "; }
                    ItemDef res = Items.Get(r.Result); string id = r.Id;
                    w.Row(res.Name + (r.Qty > 1 ? " x" + r.Qty : ""), (res.Equippable ? res.StatLine() + "\n" : res.Desc + "\n") + mats + (r.Gold > 0 ? "Gold " + r.Gold : "") + (r.Req > 1 ? "   Req Lv." + r.Req : ""), ok ? Color.white : UIKit.Dim, "Craft", () => { if (Me != null) Me.CraftServerRpc(id); }, ok && Online);
                }
            });
        }

        static void OpenSell()
        {
            Open("sell", "Sell Items", w =>
            {
                Profile p = P; w.Header("Gold " + p.gold); bool any = false;
                for (int i = 0; i < p.inv.Count; i++)
                {
                    ItemDef d = Items.Get(p.inv[i].id); if (d == null || p.inv[i].n <= 0 || d.Kind == ItemKind.Token || d.Kind == ItemKind.Pill || d.Kind == ItemKind.Chest) continue;
                    any = true; string id = d.Id; int unit = Mathf.Max(1, d.Value / 2);
                    w.Row(d.Name + "  x" + p.inv[i].n, "Sells for " + unit + " gold each", Color.white, "Sell 1", () => { if (Me != null) Me.SellServerRpc(id, 1); }, Online);
                }
                if (!any) w.Text("Nothing to sell.", 20, UIKit.Dim);
            });
        }

        public static void OpenChestResult(string grade, string text)
        {
            Open("chest", grade + " Chest opened!", w => { w.Header(grade + " Chest"); w.Text(text.Replace(",  ", "\n"), 24, Color.white); });
        }
    }

    /// <summary>Handle to the 3D character shown in the lobby/creator, so panels can preview emotes.</summary>
    public static class LobbyPreview { public static SL.Actors.CharacterRig Rig; }
}
