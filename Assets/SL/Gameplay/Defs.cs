using System;
using System.Collections.Generic;
using UnityEngine;
using SL.Core;
using SL.Actors;

namespace SL.Gameplay
{
    public static class ElementInfo
    {
        public static readonly string[] Names = { "Water", "Fire", "Earth", "Void", "Thunder", "Nature", "Light", "Air", "Ice", "Omnigod" };
        public static readonly int[] Hex = { 0x3aa0ff, 0xff5a1f, 0x9a7b4f, 0x8a3cff, 0x6aa8ff, 0x4fd04f, 0xfff0a0, 0xa0f0ff, 0xbfe8ff, 0xffffff };
        public static Color Col(int e) { return e < 0 || e > 9 ? Color.white : SLMath.Hex(Hex[e]); }
        public static string God(int e) { return e < 0 || e > 9 ? "None" : (e == 9 ? "Omnigod" : Names[e] + " God"); }
    }

    public enum ItemKind { Material, Potion, Weapon, Armor, Accessory, Token, Pill, Chest }

    public sealed class ItemDef
    {
        public string Id, Name, Desc; public ItemKind Kind; public WeaponType Wt;
        public int Rarity, H, S, T, B, M, Heal, Mana, Value, Req;
        public bool Equippable { get { return Kind == ItemKind.Weapon || Kind == ItemKind.Armor || Kind == ItemKind.Accessory; } }
        public int Slot { get { return Kind == ItemKind.Weapon ? 0 : (Kind == ItemKind.Armor ? 1 : 2); } }
        public string StatLine()
        {
            string s = "";
            if (S != 0) s += "STR " + S + "  "; if (T != 0) s += "TOU " + T + "  "; if (H != 0) s += "HP " + H + "  ";
            if (M != 0) s += "MAG " + M + "  "; if (B != 0) s += "BEA " + B + "  ";
            if (Heal != 0) s += "Heals " + Heal + "  "; if (Mana != 0) s += "Mana " + Mana + "  ";
            return s.Trim();
        }
    }

    public static class Items
    {
        public static readonly string[] RarityNames = { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };
        public static readonly int[] RarityHex = { 0xc8c8c8, 0x62d36a, 0x4aa3ff, 0xb06aff, 0xffc040, 0xff5a5a };
        static readonly Dictionary<string, ItemDef> all = new Dictionary<string, ItemDef>();
        public static readonly List<ItemDef> List = new List<ItemDef>();
        public static ItemDef Get(string id) { ItemDef d; return id != null && all.TryGetValue(id, out d) ? d : null; }
        public static string Name(string id) { ItemDef d = Get(id); return d == null ? id : d.Name; }

        static void Add(ItemDef d) { all[d.Id] = d; List.Add(d); }
        static void Mat(string id, string name, int rar, int val, string desc) { Add(new ItemDef { Id = id, Name = name, Kind = ItemKind.Material, Rarity = rar, Value = val, Desc = desc }); }
        static void Pot(string id, string name, int rar, int heal, int mana, string desc) { Add(new ItemDef { Id = id, Name = name, Kind = ItemKind.Potion, Rarity = rar, Heal = heal, Mana = mana, Value = (heal + mana) / 4, Desc = desc }); }
        static void Wep(string id, string name, WeaponType wt, int rar, int req, int s, int m, int t, int h, string desc) { Add(new ItemDef { Id = id, Name = name, Kind = ItemKind.Weapon, Wt = wt, Rarity = rar, Req = req, S = s, M = m, T = t, H = h, Value = (s + m) * 12, Desc = desc }); }
        static void Arm(string id, string name, int rar, int req, int t, int h, int m, string desc) { Add(new ItemDef { Id = id, Name = name, Kind = ItemKind.Armor, Rarity = rar, Req = req, T = t, H = h, M = m, Value = (t + h) * 10, Desc = desc }); }
        static void Acc(string id, string name, int rar, int req, int s, int t, int m, int b, string desc) { Add(new ItemDef { Id = id, Name = name, Kind = ItemKind.Accessory, Rarity = rar, Req = req, S = s, T = t, M = m, B = b, Value = 80, Desc = desc }); }

        public static readonly string[] ChestGrades = { "Wood", "Dark Iron", "Bronze", "Silver", "Gold", "Diamond", "Emerald", "Sapphire", "Ruby", "Mythril", "Celestial" };
        public static readonly int[] ChestHex = { 0x9b7448, 0x4a4a55, 0xcd7f32, 0xd0d4da, 0xffc940, 0x9fe8ff, 0x3ddc84, 0x3a6bff, 0xe0245e, 0x7fffd4, 0xfff7c2 };

        static Items()
        {
            Mat("wood", "Timber", 0, 3, "Sturdy wood for tools and weapons.");
            Mat("iron_ore", "Iron Ore", 0, 6, "Raw ore for the forge.");
            Mat("crystal_shard", "Crystal Shard", 2, 25, "A humming shard of crystal.");
            Mat("herb_red", "Redleaf", 0, 4, "A healing herb. Brew at the alchemist.");
            Mat("herb_blue", "Bluebell", 0, 4, "A mana herb. Brew at the alchemist.");
            Mat("herb_green", "Greenmoss", 0, 5, "A binding herb that strengthens potions.");
            Mat("hide", "Tough Hide", 0, 7, "Hide from wild beasts.");
            Mat("bone", "Sharp Bone", 0, 5, "A hard bone fragment.");
            Mat("stone_heart", "Stone Heart", 2, 40, "The core of a golem.");
            Mat("ecto", "Wraith Essence", 2, 45, "Cold essence of a wraith.");
            Mat("spirit_dust", "Spirit Dust", 3, 70, "Shimmering dust used in high potions.");
            Mat("ember_core", "Ember Core", 3, 90, "A glowing drakeling core.");
            Pot("hp_s", "Minor Healing Potion", 0, 60, 0, "Restores 60 HP.");
            Pot("hp_m", "Healing Potion", 1, 150, 0, "Restores 150 HP.");
            Pot("hp_l", "Greater Healing Potion", 2, 400, 0, "Restores 400 HP.");
            Pot("mp_s", "Minor Mana Potion", 0, 0, 40, "Restores 40 mana.");
            Pot("mp_m", "Mana Potion", 1, 0, 100, "Restores 100 mana.");
            Pot("elixir", "Soul Elixir", 3, 300, 150, "Restores 300 HP and 150 mana.");
            Wep("wood_sword", "Training Sword", WeaponType.Sword, 0, 0, 2, 0, 0, 0, "A plain wooden blade.");
            Wep("iron_sword", "Iron Sword", WeaponType.Sword, 1, 3, 6, 0, 0, 0, "A reliable blade.");
            Wep("steel_sword", "Steel Sword", WeaponType.Sword, 2, 8, 11, 0, 1, 0, "Folded steel.");
            Wep("crystal_blade", "Crystal Blade", WeaponType.Sword, 3, 12, 15, 6, 0, 0, "Hums with latent power.");
            Wep("greatsword_iron", "Iron Greatsword", WeaponType.Greatsword, 1, 6, 12, 0, 0, 0, "Slow, heavy, brutal.");
            Wep("oak_staff", "Oak Staff", WeaponType.Staff, 0, 0, 0, 5, 0, 0, "A simple casting staff.");
            Wep("arcane_staff", "Arcane Staff", WeaponType.Staff, 2, 8, 2, 12, 0, 0, "Crowned with a crystal.");
            Wep("hunter_bow", "Hunter Bow", WeaponType.Bow, 0, 0, 4, 0, 0, 0, "A light hunting bow.");
            Wep("longbow", "Longbow", WeaponType.Bow, 2, 6, 9, 0, 0, 0, "Long reach, steady aim.");
            Wep("iron_spear", "Iron Spear", WeaponType.Spear, 1, 3, 7, 0, 0, 0, "Reach and speed.");
            Wep("moon_spear", "Moonlit Spear", WeaponType.Spear, 3, 10, 13, 3, 0, 0, "Pale and quick.");
            Arm("cloth_robe", "Traveller's Robe", 0, 0, 2, 0, 2, "Light cloth.");
            Arm("leather_armor", "Leather Armor", 1, 0, 5, 20, 0, "Flexible protection.");
            Arm("iron_armor", "Iron Armor", 2, 5, 9, 50, 0, "Solid plates.");
            Arm("knight_plate", "Knight Plate", 3, 12, 15, 110, 0, "Heavy knightly armor.");
            Arm("mythril_mail", "Mythril Mail", 4, 20, 22, 200, 4, "Light as silk, hard as steel.");
            Acc("lucky_charm", "Lucky Charm", 1, 0, 0, 0, 2, 3, "A small charm that makes you look good.");
            Acc("ember_ring", "Ember Ring", 2, 5, 4, 0, 0, 0, "Warm to the touch.");
            Acc("frost_pendant", "Frost Pendant", 2, 5, 0, 4, 0, 0, "Cool and calming.");
            Acc("sun_crest", "Sun Crest", 3, 12, 6, 5, 6, 2, "A golden crest.");
            Add(new ItemDef { Id = "kingdom_token", Name = "Kingdom Token", Kind = ItemKind.Token, Rarity = 3, Desc = "Opens the portal to the Kingdom." });
            Add(new ItemDef { Id = "levelup_pill", Name = "Level-Up Pill", Kind = ItemKind.Pill, Rarity = 2, Desc = "Raises your level by 1. Works from Level 1 to 10 only." });
            for (int i = 0; i < ChestGrades.Length; i++)
                Add(new ItemDef { Id = "chest_" + i, Name = ChestGrades[i] + " Chest", Kind = ItemKind.Chest, Rarity = Mathf.Min(5, i / 2), Desc = "A sealed " + ChestGrades[i] + " chest. Open it for rewards." });
        }
    }

    public enum SkillKind { Melee, Projectile, Nova, Dash, Heal, Shield }

    public sealed class SkillDef
    {
        public string Id, Name, Desc; public int Element; public SkillKind Kind; public int Mp; public float Cd, Mult, Range, Radius; public int Level; public bool Crater;
    }

    public static class Skills
    {
        public static readonly List<SkillDef> All = new List<SkillDef>();
        static readonly Dictionary<string, SkillDef> map = new Dictionary<string, SkillDef>();
        public static SkillDef Get(string id) { SkillDef d; return id != null && map.TryGetValue(id, out d) ? d : null; }
        public const int MaxLoadout = 10;

        static void A(SkillDef d) { All.Add(d); map[d.Id] = d; }

        static readonly string[][] ElemNames =
        {
            new[] { "Tidal Lance", "Wave Crash", "Riptide Step", "Mending Spring" },
            new[] { "Fire Bolt", "Inferno Burst", "Blaze Rush", "Phoenix Ward" },
            new[] { "Stone Shard", "Quake", "Boulder Charge", "Earthen Hide" },
            new[] { "Void Bolt", "Singularity", "Rift Step", "Dark Barrier" },
            new[] { "Thunder Spear", "Storm Nova", "Lightning Dash", "Static Shield" },
            new[] { "Thorn Shot", "Bloom Burst", "Vine Leap", "Verdant Heal" },
            new[] { "Light Ray", "Radiant Nova", "Dawn Step", "Holy Mend" },
            new[] { "Gale Cutter", "Cyclone", "Wind Rush", "Zephyr Guard" },
            new[] { "Ice Shard", "Frost Nova", "Glacier Slide", "Frozen Armor" },
            new[] { "Prism Bolt", "Harmony Nova", "Astral Step", "Divine Grace" }
        };

        static Skills()
        {
            A(new SkillDef { Id = "power_strike", Name = "Power Strike", Element = -1, Kind = SkillKind.Melee, Mp = 8, Cd = 4f, Mult = 2.0f, Range = 3.2f, Radius = 60f, Level = 1, Desc = "A heavy strike in front of you." });
            A(new SkillDef { Id = "whirlwind", Name = "Whirlwind", Element = -1, Kind = SkillKind.Nova, Mp = 14, Cd = 7f, Mult = 1.4f, Range = 4f, Radius = 4f, Level = 2, Desc = "Spin and hit everything around you." });
            A(new SkillDef { Id = "guardian", Name = "Guardian Stance", Element = -1, Kind = SkillKind.Shield, Mp = 12, Cd = 14f, Mult = 0.3f, Range = 0f, Radius = 0f, Level = 3, Desc = "Absorb damage for a few seconds." });
            A(new SkillDef { Id = "leap_slam", Name = "Leap Slam", Element = -1, Kind = SkillKind.Dash, Mp = 18, Cd = 9f, Mult = 1.8f, Range = 7f, Radius = 3.5f, Level = 4, Crater = true, Desc = "Leap forward and slam the ground." });
            A(new SkillDef { Id = "second_wind", Name = "Second Wind", Element = -1, Kind = SkillKind.Heal, Mp = 20, Cd = 18f, Mult = 0.3f, Range = 8f, Radius = 8f, Level = 5, Desc = "Restore 30% HP to you and nearby allies." });
            A(new SkillDef { Id = "focus_burst", Name = "Focus Burst", Element = -1, Kind = SkillKind.Projectile, Mp = 16, Cd = 6f, Mult = 1.9f, Range = 16f, Radius = 1.5f, Level = 6, Desc = "A burst of focused energy." });
            for (int e = 0; e < 10; e++)
            {
                string pre = ElementInfo.Names[e].ToLower();
                A(new SkillDef { Id = pre + "_1", Name = ElemNames[e][0], Element = e, Kind = SkillKind.Projectile, Mp = 12, Cd = 3f, Mult = 1.6f, Range = 18f, Radius = 1.5f, Level = 3, Desc = "A bolt of " + ElementInfo.Names[e] + " energy." });
                A(new SkillDef { Id = pre + "_2", Name = ElemNames[e][1], Element = e, Kind = SkillKind.Nova, Mp = 24, Cd = 9f, Mult = 2.2f, Range = 6f, Radius = 6f, Level = 5, Crater = (e == 1 || e == 2 || e == 4), Desc = "A burst of " + ElementInfo.Names[e] + " around the target area." });
                A(new SkillDef { Id = pre + "_3", Name = ElemNames[e][2], Element = e, Kind = SkillKind.Dash, Mp = 18, Cd = 8f, Mult = 1.5f, Range = 9f, Radius = 3f, Level = 7, Desc = "Dash forward in a flash of " + ElementInfo.Names[e] + "." });
                A(new SkillDef { Id = pre + "_4", Name = ElemNames[e][3], Element = e, Kind = e == 2 || e == 3 || e == 4 || e == 7 || e == 8 ? SkillKind.Shield : SkillKind.Heal, Mp = 28, Cd = 16f, Mult = 0.35f, Range = 8f, Radius = 8f, Level = 9, Desc = "Protect yourself and allies with " + ElementInfo.Names[e] + "." });
            }
        }

        public static List<SkillDef> Unlocked(int level, int element)
        {
            List<SkillDef> r = new List<SkillDef>();
            for (int i = 0; i < All.Count; i++)
            {
                SkillDef s = All[i];
                if (s.Level > level) continue;
                if (s.Element == -1 || s.Element == element) r.Add(s);
            }
            return r;
        }
    }

    public sealed class LootEntry { public string Item; public float Chance; public int Min, Max; public LootEntry(string i, float c, int mn, int mx) { Item = i; Chance = c; Min = mn; Max = mx; } }

    public sealed class MonsterDef
    {
        public string Id, Name, Plan; public int Level, Hp, Dmg, XpReward, GoldMin, GoldMax; public float Speed, Aggro, Range, AtkCd, Scale, DropChance; public bool Ranged, Flying, Boss;
        public int Weak = -1, Resist = -1; public string Notes = "";
        public int ColA, ColB, ColC; public List<LootEntry> Loot = new List<LootEntry>();
    }

    public static class Monsters
    {
        public static readonly List<MonsterDef> All = new List<MonsterDef>();
        static readonly Dictionary<string, MonsterDef> map = new Dictionary<string, MonsterDef>();
        public static MonsterDef Get(string id) { MonsterDef d; return id != null && map.TryGetValue(id, out d) ? d : null; }
        public static int IndexOf(string id) { for (int i = 0; i < All.Count; i++) if (All[i].Id == id) return i; return 0; }

        static MonsterDef M(string id, string name, string plan, int lv, int hp, int dmg, float spd, float aggro, float range, float cd, int xp, int g0, int g1, float drop, float scale, int ca, int cb, int cc, bool ranged = false, bool flying = false, bool boss = false)
        {
            MonsterDef d = new MonsterDef { Id = id, Name = name, Plan = plan, Level = lv, Hp = hp, Dmg = dmg, Speed = spd, Aggro = aggro, Range = range, AtkCd = cd, XpReward = xp, GoldMin = g0, GoldMax = g1, DropChance = drop, Scale = scale, ColA = ca, ColB = cb, ColC = cc, Ranged = ranged, Flying = flying, Boss = boss };
            All.Add(d); map[id] = d; return d;
        }

        static void Info(string id, int weak, int resist, string notes) { MonsterDef d = Get(id); if (d != null) { d.Weak = weak; d.Resist = resist; d.Notes = notes; } }
        public static float ElementMult(MonsterDef d, int element)
        {
            if (element < 0 || d == null) return 1f;
            if (element == 9) return 1.15f;
            if (element == d.Weak) return 1.4f;
            if (element == d.Resist) return 0.7f;
            return 1f;
        }

        static Monsters()
        {
            M("gloop", "Mossy Gloop", "blob", 1, 40, 5, 2.6f, 9f, 1.8f, 1.6f, 8, 1, 3, 0.55f, 0.9f, 0x5fb85a, 0x9be08a, 0x2f6b34).Loot.AddRange(new[] { new LootEntry("herb_red", 0.45f, 1, 2), new LootEntry("herb_green", 0.2f, 1, 1), new LootEntry("wood", 0.15f, 1, 1) });
            M("fang", "Fangrunner", "quad", 3, 90, 10, 5.5f, 14f, 2.0f, 1.2f, 18, 2, 6, 0.5f, 1.0f, 0x8a8f99, 0xd9dde6, 0x3b3f48).Loot.AddRange(new[] { new LootEntry("hide", 0.55f, 1, 2), new LootEntry("bone", 0.3f, 1, 2), new LootEntry("herb_red", 0.12f, 1, 1) });
            M("spitter", "Venom Wisp", "float", 4, 70, 12, 3.0f, 15f, 12f, 2.0f, 22, 3, 8, 0.5f, 0.9f, 0x6a3aa8, 0xc58bff, 0x2a1646, true, true).Loot.AddRange(new[] { new LootEntry("herb_blue", 0.45f, 1, 2), new LootEntry("crystal_shard", 0.15f, 1, 1) });
            M("golem", "Stone Golem", "heavy", 6, 260, 22, 2.4f, 10f, 2.6f, 2.0f, 55, 6, 14, 0.6f, 1.5f, 0x7c7a74, 0xb8b4aa, 0x4a8cff).Loot.AddRange(new[] { new LootEntry("iron_ore", 0.65f, 1, 3), new LootEntry("stone_heart", 0.15f, 1, 1) });
            M("wraith", "Grave Wraith", "float", 10, 220, 28, 4.5f, 16f, 2.2f, 1.4f, 90, 10, 22, 0.5f, 1.2f, 0x3a4a6a, 0x9ad0ff, 0x101828, false, true).Loot.AddRange(new[] { new LootEntry("ecto", 0.45f, 1, 2), new LootEntry("spirit_dust", 0.12f, 1, 1) });
            M("brute", "Ashen Brute", "heavy", 12, 600, 45, 3.2f, 12f, 3.0f, 2.2f, 140, 14, 30, 0.6f, 1.7f, 0x8a5a44, 0xd8a070, 0xff6a20).Loot.AddRange(new[] { new LootEntry("hide", 0.7f, 2, 4), new LootEntry("iron_ore", 0.5f, 2, 4), new LootEntry("stone_heart", 0.2f, 1, 1) });
            M("harpy", "Gale Harpy", "flyer", 14, 420, 38, 5.0f, 18f, 2.4f, 1.5f, 170, 16, 34, 0.5f, 1.3f, 0x9a6a3a, 0xf0d8a0, 0x2a8a8a, false, true).Loot.AddRange(new[] { new LootEntry("bone", 0.6f, 1, 3), new LootEntry("spirit_dust", 0.15f, 1, 1) });
            M("drakeling", "Ember Drakeling", "drake", 18, 900, 60, 4.5f, 20f, 3.0f, 1.8f, 300, 30, 70, 0.55f, 1.8f, 0xb03a2a, 0xff9a40, 0x2a1410).Loot.AddRange(new[] { new LootEntry("ember_core", 0.35f, 1, 1), new LootEntry("crystal_shard", 0.3f, 1, 2), new LootEntry("hide", 0.5f, 2, 4) });
            M("warden", "Ancient Warden", "heavy", 10, 2400, 55, 3.0f, 22f, 3.6f, 2.0f, 600, 100, 160, 1f, 3.0f, 0x4a4650, 0xb8a8ff, 0x8a6bff, false, false, true).Loot.AddRange(new[] { new LootEntry("kingdom_token", 1f, 1, 1), new LootEntry("chest_3", 1f, 1, 1), new LootEntry("stone_heart", 0.8f, 1, 2) });
            Info("gloop", 1, 0, "Hops and bounces into its target. Slow, harmless alone but they come in groups. Weak to Fire.");
            Info("fang", 4, 7, "Fast pack hunter that circles and bites. Dodge when it lunges. Weak to Thunder.");
            Info("spitter", 2, 5, "Floats out of melee range and spits venom bolts from afar. Close in quickly. Weak to Earth.");
            Info("golem", 0, 2, "Slow and heavy. Slams with huge arms; very high health. Weak to Water, shrugs off Earth.");
            Info("wraith", 6, 3, "Fast ghost that drifts through the dark and slashes. Weak to Light, immune-like to Void.");
            Info("brute", 8, 1, "Ashen ogre with crushing blows and heavy armour. Weak to Ice, resists Fire.");
            Info("harpy", 2, 7, "Dives and rakes from the air; hard to hit while flying. Weak to Earth, resists Air.");
            Info("drakeling", 0, 1, "Young dragon with claws and a breath-like rush. Weak to Water, resists Fire.");
            Info("warden", 4, 2, "BOSS. Heavy melee combos plus a ground slam every few seconds (watch the red circle). Enrages below 40% health. Weak to Thunder.");
        }
    }

    public sealed class Recipe { public string Id, Result, Station; public int Qty = 1, Req, Gold; public string[] Mats; public int[] Counts; }

    public static class Recipes
    {
        public static readonly List<Recipe> All = new List<Recipe>();
        static void R(string station, string result, int req, int gold, params object[] mats)
        {
            Recipe r = new Recipe { Id = station + ":" + result, Result = result, Station = station, Req = req, Gold = gold };
            int n = mats.Length / 2; r.Mats = new string[n]; r.Counts = new int[n];
            for (int i = 0; i < n; i++) { r.Mats[i] = (string)mats[i * 2]; r.Counts[i] = (int)mats[i * 2 + 1]; }
            All.Add(r);
        }
        public static Recipe Get(string id) { for (int i = 0; i < All.Count; i++) if (All[i].Id == id) return All[i]; return null; }

        static Recipes()
        {
            R("smith", "hunter_bow", 0, 0, "wood", 4, "hide", 2);
            R("smith", "iron_sword", 3, 10, "iron_ore", 3, "wood", 2);
            R("smith", "iron_spear", 3, 10, "iron_ore", 3, "wood", 3);
            R("smith", "leather_armor", 0, 0, "hide", 4, "wood", 2);
            R("smith", "iron_armor", 5, 30, "iron_ore", 6, "hide", 4);
            R("smith", "greatsword_iron", 6, 40, "iron_ore", 8, "wood", 3);
            R("smith", "steel_sword", 8, 80, "iron_ore", 5, "hide", 2, "bone", 2);
            R("smith", "arcane_staff", 8, 80, "wood", 4, "crystal_shard", 3);
            R("smith", "crystal_blade", 12, 200, "crystal_shard", 3, "iron_ore", 4);
            R("smith", "knight_plate", 12, 250, "iron_ore", 10, "hide", 6, "stone_heart", 2);
            R("alchemist", "hp_s", 0, 0, "herb_red", 2);
            R("alchemist", "mp_s", 0, 0, "herb_blue", 2);
            R("alchemist", "hp_m", 5, 5, "herb_red", 3, "herb_green", 1);
            R("alchemist", "mp_m", 5, 5, "herb_blue", 3, "herb_green", 1);
            R("alchemist", "elixir", 10, 40, "herb_red", 4, "herb_blue", 4, "crystal_shard", 1);
            R("alchemist", "hp_l", 15, 60, "herb_red", 5, "herb_green", 2, "spirit_dust", 1);
        }
    }

    public static class Loot
    {
        /// <summary>Roll a monster's drops. Not every kill drops loot; higher level means better odds and amounts.</summary>
        public static List<KeyValuePair<string, int>> Roll(MonsterDef m, DetRng rng)
        {
            List<KeyValuePair<string, int>> r = new List<KeyValuePair<string, int>>();
            float baseChance = Mathf.Clamp01(m.DropChance + m.Level * 0.004f);
            if (!m.Boss && rng.Next() > baseChance) return r;
            for (int i = 0; i < m.Loot.Count; i++)
            {
                LootEntry e = m.Loot[i];
                if (rng.Next() <= e.Chance) r.Add(new KeyValuePair<string, int>(e.Item, rng.Int(e.Min, e.Max + 1)));
            }
            if (!m.Boss && r.Count == 0 && rng.Next() < 0.35f) r.Add(new KeyValuePair<string, int>(m.Loot[0].Item, 1));
            if (m.Level >= 8 && rng.Next() < 0.015f + m.Level * 0.001f) r.Add(new KeyValuePair<string, int>("chest_" + Mathf.Clamp(m.Level / 8, 0, 4), 1));
            return r;
        }

        static readonly string[][] ChestItems =
        {
            new[] { "herb_red", "herb_blue", "wood", "hp_s", "mp_s" },
            new[] { "iron_ore", "hide", "bone", "herb_green", "hp_s", "mp_s" },
            new[] { "iron_ore", "hide", "hp_m", "mp_m", "wood", "crystal_shard", "lucky_charm" },
            new[] { "hp_m", "mp_m", "crystal_shard", "iron_sword", "iron_armor", "ember_ring", "frost_pendant", "iron_spear" },
            new[] { "hp_l", "elixir", "steel_sword", "longbow", "arcane_staff", "iron_armor", "spirit_dust", "sun_crest" },
            new[] { "elixir", "hp_l", "crystal_blade", "knight_plate", "moon_spear", "sun_crest", "spirit_dust", "ember_core" },
            new[] { "crystal_blade", "knight_plate", "moon_spear", "sun_crest", "ember_core", "spirit_dust", "hp_l" },
            new[] { "knight_plate", "moon_spear", "sun_crest", "ember_core", "elixir", "spirit_dust" },
            new[] { "mythril_mail", "moon_spear", "sun_crest", "ember_core", "elixir" },
            new[] { "mythril_mail", "crystal_blade", "sun_crest", "elixir", "ember_core" },
            new[] { "mythril_mail", "sun_crest", "elixir", "ember_core", "spirit_dust" }
        };

        /// <summary>Graded chests: low grades are often modest or nearly empty, high grades give real gear.</summary>
        public static List<KeyValuePair<string, int>> RollChest(int grade, DetRng rng, out int gold)
        {
            grade = Mathf.Clamp(grade, 0, 10);
            List<KeyValuePair<string, int>> r = new List<KeyValuePair<string, int>>();
            gold = rng.Int(2 + grade * 8, 10 + grade * 30);
            if (rng.Next() < Mathf.Max(0.04f, 0.3f - grade * 0.04f)) { gold /= 3; return r; }
            int rolls = 1 + grade / 3 + (rng.Next() < 0.4f ? 1 : 0);
            string[] pool = ChestItems[grade];
            for (int i = 0; i < rolls; i++)
            {
                string id = pool[rng.Int(0, pool.Length)];
                ItemDef d = Items.Get(id);
                int n = d != null && d.Kind == ItemKind.Material || d != null && d.Kind == ItemKind.Potion ? rng.Int(1, 2 + grade / 2) : 1;
                r.Add(new KeyValuePair<string, int>(id, n));
            }
            return r;
        }
    }
}
