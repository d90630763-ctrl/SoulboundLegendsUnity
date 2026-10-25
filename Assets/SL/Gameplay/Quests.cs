using System;
using System.Collections.Generic;
using UnityEngine;

namespace SL.Gameplay
{
    public enum ObjType { Talk, Kill, Gather, Craft, Reach, Chest, Emote, Visit, Harvest, Choose }

    public sealed class Objective { public ObjType Type; public string Target; public int Count; public string Text; }

    public sealed class QuestDef
    {
        public string Id, Title, Giver, Desc, Prereq; public int MinLevel, Xp, Gold, Zone;
        public bool Hidden, Promotion, Consume;
        public List<Objective> Objs = new List<Objective>();
        public List<ItemStack> Rewards = new List<ItemStack>();
    }

    public static class Quests
    {
        public static readonly List<QuestDef> All = new List<QuestDef>();
        static readonly Dictionary<string, QuestDef> map = new Dictionary<string, QuestDef>();
        public static QuestDef Get(string id) { QuestDef q; return id != null && map.TryGetValue(id, out q) ? q : null; }

        static Objective O(ObjType t, string target, int n, string text) { return new Objective { Type = t, Target = target, Count = n, Text = text }; }
        static ItemStack S(string id, int n) { return new ItemStack { id = id, n = n }; }

        static QuestDef Q(string id, string title, string giver, string desc, int minLv, string prereq, int xp, int gold, Objective[] objs, params ItemStack[] rewards)
        {
            QuestDef q = new QuestDef { Id = id, Title = title, Giver = giver, Desc = desc, MinLevel = minLv, Prereq = prereq, Xp = xp, Gold = gold };
            q.Objs.AddRange(objs); q.Rewards.AddRange(rewards);
            All.Add(q); map[id] = q; return q;
        }

        static QuestDef Hid(string id, string title, string desc, int minLv, int xp, int gold, Objective[] objs, params ItemStack[] rewards)
        {
            QuestDef q = Q(id, title, "hidden", desc, minLv, null, xp, gold, objs, rewards);
            q.Hidden = true; return q;
        }

        static Quests()
        {
            // ----- Dawnrest Village -----
            Q("q_intro", "Welcome to Dawnrest", "elder", "Elder Maren asks you to introduce yourself to Borin the Smith.", 0, null, 25, 5, new[] { O(ObjType.Talk, "smith", 1, "Speak to Borin the Smith") }, S("hp_s", 2));
            Q("q_gloop", "Gloop Trouble", "elder", "Mossy Gloops are creeping in from the outskirts. Thin them out.", 0, "q_intro", 45, 10, new[] { O(ObjType.Kill, "gloop", 5, "Defeat Mossy Gloops") }, S("hp_s", 2));
            Q("q_herbs", "Herbs for Sylva", "alchemist", "Sylva needs fresh herbs. Collect them around the village edges.", 0, "q_intro", 40, 8, new[] { O(ObjType.Gather, "herb_red", 5, "Collect Redleaf"), O(ObjType.Gather, "herb_blue", 3, "Collect Bluebell") }, S("mp_s", 2)).Consume = true;
            Q("q_brew", "First Brew", "alchemist", "Brew your first potions at the alchemist.", 0, "q_herbs", 35, 5, new[] { O(ObjType.Craft, "hp_s", 2, "Brew Minor Healing Potions") }, S("herb_red", 3));
            Q("q_forge", "Metal for Borin", "smith", "Borin needs raw materials for the forge.", 1, "q_intro", 50, 10, new[] { O(ObjType.Gather, "wood", 4, "Collect Timber"), O(ObjType.Gather, "iron_ore", 3, "Collect Iron Ore") }, S("hide", 2)).Consume = false;
            Q("q_sword", "A Proper Blade", "smith", "Forge an Iron Sword at the smith.", 3, "q_forge", 70, 20, new[] { O(ObjType.Craft, "iron_sword", 1, "Forge an Iron Sword") }, S("ember_ring", 1));
            Q("q_awaken", "Awaken the Inheritance", "elder", "At Level 3 the gods stir within you. Speak to Elder Maren and choose your divine inheritance.", 3, "q_gloop", 90, 0, new[] { O(ObjType.Reach, "3", 3, "Reach Level 3"), O(ObjType.Choose, "element", 1, "Choose your inheritance") }, S("chest_0", 1));
            Q("q_wolves", "The Wolf Pack", "elder", "Fangrunners have been stalking travellers. Hunt them.", 3, "q_awaken", 110, 20, new[] { O(ObjType.Kill, "fang", 6, "Defeat Fangrunners") }, S("chest_1", 1), S("hp_s", 3));
            Q("q_patrol", "Across the Bridge", "guard", "Guard Roan wants you to scout across the river bridge.", 2, "q_intro", 50, 10, new[] { O(ObjType.Visit, "bridge", 1, "Cross the river bridge") }, S("mp_s", 2));
            Q("q_spitters", "Venom in the Air", "guard", "Venom Wisps are harassing the road. Put them down.", 4, "q_patrol", 140, 25, new[] { O(ObjType.Kill, "spitter", 4, "Defeat Venom Wisps") }, S("hp_m", 2));
            Q("q_lore", "Tavern Gossip", "innkeeper", "Tamsin wants you to meet the village: speak to the Elder, the Smith and the Alchemist.", 0, null, 40, 8, new[] { O(ObjType.Talk, "elder", 1, "Speak to Elder Maren"), O(ObjType.Talk, "smith", 1, "Speak to Borin"), O(ObjType.Talk, "alchemist", 1, "Speak to Sylva") }, S("hp_s", 2));
            Q("q_chests", "Hidden Caches", "innkeeper", "Travellers hid caches around the village. Open three.", 2, "q_lore", 70, 15, new[] { O(ObjType.Chest, "any", 3, "Open chests") }, S("lucky_charm", 1));
            Q("q_golems", "Stone Guardians", "elder", "Stone Golems patrol the hills. They guard something. Defeat two.", 6, "q_wolves", 220, 40, new[] { O(ObjType.Kill, "golem", 2, "Defeat Stone Golems"), O(ObjType.Gather, "iron_ore", 3, "Carry Iron Ore") }, S("iron_armor", 1), S("chest_2", 1));
            Q("q_ascension", "Trial of Ascension", "elder", "At Level 10, face the Ancient Warden in the ruins to the north-west. Prove yourself and earn a Kingdom Token.", 10, "q_golems", 500, 100, new[] { O(ObjType.Reach, "10", 10, "Reach Level 10"), O(ObjType.Kill, "warden", 1, "Defeat the Ancient Warden") }, S("kingdom_token", 1), S("chest_3", 1)).Promotion = true;

            Hid("h_hello", "A Friendly Face", "Wave at another adventurer.", 0, 40, 10, new[] { O(ObjType.Emote, "near_player", 1, "Use an emote near another player") }, S("lucky_charm", 1));
            Hid("h_forager", "The Forager", "You have a knack for herbs.", 0, 80, 15, new[] { O(ObjType.Harvest, "herb", 15, "Harvest herbs") }, S("chest_1", 1));
            Hid("h_ruins", "Echoes in the Ruins", "Something ancient calls from the north-west.", 0, 60, 10, new[] { O(ObjType.Visit, "ruins", 1, "Find the ancient ruins") }, S("chest_0", 1));

            // ----- Aurelion Kingdom (jobs) -----
            Q("k_welcome", "Welcome to Aurelion", "kguide", "Seneschal Valen welcomes new arrivals. Meet the Armorer and the Brewer.", 10, null, 300, 80, new[] { O(ObjType.Talk, "ksmith", 1, "Speak to Hale the Armorer"), O(ObjType.Talk, "kalch", 1, "Speak to Orla the Brewer") }, S("chest_2", 1));
            Q("k_armor", "Armor for the Guard", "ksmith", "Forge Iron Armor for the city guard.", 10, null, 250, 60, new[] { O(ObjType.Craft, "iron_armor", 1, "Forge Iron Armor") }, S("chest_2", 1));
            Q("k_potions", "Brewer's Order", "kalch", "Orla needs a batch of Healing Potions.", 10, null, 220, 50, new[] { O(ObjType.Craft, "hp_m", 3, "Brew Healing Potions") }, S("hp_m", 2));
            Q("k_cook", "Hearty Stew", "kcook", "Mabel needs herbs for the royal stew.", 10, null, 200, 40, new[] { O(ObjType.Gather, "herb_green", 6, "Bring Greenmoss"), O(ObjType.Gather, "herb_red", 4, "Bring Redleaf") }, S("hp_m", 3)).Consume = true;
            Q("k_fisher", "River Lore", "kfisher", "Pike wants someone to check the river bend.", 10, null, 150, 30, new[] { O(ObjType.Visit, "river", 1, "Reach the river") }, S("chest_2", 1));
            Q("k_farmer", "The Herb Harvest", "kfarmer", "Dorn's fields need herbs gathered from the wild.", 10, null, 220, 40, new[] { O(ObjType.Harvest, "herb", 10, "Harvest herbs") }, S("hp_m", 2));
            Q("k_hunter", "Beast Season", "khunter", "Kessa is tracking Fangrunners near the walls.", 10, null, 260, 50, new[] { O(ObjType.Kill, "fang", 8, "Defeat Fangrunners") }, S("hide", 4));
            Q("k_drake", "Drake Hunt", "khunter", "A pair of Ember Drakelings roost far out. Slay them.", 14, "k_hunter", 700, 150, new[] { O(ObjType.Kill, "drakeling", 2, "Defeat Ember Drakelings") }, S("chest_4", 1));
            Q("k_miner", "Ore for the Crown", "kminer", "Gruff needs iron ore for the royal forge.", 10, null, 240, 60, new[] { O(ObjType.Gather, "iron_ore", 10, "Bring Iron Ore") }, S("chest_2", 1)).Consume = true;
            Q("k_scribe", "Census", "kscribe", "Ilyan is counting the city's craftsmen. Meet four of them.", 10, null, 180, 30, new[] { O(ObjType.Talk, "kcook", 1, "Meet Mabel"), O(ObjType.Talk, "kfisher", 1, "Meet Pike"), O(ObjType.Talk, "kfarmer", 1, "Meet Dorn"), O(ObjType.Talk, "kbard", 1, "Meet Lyra") }, S("lucky_charm", 1));
            Q("k_merchant", "Leather Trade", "kmerchant", "Zara pays well for good hides.", 10, null, 150, 120, new[] { O(ObjType.Gather, "hide", 6, "Bring Tough Hide") }).Consume = true;
            Q("k_captain", "Wraith Patrol", "kcaptain", "Captain Marek needs the night patrol cleared of wraiths.", 10, null, 400, 80, new[] { O(ObjType.Kill, "wraith", 6, "Defeat Grave Wraiths") }, S("chest_3", 1));
            Q("k_brutes", "Ashen Menace", "kcaptain", "Ashen Brutes have been spotted near the walls.", 12, "k_captain", 520, 100, new[] { O(ObjType.Kill, "brute", 4, "Defeat Ashen Brutes") }, S("chest_3", 1), S("knight_plate", 1));
            Q("k_jeweler", "A Stone for the Queen", "kjeweler", "Opal needs crystal shards for a royal commission.", 10, null, 300, 90, new[] { O(ObjType.Gather, "crystal_shard", 3, "Bring Crystal Shards") }, S("sun_crest", 1)).Consume = true;
            Q("k_tailor", "Winter Coats", "ktailor", "Wilm needs materials for winter coats.", 10, null, 180, 40, new[] { O(ObjType.Gather, "hide", 5, "Bring Tough Hide"), O(ObjType.Gather, "bone", 4, "Bring Sharp Bone") }, S("cloth_robe", 1), S("lucky_charm", 1)).Consume = true;
            Q("k_bard", "A Song for Aurelion", "kbard", "Lyra wants an audience. Use an emote near another player.", 10, null, 120, 20, new[] { O(ObjType.Emote, "near_player", 1, "Use an emote near another player") }, S("chest_1", 1));
            Q("k_priest", "Winged Terrors", "kpriest", "Brother Aldous asks you to drive off Gale Harpies.", 12, null, 450, 90, new[] { O(ObjType.Kill, "harpy", 4, "Defeat Gale Harpies") }, S("hp_l", 2));
        }

        public static List<QuestDef> ForGiver(string npcId)
        {
            List<QuestDef> r = new List<QuestDef>();
            for (int i = 0; i < All.Count; i++) if (All[i].Giver == npcId) r.Add(All[i]);
            return r;
        }
    }

    public sealed class Milestone { public string Id, Title; public string Kind; public int Value; public int ChestGrade; public int Gold; public bool WorldFirst; }

    public static class Milestones
    {
        public static readonly List<Milestone> All = new List<Milestone>();
        static void M(string id, string title, string kind, int v, int chest, int gold, bool wf) { All.Add(new Milestone { Id = id, Title = title, Kind = kind, Value = v, ChestGrade = chest, Gold = gold, WorldFirst = wf }); }
        public static Milestone Get(string id) { for (int i = 0; i < All.Count; i++) if (All[i].Id == id) return All[i]; return null; }
        static Milestones()
        {
            M("first_kill", "First Blood", "kills", 1, 0, 10, false);
            M("kills_100", "Monster Hunter", "kills", 100, 2, 150, false);
            M("first_craft", "Apprentice Crafter", "crafted", 1, 0, 10, false);
            M("herbalist", "Herbalist", "herbs", 20, 1, 40, false);
            M("chests_10", "Treasure Seeker", "chests", 10, 2, 100, false);
            M("lv5", "Rising Adventurer", "level", 5, 1, 100, false);
            M("lv10", "Breakthrough", "level", 10, 3, 300, true);
            M("promoted", "Ascended", "promoted", 1, 4, 500, true);
            M("kingdom", "Citizen of Aurelion", "kingdom", 1, 3, 400, true);
            M("lv20", "Veteran", "level", 20, 5, 600, false);
            M("lv30", "Champion", "level", 30, 6, 900, false);
            M("lv40", "Hero", "level", 40, 7, 1200, false);
            M("lv50", "Legend", "level", 50, 8, 1800, false);
            M("lv70", "Mythic", "level", 70, 9, 3000, false);
            M("lv100", "Soulbound Legend", "level", 100, 10, 8000, true);
        }
    }
}
