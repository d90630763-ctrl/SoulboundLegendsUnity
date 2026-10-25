using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using SL.Core;
using SL.World;

namespace SL.Gameplay
{
    /// <summary>
    /// Builds the admin guidebook (quests, loot, equipment, monsters, bosses) straight from the game's own data tables,
    /// so the book can never drift out of date. Run from the Unity menu: Soulbound > Export Admin Compendium.
    /// </summary>
    public static class AdminCompendium
    {
        static readonly string[] Compass = { "east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east" };

        public static string Region(ZoneLayout L, Vector3 p)
        {
            float r = Mathf.Sqrt(p.x * p.x + p.z * p.z);
            float ang = Mathf.Atan2(p.z, p.x) * Mathf.Rad2Deg; if (ang < 0f) ang += 360f;
            string dir = Compass[Mathf.RoundToInt(ang / 45f) % 8];
            string ring = r < L.Def.PlazaR ? "town centre" : (r < 60f ? "inner fields" : (r < 90f ? "outskirts" : "far borderlands"));
            string river = p.z > L.Base.RiverCentre(p.x) + L.Def.RiverW ? ", across the river" : "";
            return ring + ", " + dir + river;
        }

        static string Items2(List<ItemStack> l) { string s = ""; for (int i = 0; i < l.Count; i++) s += (i > 0 ? ", " : "") + Items.Name(l[i].id) + " x" + l[i].n; return s.Length == 0 ? "-" : s; }
        static string El(int e) { return e < 0 ? "none" : ElementInfo.Names[e]; }

        public static string Build()
        {
            StringBuilder b = new StringBuilder();
            b.AppendLine("# SOULBOUND LEGENDS - ADMIN COMPENDIUM");
            b.AppendLine("Generated from the game's data tables. Coordinates are world metres (x = east, z = north) inside each zone; (0,0) is the plaza centre.");
            b.AppendLine();
            b.AppendLine("## 1. Progression overview");
            int total = 0; for (int l = 0; l < 10; l++) total += Rules.XpToNext(l);
            b.AppendLine("- Players start at Level 0 in Dawnrest Village (instanced; " + ZoneDef.Get(0).Capacity + " players per instance).");
            b.AppendLine("- XP needed to go from Lv0 to Lv10: " + total + ". Max level: " + Rules.MaxLevel + ".");
            b.AppendLine("- Level 3: awaken the elemental inheritance (Elder Maren). Level 10 + promotion quest 'Trial of Ascension' unlocks the Kingdom Token, the village portal to Aurelion Kingdom (" + ZoneDef.Get(1).Capacity + " players per instance, extra kingdoms open when full), top-up and modes.");
            b.AppendLine("- LV Up Pill: levels 1-9 only; becomes claimable once " + "5" + " players have completed the promotion.");
            b.AppendLine("- Skills: max " + Skills.MaxLoadout + " equipped at once; the HUD shows 8 per page.");
            b.AppendLine();

            // -------------------------------------------------------------- NPCs
            b.AppendLine("## 2. NPCs and what they give");
            for (int z = 0; z < 2; z++)
            {
                ZoneLayout L = ZoneLayout.Get(z);
                b.AppendLine("### " + L.Def.Name);
                b.AppendLine("| NPC | Id | Role | Location | Quests given |");
                b.AppendLine("|---|---|---|---|---|");
                for (int i = 0; i < L.Npcs.Count; i++)
                {
                    NpcDef n = L.Npcs[i]; List<QuestDef> qs = Quests.ForGiver(n.Id); string qn = "";
                    for (int k = 0; k < qs.Count; k++) qn += (k > 0 ? "; " : "") + qs[k].Title;
                    b.AppendLine("| " + n.Name + " | " + n.Id + " | " + n.Role + " | (" + Mathf.RoundToInt(n.Pos.x) + ", " + Mathf.RoundToInt(n.Pos.z) + ") " + Region(L, n.Pos) + " | " + (qn.Length == 0 ? "-" : qn) + " |");
                }
                b.AppendLine();
            }

            // -------------------------------------------------------------- quests
            b.AppendLine("## 3. All quests");
            b.AppendLine("| Quest | Id | Given by | Min Lv | Needs first | Objectives | Rewards |");
            b.AppendLine("|---|---|---|---|---|---|---|");
            for (int i = 0; i < Quests.All.Count; i++)
            {
                QuestDef q = Quests.All[i]; string objs = "";
                for (int k = 0; k < q.Objs.Count; k++) objs += (k > 0 ? "; " : "") + q.Objs[k].Text + " (" + q.Objs[k].Count + ")";
                string giver = q.Hidden ? "HIDDEN (discovered by doing)" : q.Giver;
                b.AppendLine("| " + q.Title + (q.Promotion ? " **(promotion)**" : "") + " | " + q.Id + " | " + giver + " | " + q.MinLevel + " | " + (string.IsNullOrEmpty(q.Prereq) ? "-" : q.Prereq) + " | " + objs + " | " + q.Xp + " XP, " + q.Gold + "g, " + Items2(q.Rewards) + " |");
            }
            b.AppendLine();
            b.AppendLine("Hidden quests have no giver: they are discovered by doing the thing (emote near another player, harvest many herbs, visit the ruins...). The player sees a toast when one is found.");
            b.AppendLine();

            // -------------------------------------------------------------- milestones
            b.AppendLine("## 4. Milestone / first-time rewards");
            b.AppendLine("| Milestone | Id | Condition | Chest | Gold | World-first bonus |");
            b.AppendLine("|---|---|---|---|---|---|");
            for (int i = 0; i < Milestones.All.Count; i++)
            {
                Milestone m = Milestones.All[i];
                b.AppendLine("| " + m.Title + " | " + m.Id + " | " + m.Kind + " >= " + m.Value + " | " + Items.ChestGrades[m.ChestGrade] + " | " + m.Gold + " | " + (m.WorldFirst ? "yes (+2 chest grades for the first player in the world)" : "no") + " |");
            }
            b.AppendLine();

            // -------------------------------------------------------------- monsters & bosses
            b.AppendLine("## 5. Monsters and bosses");
            b.AppendLine("| Monster | Lv | HP | Damage | Speed | Weak to | Resists | Ability notes | XP | Gold | Drops (chance when it drops) |");
            b.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
            for (int i = 0; i < Monsters.All.Count; i++)
            {
                MonsterDef d = Monsters.All[i]; string drops = "";
                for (int k = 0; k < d.Loot.Count; k++) drops += (k > 0 ? ", " : "") + Items.Name(d.Loot[k].Item) + " " + Mathf.RoundToInt(d.Loot[k].Chance * 100f) + "% (x" + d.Loot[k].Min + "-" + d.Loot[k].Max + ")";
                b.AppendLine("| " + d.Name + (d.Boss ? " **BOSS**" : "") + " | " + d.Level + " | " + d.Hp + " | " + d.Dmg + " | " + d.Speed.ToString("0.0") + " | " + El(d.Weak) + " | " + El(d.Resist) + " | " + d.Notes + " | " + d.XpReward + " | " + d.GoldMin + "-" + d.GoldMax + " | " + drops + " |");
            }
            b.AppendLine();
            b.AppendLine("Drops: a monster only drops loot some of the time (see section 6); each entry is then rolled separately. Higher-level monsters drop more often and better.");
            b.AppendLine();
            b.AppendLine("### Where each monster lives");
            for (int z = 0; z < 2; z++)
            {
                ZoneLayout L = ZoneLayout.Get(z);
                b.AppendLine("**" + L.Def.Name + "**");
                Dictionary<string, int> cnt = new Dictionary<string, int>(); Dictionary<string, string> where = new Dictionary<string, string>();
                for (int i = 0; i < L.Spawns.Count; i++) { string t = L.Spawns[i].Type; if (!cnt.ContainsKey(t)) { cnt[t] = 0; where[t] = Region(L, L.Spawns[i].Pos); } cnt[t]++; }
                if (z == 0) { cnt["warden"] = 1; where["warden"] = "Trial Grounds ruins at (" + Mathf.RoundToInt(L.BossPos.x) + ", " + Mathf.RoundToInt(L.BossPos.z) + "), " + Region(L, L.BossPos); }
                foreach (KeyValuePair<string, int> kv in cnt) { MonsterDef d = Monsters.Get(kv.Key); b.AppendLine("- " + (d == null ? kv.Key : d.Name) + ": " + kv.Value + " per instance, e.g. " + where[kv.Key]); }
                b.AppendLine();
            }
            b.AppendLine("**Area bosses:** Dawnrest Village - Ancient Warden (promotion boss, respawns after 4 minutes). Aurelion Kingdom - Ember Drakelings (elite) roam the far borderlands; a full kingdom boss is reserved for a later version.");
            b.AppendLine();

            // -------------------------------------------------------------- loot placement
            b.AppendLine("## 6. Loot: how drops work");
            DetRng rng = new DetRng(99);
            b.AppendLine("Chance that ANY loot drops when a monster dies (simulated 4000 kills each):");
            for (int i = 0; i < Monsters.All.Count; i++)
            {
                int hit = 0; for (int k = 0; k < 4000; k++) if (Loot.Roll(Monsters.All[i], rng).Count > 0) hit++;
                b.AppendLine("- " + Monsters.All[i].Name + " (Lv" + Monsters.All[i].Level + "): " + (hit * 100 / 4000) + "%");
            }
            b.AppendLine();
            b.AppendLine("## 7. Chests");
            b.AppendLine("Chest grades from lowest to highest: " + string.Join(" > ", Items.ChestGrades) + ". A chest is locked if the player is too low level (level + 6 must be at least grade x 4). Opened chests refill after 6 minutes in that instance.");
            b.AppendLine();
            b.AppendLine("### Chest contents by grade (simulated 3000 openings each)");
            b.AppendLine("| Grade | Average gold | Items per chest | Items that can appear (% of chests) |");
            b.AppendLine("|---|---|---|---|");
            for (int g = 0; g < Items.ChestGrades.Length; g++)
            {
                Dictionary<string, int> tally = new Dictionary<string, int>(); long gold = 0; long items = 0; int N = 3000;
                for (int k = 0; k < N; k++) { int gg; List<KeyValuePair<string, int>> r = Loot.RollChest(g, rng, out gg); gold += gg; items += r.Count; HashSet<string> seen = new HashSet<string>(); for (int j = 0; j < r.Count; j++) if (seen.Add(r[j].Key)) { int c; tally.TryGetValue(r[j].Key, out c); tally[r[j].Key] = c + 1; } }
                List<KeyValuePair<string, int>> sorted = new List<KeyValuePair<string, int>>(tally); sorted.Sort((x, y) => y.Value.CompareTo(x.Value));
                string list = ""; for (int j = 0; j < sorted.Count && j < 14; j++) list += (j > 0 ? ", " : "") + Items.Name(sorted[j].Key) + " " + (sorted[j].Value * 100 / N) + "%";
                b.AppendLine("| " + Items.ChestGrades[g] + " | " + (gold / N) + " | " + (items * 10 / N / 10f).ToString("0.0") + " | " + list + " |");
            }
            b.AppendLine();
            for (int z = 0; z < 2; z++)
            {
                ZoneLayout L = ZoneLayout.Get(z);
                b.AppendLine("### Chest locations - " + L.Def.Name + " (" + L.Chests.Count + " chests)");
                b.AppendLine("| Id | Grade | Position | Area | Min level to open |");
                b.AppendLine("|---|---|---|---|---|");
                for (int i = 0; i < L.Chests.Count; i++) { ChestDef c = L.Chests[i]; b.AppendLine("| " + c.Id + " | " + Items.ChestGrades[c.Grade] + " | (" + Mathf.RoundToInt(c.Pos.x) + ", " + Mathf.RoundToInt(c.Pos.z) + ") | " + Region(L, c.Pos) + " | " + Mathf.Max(0, c.Grade * 4 - 6) + " |"); }
                b.AppendLine();
            }
            b.AppendLine("## 8. Gathering nodes (herbs, ore, crystal, wood)");
            for (int z = 0; z < 2; z++)
            {
                ZoneLayout L = ZoneLayout.Get(z);
                b.AppendLine("### " + L.Def.Name);
                NodeKind[] ks = { NodeKind.Herb, NodeKind.Tree, NodeKind.Ore, NodeKind.Crystal };
                string[] yields = { "red/blue/green herbs (potions)", "wood", "iron ore (12% crystal shard)", "crystal shards" };
                for (int k = 0; k < ks.Length; k++)
                {
                    int n = 0; int near = 0; float minR = 999f, maxR = 0f;
                    for (int i = 0; i < L.Nodes.Count; i++) if (L.Nodes[i].Kind == ks[k]) { n++; float r = Mathf.Sqrt(L.Nodes[i].Pos.x * L.Nodes[i].Pos.x + L.Nodes[i].Pos.z * L.Nodes[i].Pos.z); minR = Mathf.Min(minR, r); maxR = Mathf.Max(maxR, r); if (r < 48f) near++; }
                    b.AppendLine("- " + ks[k] + " nodes: " + n + " (" + near + " within 48 m of the plaza; found between " + Mathf.RoundToInt(minR) + " m and " + Mathf.RoundToInt(maxR) + " m). Yields " + yields[k] + ". Respawn 45-75 s.");
                }
                b.AppendLine();
            }

            // -------------------------------------------------------------- items & equipment sources
            b.AppendLine("## 9. Equipment and where to get it");
            Dictionary<string, List<string>> src = new Dictionary<string, List<string>>();
            Action<string, string> add = (id, s) => { List<string> l; if (!src.TryGetValue(id, out l)) { l = new List<string>(); src[id] = l; } l.Add(s); };
            for (int i = 0; i < Recipes.All.Count; i++) { Recipe r = Recipes.All[i]; string m = ""; for (int k = 0; k < r.Mats.Length; k++) m += (k > 0 ? " + " : "") + r.Counts[k] + " " + Items.Name(r.Mats[k]); add(r.Result, "Craft at " + r.Station + " (" + m + (r.Gold > 0 ? ", " + r.Gold + "g" : "") + ")"); }
            for (int i = 0; i < Monsters.All.Count; i++) for (int k = 0; k < Monsters.All[i].Loot.Count; k++) add(Monsters.All[i].Loot[k].Item, "Drop: " + Monsters.All[i].Name + " " + Mathf.RoundToInt(Monsters.All[i].Loot[k].Chance * 100f) + "%");
            for (int i = 0; i < Quests.All.Count; i++) for (int k = 0; k < Quests.All[i].Rewards.Count; k++) add(Quests.All[i].Rewards[k].id, "Quest reward: " + Quests.All[i].Title);
            for (int g = 0; g < Items.ChestGrades.Length; g++)
            {
                HashSet<string> seen = new HashSet<string>();
                for (int k = 0; k < 1500; k++) { int gg; List<KeyValuePair<string, int>> r = Loot.RollChest(g, rng, out gg); for (int j = 0; j < r.Count; j++) seen.Add(r[j].Key); }
                foreach (string id in seen) add(id, "Chest: " + Items.ChestGrades[g]);
            }
            add("wood", "Gather: trees"); add("iron_ore", "Gather: ore nodes"); add("crystal_shard", "Gather: crystal nodes / ore nodes (12%)"); add("herb_red", "Gather: herb nodes"); add("herb_blue", "Gather: herb nodes"); add("herb_green", "Gather: herb nodes");
            b.AppendLine("| Item | Type | Rarity | Req Lv | Stats | Where to get it |");
            b.AppendLine("|---|---|---|---|---|---|");
            for (int i = 0; i < Items.List.Count; i++)
            {
                ItemDef d = Items.List[i]; if (!d.Equippable) continue;
                List<string> l; src.TryGetValue(d.Id, out l);
                b.AppendLine("| " + d.Name + " | " + d.Kind + (d.Kind == ItemKind.Weapon ? " (" + d.Wt + ")" : "") + " | " + Items.RarityNames[Mathf.Clamp(d.Rarity, 0, 5)] + " | " + d.Req + " | " + d.StatLine() + " | " + (l == null ? "-" : string.Join("; ", l)) + " |");
            }
            b.AppendLine();
            b.AppendLine("## 10. Rare and crafting materials");
            b.AppendLine("| Material | Rarity | Where to get it |");
            b.AppendLine("|---|---|---|");
            for (int i = 0; i < Items.List.Count; i++)
            {
                ItemDef d = Items.List[i]; if (d.Kind != ItemKind.Material && d.Kind != ItemKind.Token) continue;
                List<string> l; src.TryGetValue(d.Id, out l);
                b.AppendLine("| " + d.Name + " | " + Items.RarityNames[Mathf.Clamp(d.Rarity, 0, 5)] + " | " + (l == null ? "-" : string.Join("; ", l)) + " |");
            }
            b.AppendLine();
            b.AppendLine("## 11. Consumables");
            for (int i = 0; i < Items.List.Count; i++) { ItemDef d = Items.List[i]; if (d.Kind == ItemKind.Potion || d.Kind == ItemKind.Pill) { List<string> l; src.TryGetValue(d.Id, out l); b.AppendLine("- " + d.Name + ": " + d.Desc + "  Source: " + (l == null ? "-" : string.Join("; ", l))); } }
            b.AppendLine();

            // -------------------------------------------------------------- skills
            b.AppendLine("## 12. Skills");
            b.AppendLine("| Skill | Element | Type | Unlock Lv | Mana | Cooldown | Notes |");
            b.AppendLine("|---|---|---|---|---|---|---|");
            for (int i = 0; i < Skills.All.Count; i++) { SkillDef s = Skills.All[i]; b.AppendLine("| " + s.Name + " | " + El(s.Element) + " | " + s.Kind + " | " + s.Level + " | " + s.Mp + " | " + s.Cd.ToString("0.#") + "s | " + s.Desc + (s.Crater ? " (leaves a crater)" : "") + " |"); }
            b.AppendLine();
            b.AppendLine("## 13. Element weaknesses");
            b.AppendLine("A skill of an element a monster is weak to deals x1.4 damage; a resisted element deals x0.7; Omnigod skills deal x1.15 to everything. Basic weapon attacks are neutral.");
            return b.ToString();
        }
    }
}
