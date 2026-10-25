using System;
using System.Collections.Generic;
using UnityEngine;
using SL.Core;
using SL.Actors;

namespace SL.Gameplay
{
    [Serializable] public class ItemStack { public string id; public int n; }
    [Serializable] public class KillCount { public string id; public int n; }
    [Serializable] public class QuestProg { public string id; public int[] p; public bool accepted; public bool discovered; public bool done; }
    [Serializable] public class MailItem { public int id; public string subject; public string body; public List<ItemStack> items = new List<ItemStack>(); public int gold; public bool claimed; }

    [Serializable]
    public class Profile
    {
        public string name = "Adventurer";
        public uint look;
        public int level, xp, gold, aether, element = -1, zone;
        public bool promoted, kingdomEntered;
        public string weapon = "wood_sword", armor = "", accessory = "";
        public List<ItemStack> inv = new List<ItemStack>();
        public List<QuestProg> quests = new List<QuestProg>();
        public List<string> loadout = new List<string>();
        public List<int> openedChests = new List<int>();
        public List<string> milestones = new List<string>();
        public List<KillCount> kills = new List<KillCount>();
        public List<MailItem> mail = new List<MailItem>();
        public int nextMail = 1, totalKills, deaths, herbs, chestsOpened, crafted, playSeconds, pillsUsed;
        public long lastDaily;

        public int Count(string id) { for (int i = 0; i < inv.Count; i++) if (inv[i].id == id) return inv[i].n; return 0; }
        public void Add(string id, int n)
        {
            if (n <= 0) return;
            for (int i = 0; i < inv.Count; i++) if (inv[i].id == id) { inv[i].n += n; return; }
            inv.Add(new ItemStack { id = id, n = n });
        }
        public bool Remove(string id, int n)
        {
            for (int i = 0; i < inv.Count; i++)
                if (inv[i].id == id)
                {
                    if (inv[i].n < n) return false;
                    inv[i].n -= n; if (inv[i].n == 0) inv.RemoveAt(i); return true;
                }
            return false;
        }
        public QuestProg Q(string id) { for (int i = 0; i < quests.Count; i++) if (quests[i].id == id) return quests[i]; return null; }
        public int Kills(string id) { for (int i = 0; i < kills.Count; i++) if (kills[i].id == id) return kills[i].n; return 0; }
        public void AddKill(string id) { totalKills++; for (int i = 0; i < kills.Count; i++) if (kills[i].id == id) { kills[i].n++; return; } kills.Add(new KillCount { id = id, n = 1 }); }
        public bool Has(string ms) { return milestones.Contains(ms); }

        public static Profile Fresh(string name, uint look)
        {
            Profile p = new Profile { name = name, look = look };
            p.Add("hp_s", 3); p.Add("mp_s", 2);
            p.loadout = new List<string>();
            return p;
        }

        public string ToJson() { return JsonUtility.ToJson(this); }
        public static Profile FromJson(string j) { try { return JsonUtility.FromJson<Profile>(j); } catch (Exception) { return null; } }
    }

    public struct Stats { public int MaxHp, MaxMp, Atk, Def, H, S, T, B, M; }

    public static class Rules
    {
        public const int MaxLevel = 100;
        public static int XpToNext(int level) { return 30 + 22 * level + level * level; }

        public static Stats Compute(Profile p)
        {
            Stats s = new Stats();
            string[] eq = { p.weapon, p.armor, p.accessory };
            for (int i = 0; i < 3; i++)
            {
                ItemDef d = Items.Get(eq[i]);
                if (d == null) continue;
                s.H += d.H; s.S += d.S; s.T += d.T; s.B += d.B; s.M += d.M;
            }
            s.MaxHp = 100 + p.level * 18 + s.H;
            s.MaxMp = 50 + p.level * 6 + s.M * 4;
            s.Atk = 8 + p.level * 3 + s.S * 2 + (int)(s.M * 0.5f);
            s.Def = p.level + s.T * 2;
            return s;
        }

        public static WeaponType WeaponOf(Profile p)
        {
            ItemDef d = Items.Get(p.weapon);
            return d == null || d.Wt == WeaponType.None ? WeaponType.Sword : d.Wt;
        }

        public static float AttackRange(WeaponType w) { switch (w) { case WeaponType.Greatsword: return 3.4f; case WeaponType.Spear: return 4.4f; case WeaponType.Staff: return 16f; case WeaponType.Bow: return 22f; default: return 2.8f; } }
        public static float AttackCd(WeaponType w) { switch (w) { case WeaponType.Greatsword: return 0.9f; case WeaponType.Staff: return 0.7f; case WeaponType.Bow: return 0.8f; case WeaponType.Spear: return 0.6f; default: return 0.5f; } }
        public static float DamageMult(WeaponType w) { return w == WeaponType.Greatsword ? 1.6f : (w == WeaponType.Bow || w == WeaponType.Staff ? 1.1f : 1f); }
        public static bool IsRanged(WeaponType w) { return w == WeaponType.Staff || w == WeaponType.Bow; }

        public static bool ModesUnlocked(Profile p) { return p.level >= 10; }
        public static bool ShopUnlocked(Profile p) { return p.level >= 10; }

        /// <summary>Quest availability for a giver, evaluated on the profile.</summary>
        public static bool Available(Profile p, QuestDef q)
        {
            if (q.Hidden) return false;
            QuestProg qp = p.Q(q.Id);
            if (qp != null && (qp.accepted || qp.done)) return false;
            if (p.level < q.MinLevel) return false;
            if (!string.IsNullOrEmpty(q.Prereq)) { QuestProg pq = p.Q(q.Prereq); if (pq == null || !pq.done) return false; }
            return true;
        }

        public static int ObjProgress(Profile p, QuestDef q, int i)
        {
            Objective o = q.Objs[i];
            switch (o.Type)
            {
                case ObjType.Gather: return Mathf.Min(o.Count, p.Count(o.Target));
                case ObjType.Reach: return Mathf.Min(o.Count, p.level);
                case ObjType.Choose: return p.element >= 0 ? 1 : 0;
                default:
                    QuestProg qp = p.Q(q.Id);
                    return qp == null || qp.p == null || i >= qp.p.Length ? 0 : Mathf.Min(o.Count, qp.p[i]);
            }
        }

        public static bool Complete(Profile p, QuestDef q)
        {
            for (int i = 0; i < q.Objs.Count; i++) if (ObjProgress(p, q, i) < q.Objs[i].Count) return false;
            return true;
        }

        public static QuestProg Ensure(Profile p, QuestDef q)
        {
            QuestProg qp = p.Q(q.Id);
            if (qp == null) { qp = new QuestProg { id = q.Id, p = new int[q.Objs.Count] }; p.quests.Add(qp); }
            if (qp.p == null || qp.p.Length != q.Objs.Count) qp.p = new int[q.Objs.Count];
            return qp;
        }

        /// <summary>Advance event-style objectives. Returns ids of hidden quests newly discovered.</summary>
        public static List<string> Event(Profile p, ObjType type, string target, int amount)
        {
            List<string> found = new List<string>();
            for (int k = 0; k < Quests.All.Count; k++)
            {
                QuestDef q = Quests.All[k];
                QuestProg qp = p.Q(q.Id);
                if (qp != null && qp.done) continue;
                if (q.Hidden) { if (p.level < q.MinLevel) continue; }
                else if (qp == null || !qp.accepted) continue;
                for (int i = 0; i < q.Objs.Count; i++)
                {
                    Objective o = q.Objs[i];
                    if (o.Type != type) continue;
                    if (o.Target != target && o.Target != "any") continue;
                    qp = Ensure(p, q);
                    if (qp.p[i] >= o.Count) continue;
                    qp.p[i] = Mathf.Min(o.Count, qp.p[i] + amount);
                    if (q.Hidden && !qp.discovered) { qp.discovered = true; found.Add(q.Id); }
                }
            }
            return found;
        }
    }
}
