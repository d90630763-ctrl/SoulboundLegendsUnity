using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SL.Core;
using SL.Actors;
using SL.Gameplay;
using SL.Net;
using SL.World;

namespace SL.UI
{
    /// <summary>The 3D backdrop behind the creator and lobby: floating castle, platform ring, and the player's character.</summary>
    public static class Stage
    {
        static GameObject stage, charGo; static Camera cam; static SkyEnvironment sky;
        static float yawTarget; static Transform spin;

        public static void Show(Camera c)
        {
            cam = c; Hide();
            sky = SkyEnvironment.Create(c); sky.Lobby = true; sky.SetHour(10.5f);
            stage = LobbyStage.Build();
            c.transform.position = new Vector3(0f, 1.55f, -5.8f); c.transform.rotation = Quaternion.Euler(4f, 0f, 0f);
            c.fieldOfView = 40f;
        }

        public static void Hide()
        {
            if (stage != null) UnityEngine.Object.Destroy(stage); stage = null;
            if (charGo != null) UnityEngine.Object.Destroy(charGo); charGo = null; LobbyPreview.Rig = null;
            if (sky != null) UnityEngine.Object.Destroy(sky.gameObject); sky = null;
        }

        public static void SetCharacter(Look look, int element, string weaponId)
        {
            float keep = charGo != null ? charGo.transform.eulerAngles.y : 0f;
            if (charGo != null) UnityEngine.Object.Destroy(charGo);
            charGo = CharacterBuilder.Build(look, element, "preview", false, 1.08f);
            charGo.transform.position = new Vector3(0f, 0.04f, 0f); charGo.transform.rotation = Quaternion.Euler(0f, keep, 0f);
            CharacterRig rig = charGo.GetComponent<CharacterRig>();
            ItemDef d = Items.Get(weaponId);
            rig.SetWeapon(d == null ? WeaponType.Sword : d.Wt, d == null ? 0 : d.Rarity, ElementInfo.Col(element < 0 ? 4 : element));
            rig.SetGlow(ElementInfo.Col(element < 0 ? 4 : element));
            LobbyPreview.Rig = rig;
        }

        public static Transform CharTransform { get { return charGo == null ? null : charGo.transform; } }
        public static void SetYaw(float y) { if (charGo != null) charGo.transform.rotation = Quaternion.Euler(0f, y, 0f); }
        public static void Camera(float x, float y, float z, float lookY) { if (cam != null) { cam.transform.position = new Vector3(x, y, z); cam.transform.LookAt(new Vector3(0f, lookY, 0f)); } }
    }

    // ===================================================================================== loading / account
    public sealed class LoadingView
    {
        public RectTransform Root; Image fill; Text status; Text tip; float tipT; int tipI;
        static readonly string[] Tips = { "Villages are instanced, so many adventurers can play at once.", "Reach Level 10 and finish your promotion quest to open the Kingdom.", "Herbs near the village make your first potions.", "Not every monster drops loot. Stronger monsters drop better.", "Chests come in ten grades, from Wood to Celestial.", "You can equip up to 10 skills at a time." };

        public static LoadingView Build(Transform parent)
        {
            LoadingView v = new LoadingView(); v.Root = UIKit.Fill("Loading", parent);
            UIKit.Img(v.Root, new Color(0.02f, 0.04f, 0.1f, 1f), false);
            UIKit.Label(UIKit.Rect("logo", v.Root, 0.5f, 0.5f, 0.5f, 0.5f, -500f, 20f, 500f, 150f), "<b>SOULBOUND</b>", 78, UIKit.Gold, TextAnchor.MiddleCenter);
            UIKit.Label(UIKit.Rect("logo2", v.Root, 0.5f, 0.5f, 0.5f, 0.5f, -500f, -50f, 500f, 40f), "<b>L E G E N D S</b>", 44, new Color(0.85f, 0.92f, 1f), TextAnchor.MiddleCenter);
            RectTransform br = UIKit.Rect("barwrap", v.Root, 0.5f, 0f, 0.5f, 0f, -300f, 90f, 300f, 112f);
            UIKit.Bar(br, "bar", 0, 0, 600, 22, new Color(0f, 0f, 0f, 0.6f), UIKit.Gold, out v.fill);
            v.status = UIKit.Label(UIKit.Rect("st", v.Root, 0.5f, 0f, 0.5f, 0f, -400f, 60f, 400f, 88f), "Loading...", 20, UIKit.Dim, TextAnchor.MiddleCenter);
            v.tip = UIKit.Label(UIKit.Rect("tip", v.Root, 0.5f, 0f, 0.5f, 0f, -500f, 20f, 500f, 54f), Tips[0], 18, new Color(1f, 1f, 1f, 0.45f), TextAnchor.MiddleCenter);
            return v;
        }

        public void Set(float f, string s) { if (fill != null) fill.fillAmount = Mathf.Clamp01(f); if (status != null) status.text = s; }

        public void Tick(float dt)
        {
            tipT += dt; if (tipT > 3.2f && tip != null) { tipT = 0f; tipI = (tipI + 1) % Tips.Length; tip.text = Tips[tipI]; }
        }

        public void Destroy() { if (Root != null) UnityEngine.Object.Destroy(Root.gameObject); }
    }

    public sealed class AccountView
    {
        public RectTransform Root; public Action OnGuest;

        public static AccountView Build(Transform parent, Action onGuest)
        {
            AccountView v = new AccountView { OnGuest = onGuest };
            v.Root = UIKit.Fill("Account", parent);
            UIKit.Img(v.Root, new Color(0.02f, 0.05f, 0.12f, 0.88f), false);
            UIKit.Label(UIKit.Rect("logo", v.Root, 0.5f, 1f, 0.5f, 1f, -500f, -190f, 500f, -60f), "<b>SOULBOUND LEGENDS</b>", 58, UIKit.Gold, TextAnchor.MiddleCenter);
            RectTransform box = UIKit.Rect("box", v.Root, 0.5f, 0.5f, 0.5f, 0.5f, -240f, -190f, 240f, 130f);
            UIKit.Img(box, UIKit.Panel, true);
            UIKit.Label(UIKit.At("t", box, 0f, 14f, 480f, 34f), "<b>Sign in</b>", 28, Color.white, TextAnchor.MiddleCenter);
            string[] names = { "Continue with Google", "Continue with Apple", "Continue with Email" };
            for (int i = 0; i < 3; i++)
            {
                RectTransform b = UIKit.At("b" + i, box, 40f, 64f + i * 56f, 400f, 46f);
                UIKit.Btn(b, names[i], 20, new Color(0.18f, 0.2f, 0.3f, 1f), new Color(1f, 1f, 1f, 0.45f), () => UIRoot.I.Toast("Online sign-in connects when your servers are ready. Use Guest for now."));
            }
            RectTransform g = UIKit.At("guest", box, 40f, 238f, 400f, 54f);
            UIKit.GoldBtn(g, "Play as Guest", 24, () => { if (v.OnGuest != null) v.OnGuest(); });
            UIKit.Label(UIKit.Rect("terms", v.Root, 0.5f, 0f, 0.5f, 0f, -420f, 24f, 420f, 60f), "By playing you agree to the Terms of Service and Privacy Policy.", 16, UIKit.Dim, TextAnchor.MiddleCenter);
            return v;
        }
        public void Destroy() { if (Root != null) UnityEngine.Object.Destroy(Root.gameObject); }
    }

    // ===================================================================================== character creation
    public sealed class CreatorView
    {
        public RectTransform Root; public Action OnDone;
        Look look; string nameStr = "";
        int tab; RectTransform chipArea, previewCard; Text chosenText; InputField nameField; Text errText;
        readonly List<Button> tabBtns = new List<Button>();
        static readonly string[] Tabs = { "Face", "Hair", "Body", "Outfit", "Weapon", "Accessories", "Voice", "Name" };

        public static CreatorView Build(Transform parent, Action onDone)
        {
            CreatorView v = new CreatorView { OnDone = onDone };
            v.look = ClientIdentity.Look != 0 ? Look.Unpack(ClientIdentity.Look) : Look.Default();
            v.nameStr = ClientIdentity.Name == "Adventurer" ? "" : ClientIdentity.Name;
            v.Root = UIKit.Fill("Creator", parent);
            v.BuildFrame();
            v.RefreshAll();
            return v;
        }

        void BuildFrame()
        {
            RectTransform top = UIKit.Rect("top", Root, 0, 1, 1, 1, 0, -64, 0, 0); UIKit.Img(top, new Color(0.03f, 0.06f, 0.15f, 0.88f), false);
            UIKit.Label(UIKit.At("logo", top, 20f, 6f, 420f, 52f), "<b>SOULBOUND LEGENDS</b>", 34, UIKit.Gold, TextAnchor.MiddleLeft);
            UIKit.Label(UIKit.At("sub", top, 470f, 6f, 400f, 52f), "Your Journey Begins...", 22, new Color(0.8f, 0.9f, 1f), TextAnchor.MiddleLeft);
            RectTransform left = UIKit.Rect("left", Root, 0, 0, 0, 1, 0, 90, 188, -64); UIKit.Img(left, new Color(0.03f, 0.06f, 0.15f, 0.88f), false);
            for (int i = 0; i < Tabs.Length; i++)
            {
                int idx = i;
                RectTransform r = UIKit.At("tab" + i, left, 8f, 8f + i * 62f, 172f, 56f);
                Button b = UIKit.Btn(r, Tabs[i], 21, UIKit.PanelLight, Color.white, () => { tab = idx; RefreshAll(); });
                tabBtns.Add(b);
            }
            RectTransform area = UIKit.At("area", Root, 200f, 76f, 420f, 440f); UIKit.Img(area, new Color(0.04f, 0.08f, 0.2f, 0.85f), true);
            chipArea = UIKit.Fill("chips", area);
            RectTransform pv = UIKit.AtRight("preview", Root, 12f, 76f, 290f, 250f); UIKit.Img(pv, new Color(0.04f, 0.08f, 0.2f, 0.88f), true);
            UIKit.Label(UIKit.At("t", pv, 14f, 8f, 260f, 30f), "<b>Preview</b>", 24, Color.white, TextAnchor.MiddleLeft);
            string[] views = { "Front", "Side", "Back" }; float[] ys = { 0f, 90f, 180f };
            for (int i = 0; i < 3; i++) { float yy = ys[i]; RectTransform r = UIKit.At("v" + i, pv, 14f + i * 90f, 60f, 82f, 56f); UIKit.Btn(r, views[i], 18, UIKit.PanelLight, Color.white, () => Stage.SetYaw(yy)); }
            UIKit.Label(UIKit.At("hint", pv, 14f, 130f, 262f, 100f), "Drag anywhere on the character to rotate it.\nChoose a name and press Complete.", 17, UIKit.Dim, TextAnchor.UpperLeft);
            previewCard = UIKit.AtRight("style", Root, 12f, 336f, 290f, 180f); UIKit.Img(previewCard, new Color(0.04f, 0.08f, 0.2f, 0.88f), true);
            UIKit.Label(UIKit.At("t", previewCard, 14f, 8f, 260f, 30f), "<b>Chosen Style</b>", 24, Color.white, TextAnchor.MiddleLeft);
            chosenText = UIKit.Label(UIKit.At("c", previewCard, 14f, 44f, 262f, 130f), "", 18, new Color(0.85f, 0.95f, 1f), TextAnchor.UpperLeft);
            RectTransform spin = UIKit.Rect("spin", Root, 0.36f, 0.14f, 0.66f, 0.82f, 0, 0, 0, 0); UIKit.Img(spin, new Color(0f, 0f, 0f, 0.001f), false);
            SpinPad sp = spin.gameObject.AddComponent<SpinPad>(); sp.Target = null; spinPad = sp;
            RectTransform bot = UIKit.Rect("bottom", Root, 0, 0, 1, 0, 0, 0, 0, 88); UIKit.Img(bot, new Color(0.03f, 0.06f, 0.15f, 0.9f), false);
            UIKit.Label(UIKit.At("lv", bot, 24f, 12f, 330f, 34f), "<b>Soulbound</b>   Lv.0", 24, Color.white, TextAnchor.MiddleLeft);
            UIKit.Label(UIKit.At("sv", bot, 24f, 48f, 330f, 28f), "Server: Village 001", 18, UIKit.Dim, TextAnchor.MiddleLeft);
            UIKit.Btn(UIKit.At("rand", bot, 420f, 14f, 150f, 60f), "Randomize", 20, UIKit.PanelLight, Color.white, () => { string n = nameStr; look = Look.Random(Environment.TickCount); RefreshAll(); });
            UIKit.Btn(UIKit.At("reset", bot, 586f, 14f, 130f, 60f), "Reset", 20, UIKit.PanelLight, Color.white, () => { look = Look.Default(); RefreshAll(); });
            errText = UIKit.Label(UIKit.At("err", bot, 730f, 10f, 330f, 66f), "", 18, UIKit.Red, TextAnchor.MiddleLeft);
            RectTransform done = UIKit.AtBottomRight("done", Root, 14f, 12f, 260f, 64f);
            UIKit.GoldBtn(done, "Complete  >", 28, Finish);
        }

        SpinPad spinPad;

        void RefreshAll()
        {
            for (int i = 0; i < tabBtns.Count; i++) tabBtns[i].targetGraphic.color = i == tab ? UIKit.Gold : UIKit.PanelLight;
            UIKit.Clear(chipArea);
            nameField = null;
            switch (tab)
            {
                case 0: Group("Face", LookPalette.FaceNames, null, 0, () => look.face, v => look.face = v); Group("Eye Color", LookPalette.EyeNames, LookPalette.Eye, 190, () => look.eye, v => look.eye = v); break;
                case 1: Group("Hair Style", LookPalette.HairNames, null, 0, () => look.hairStyle, v => look.hairStyle = v); Group("Hair Color", LookPalette.HairColorNames, LookPalette.Hair, 190, () => look.hairColor, v => look.hairColor = v); break;
                case 2: Group("Body Type", LookPalette.BodyNames, null, 0, () => look.body, v => look.body = v); Group("Skin Tone", LookPalette.SkinNames, LookPalette.Skin, 190, () => look.skin, v => look.skin = v); break;
                case 3: Group("Outfit Color", LookPalette.OutfitNames, LookPalette.Outfit, 0, () => look.outfit, v => look.outfit = v); break;
                case 4: UIKit.Label(UIKit.At("w", chipArea, 20f, 20f, 380f, 300f), "<b>Starting Weapon</b>\n\nEvery adventurer begins with a Wooden Sword.\n\nBetter weapons come from crafting, chests and monster drops. Swords, greatswords, staves, bows and spears are all possible.", 21, Color.white, TextAnchor.UpperLeft); break;
                case 5: Group("Accessory", LookPalette.AccNames, null, 0, () => look.acc, v => look.acc = v); break;
                case 6: Group("Voice", new[] { "Voice 1", "Voice 2", "Voice 3" }, null, 0, () => look.voice, v => look.voice = v); break;
                case 7:
                    {
                        UIKit.Label(UIKit.At("nl", chipArea, 20f, 16f, 380f, 30f), "<b>Character Name</b>", 24, UIKit.Gold, TextAnchor.MiddleLeft);
                        RectTransform nf = UIKit.At("nf", chipArea, 20f, 56f, 380f, 56f);
                        nameField = UIKit.Input(nf, nameStr, "Enter a name (3-16 letters)", 16);
                        nameField.onValueChanged.AddListener(s => { nameStr = s; UpdateChosen(); });
                        UIKit.Label(UIKit.At("nt", chipArea, 20f, 130f, 380f, 220f), "Your name is shown above your head and saved with your character.\n\nYour elemental inheritance is awakened later, at Level 3, by Elder Maren.", 19, UIKit.Dim, TextAnchor.UpperLeft);
                        break;
                    }
            }
            Stage.SetCharacter(look, -1, "wood_sword");
            if (spinPad != null) spinPad.Target = Stage.CharTransform;
            UpdateChosen();
        }

        void Group(string title, string[] names, int[] colors, float yOff, Func<int> get, Action<int> set)
        {
            UIKit.Label(UIKit.At("g", chipArea, 16f, 10f + yOff, 380f, 30f), "<b>" + title + "</b>", 23, UIKit.Gold, TextAnchor.MiddleLeft);
            for (int i = 0; i < names.Length; i++)
            {
                int idx = i; int col = i % 3, row = i / 3;
                RectTransform r = UIKit.At("c" + i, chipArea, 16f + col * 130f, 48f + yOff + row * 66f, 122f, 58f);
                bool sel = get() == i;
                Button b = UIKit.Btn(r, colors == null ? names[i] : "", 18, sel ? UIKit.Gold : UIKit.PanelLight, sel ? new Color(0.15f, 0.08f, 0f) : Color.white, () => { set(idx); RefreshAll(); });
                if (colors != null)
                {
                    RectTransform sw = UIKit.Rect("sw", r, 0.5f, 0.5f, 0.5f, 0.5f, -22f, -22f, 22f, 22f); UIKit.Img(sw, UIKit.Hex(colors[i]), true);
                    UIKit.Label(UIKit.Rect("n", r, 0, 0, 1, 0, 0, 2, 0, 18), names[i], 13, sel ? new Color(0.15f, 0.08f, 0f) : Color.white, TextAnchor.LowerCenter);
                }
            }
        }

        void UpdateChosen()
        {
            if (chosenText == null) return;
            chosenText.text = "Name: " + (string.IsNullOrEmpty(nameStr) ? "(not set)" : nameStr) + "\nFace: " + LookPalette.FaceNames[look.face] + "   Eyes: " + LookPalette.EyeNames[look.eye] + "\nHair: " + LookPalette.HairNames[look.hairStyle] + " (" + LookPalette.HairColorNames[look.hairColor] + ")\nBody: " + LookPalette.BodyNames[look.body] + "   Skin: " + LookPalette.SkinNames[look.skin] + "\nOutfit: " + LookPalette.OutfitNames[look.outfit] + "   Accessory: " + LookPalette.AccNames[look.acc];
        }

        void Finish()
        {
            string n = nameField != null ? nameField.text : nameStr; if (n == null) n = "";
            n = n.Trim();
            if (n.Length < 3) { errText.text = "Pick a name with at least 3 characters (Name tab)."; tab = 7; RefreshAll(); return; }
            for (int i = 0; i < n.Length; i++) if (!char.IsLetterOrDigit(n[i]) && n[i] != ' ') { errText.text = "Names may only use letters, numbers and spaces."; return; }
            SLSave.SetStr("char_name", n); SLSave.SetStr("char_look", look.Pack().ToString()); SLSave.SetInt("char_made", 1); SLSave.Flush();
            if (ClientIdentity.Name != n) UIRoot.ResetCache();
            ClientIdentity.Name = n; ClientIdentity.Look = look.Pack();
            if (OnDone != null) OnDone();
        }

        public void Destroy() { if (Root != null) UnityEngine.Object.Destroy(Root.gameObject); }
    }
}
