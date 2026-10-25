using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using SL.Core;
using SL.Actors;
using SL.Gameplay;
using SL.World;

namespace SL.Net
{
    /// <summary>
    /// Authoritative game rules. Runs only on the host/server. Clients send requests; everything is validated here.
    /// Replaceable later by a dedicated server build without changing client code.
    /// </summary>
    public sealed class ServerSim : MonoBehaviour
    {
        public static ServerSim I;
        public readonly InstanceManager Inst = new InstanceManager();
        readonly DetRng rng = new DetRng(Environment.TickCount);
        struct Pending { public float At; public PlayerNet Src; public MonsterNet Target; public float Dmg; public bool Crit; public int Kind; public PlayerNet VictimP; public MonsterNet Atk; public int Element; }
        readonly List<Pending> pending = new List<Pending>();
        float saveTimer;

        public static ServerSim Create()
        {
            if (I != null) return I;
            GameObject g = new GameObject("SL_ServerSim");
            DontDestroyOnLoad(g);
            I = g.AddComponent<ServerSim>();
            ProfileStore.Load();
            return I;
        }

        // ============================================================ helpers
        ClientRpcParams To(PlayerNet p) { ClientRpcParams c = new ClientRpcParams(); c.Send.TargetClientIds = new[] { p.OwnerClientId }; return c; }
        ClientRpcParams ToInst(Instance it)
        {
            List<ulong> ids = new List<ulong>();
            for (int i = 0; i < it.Players.Count; i++) ids.Add(it.Players[i].OwnerClientId);
            ClientRpcParams c = new ClientRpcParams(); c.Send.TargetClientIds = ids; return c;
        }
        void Toast(PlayerNet p, string m) { p.ToastClientRpc(m, To(p)); }
        void Fx(Instance it, int fx, Vector3 pos, Vector3 dir, int el)
        {
            if (it.Players.Count == 0) return;
            it.Players[0].FxClientRpc(fx, pos, dir, el, ToInst(it));
        }
        void Float(Instance it, Vector3 pos, string text, int kind)
        {
            if (it.Players.Count == 0) return;
            it.Players[0].FloatClientRpc(pos, text, kind, ToInst(it));
        }
        void WorldEvt(Instance it, int kind, int id, float a, float b, float c)
        {
            if (it.Players.Count == 0) return;
            it.Players[0].WorldEvtClientRpc(kind, id, a, b, c, ToInst(it));
        }
        void Anim(PlayerNet p, int anim) { p.S.AnimCounter++; p.NAnim.Value = (p.S.AnimCounter << 4) | anim; }

        public void SendState(PlayerNet p)
        {
            if (p.S == null || p.S.P == null) return;
            p.StateClientRpc(p.S.P.ToJson(), ProfileStore.PillAvailable, To(p));
        }

        void RecalcStats(PlayerNet p)
        {
            ServerPlayer s = p.S;
            s.St = Rules.Compute(s.P);
            s.Hp = Mathf.Min(s.Hp, s.St.MaxHp); s.Mp = Mathf.Min(s.Mp, s.St.MaxMp);
            p.NMaxHp.Value = s.St.MaxHp; p.NMaxMp.Value = s.St.MaxMp; p.NLevel.Value = s.P.level; p.NElement.Value = s.P.element; p.NWeapon.Value = s.P.weapon;
        }

        void AutoLoadout(Profile p)
        {
            List<SkillDef> un = Skills.Unlocked(p.level, p.element);
            for (int i = 0; i < un.Count; i++) if (!p.loadout.Contains(un[i].Id) && p.loadout.Count < Skills.MaxLoadout) p.loadout.Add(un[i].Id);
            for (int i = p.loadout.Count - 1; i >= 0; i--) { SkillDef d = Skills.Get(p.loadout[i]); if (d == null || d.Level > p.level || (d.Element >= 0 && d.Element != p.element)) p.loadout.RemoveAt(i); }
        }

        // ============================================================ join / leave
        public void OnHello(PlayerNet pn, string name, uint look)
        {
            if (pn.S == null) pn.S = new ServerPlayer();
            if (string.IsNullOrEmpty(name)) name = "Adventurer";
            name = name.Trim(); if (name.Length > 16) name = name.Substring(0, 16);
            for (int i = 0; i < PlayerNet.All.Count; i++)
            {
                PlayerNet o = PlayerNet.All[i];
                if (o != pn && o.S != null && o.S.Ready && o.S.P != null && o.S.P.name == name) { Toast(pn, "That character is already online."); NetworkManager.Singleton.DisconnectReason = "duplicate"; return; }
            }
            ServerPlayer s = pn.S;
            s.P = ProfileStore.GetOrCreate(name, look);
            if (look != 0) s.P.look = look;
            if (s.P.zone == 1 && !s.P.promoted) s.P.zone = 0;
            AutoLoadout(s.P);
            s.St = Rules.Compute(s.P); s.Hp = s.St.MaxHp; s.Mp = s.St.MaxMp;
            pn.NName.Value = name; pn.NLook.Value = s.P.look;
            RecalcStats(pn);
            s.Ready = true;
            Enter(pn, s.P.zone, true);
            Toast(pn, "Welcome, " + name + "!");
        }

        void Enter(PlayerNet pn, int zone, bool first)
        {
            ServerPlayer s = pn.S;
            if (s.In != null) { s.In.Players.Remove(pn); if (s.In.Players.Count == 0) s.In.EmptySince = Time.time; }
            bool created;
            Instance it = Inst.Pick(zone, out created);
            if (created) PopulateMonsters(it);
            it.Players.Add(pn); it.EmptySince = -1f;
            s.In = it; s.P.zone = zone;
            if (zone == 1 && !s.P.kingdomEntered) { s.P.kingdomEntered = true; }
            Vector3 sp = it.L.SpawnPoint + new Vector3(rng.Range(-2f, 2f), 0f, rng.Range(-2f, 2f));
            sp.y = it.L.Ground(it.Hf, sp.x, sp.z);
            pn.transform.position = sp; pn.NPos.Value = sp; s.Move = Vector2.zero;
            pn.NZone.Value = it.Key;
            s.Hp = s.St.MaxHp; s.Mp = s.St.MaxMp; s.Dead = false; pn.NDead.Value = false;
            pn.ZoneClientRpc(zone, it.Id, sp, BuildSync(it), To(pn));
            SendState(pn);
            CheckMilestones(pn);
        }

        string BuildSync(Instance it)
        {
            StringBuilder b = new StringBuilder();
            b.Append("b:"); bool f = true; foreach (int id in it.DestroyedBlocks) { if (!f) b.Append(','); b.Append(id); f = false; }
            b.Append("|n:"); f = true; foreach (KeyValuePair<int, float> kv in it.NodeBackAt) if (kv.Value > Time.time) { if (!f) b.Append(','); b.Append(kv.Key); f = false; }
            b.Append("|k:"); f = true; foreach (KeyValuePair<int, float> kv in it.ChestBackAt) if (kv.Value > Time.time) { if (!f) b.Append(','); b.Append(kv.Key); f = false; }
            b.Append("|c:");
            for (int i = 0; i < it.Hf.Craters.Count; i++) { Crater c = it.Hf.Craters[i]; if (i > 0) b.Append(';'); b.Append(c.x.ToString("F1")).Append(',').Append(c.z.ToString("F1")).Append(',').Append(c.r.ToString("F1")).Append(',').Append(c.d.ToString("F2")); }
            return b.ToString();
        }

        public void OnLeave(PlayerNet pn)
        {
            if (pn.S == null) return;
            if (pn.S.In != null) { pn.S.In.Players.Remove(pn); if (pn.S.In.Players.Count == 0) pn.S.In.EmptySince = Time.time; }
            if (pn.S.P != null) ProfileStore.Save(true);
        }

        // ============================================================ monsters
        void PopulateMonsters(Instance it)
        {
            for (int i = 0; i < it.L.Spawns.Count; i++) SpawnMonster(it, it.L.Spawns[i].Type, it.L.Spawns[i].Pos, i);
            if (it.Zone == 0) SpawnMonster(it, "warden", it.L.BossPos, 900);
        }

        MonsterNet SpawnMonster(Instance it, string type, Vector3 pos, int index)
        {
            MonsterDef d = Monsters.Get(type);
            GameObject prefab = NetBootstrap.MonsterPrefab;
            if (d == null || prefab == null) return null;
            GameObject go = NetBootstrap.Make(prefab, pos);
            MonsterNet m = go.GetComponent<MonsterNet>();
            go.GetComponent<NetworkObject>().Spawn();
            m.Def = d; m.In = it; m.Home = pos; m.Hp = d.Hp; m.SpawnIndex = index; m.Speed = d.Speed;
            m.NType.Value = Monsters.IndexOf(d.Id); m.NMaxHp.Value = d.Hp; m.NHp.Value = d.Hp; m.NZone.Value = it.Key; m.NPos.Value = pos; m.NDead.Value = false;
            m.NextSlam = Time.time + 8f;
            it.Monsters.Add(m);
            return m;
        }

        // ============================================================ main loop
        void Update()
        {
            NetworkManager nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsServer) return;
            float dt = Time.deltaTime, now = Time.time;
            for (int i = 0; i < Inst.All.Count; i++) TickInstance(Inst.All[i], dt, now);
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                if (pending[i].At > now) continue;
                Pending p = pending[i]; pending.RemoveAt(i);
                ResolvePending(p);
            }
            saveTimer += dt;
            if (saveTimer > 15f) { saveTimer = 0f; ProfileStore.Save(true); }
        }

        void TickInstance(Instance it, float dt, float now)
        {
            for (int i = 0; i < it.Players.Count; i++) TickPlayer(it.Players[i], it, dt, now);
            if (it.Players.Count > 0)
                for (int i = 0; i < it.Monsters.Count; i++) TickMonster(it.Monsters[i], it, dt, now);
            // restore walls and nodes
            List<int> restore = null;
            foreach (KeyValuePair<int, float> kv in it.BlockRestoreAt) if (kv.Value < now) { if (restore == null) restore = new List<int>(); restore.Add(kv.Key); }
            if (restore != null) for (int i = 0; i < restore.Count; i++) { int id = restore[i]; it.BlockRestoreAt.Remove(id); it.DestroyedBlocks.Remove(id); it.BlockHp.Remove(id); WorldEvt(it, 3, id, 0, 0, 0); }
            List<int> back = null;
            foreach (KeyValuePair<int, float> kv in it.NodeBackAt) if (kv.Value < now) { if (back == null) back = new List<int>(); back.Add(kv.Key); }
            if (back != null) for (int i = 0; i < back.Count; i++) { it.NodeBackAt.Remove(back[i]); WorldEvt(it, 4, back[i], 1, 0, 0); }
            List<int> cb = null;
            foreach (KeyValuePair<int, float> kv in it.ChestBackAt) if (kv.Value < now) { if (cb == null) cb = new List<int>(); cb.Add(kv.Key); }
            if (cb != null) for (int i = 0; i < cb.Count; i++) { it.ChestBackAt.Remove(cb[i]); WorldEvt(it, 6, cb[i], 1, 0, 0); }
        }

        void TickPlayer(PlayerNet pn, Instance it, float dt, float now)
        {
            ServerPlayer s = pn.S;
            if (s == null || !s.Ready) return;
            s.P.playSeconds += 0; // counted below
            if (s.Dead)
            {
                if (now >= s.DeadUntil) Respawn(pn);
                return;
            }
            float speed = s.Block ? Mover.BlockSpeed : (s.Sprint ? Mover.SprintSpeed : Mover.WalkSpeed);
            Vector3 pos = pn.transform.position;
            if (s.Move.sqrMagnitude > 0.01f)
            {
                pos = Mover.Step(it.L, it.Hf, it.DestroyedBlocks, pos, s.Move, speed, dt);
                pn.transform.position = pos;
            }
            else pos.y = it.L.Ground(it.Hf, pos.x, pos.z);
            if (pos != pn.NPos.Value) pn.NPos.Value = pos;
            if (Mathf.Abs(Mathf.DeltaAngle(pn.NYaw.Value, s.Yaw)) > 0.5f) pn.NYaw.Value = s.Yaw;
            if (pn.NBlock.Value != s.Block) pn.NBlock.Value = s.Block;
            // regen
            if (now - s.LastRegen >= 1f)
            {
                s.LastRegen = now;
                s.P.playSeconds++;
                s.Mp = Mathf.Min(s.St.MaxMp, s.Mp + 2f + s.St.MaxMp * 0.01f);
                if (now - s.LastHurt > 6f) s.Hp = Mathf.Min(s.St.MaxHp, s.Hp + s.St.MaxHp * 0.01f);
                if (s.Shield > 0f && now > s.ShieldUntil) s.Shield = 0f;
                CheckVisits(pn, it);
            }
            int hp = Mathf.CeilToInt(s.Hp), mp = Mathf.FloorToInt(s.Mp);
            if (pn.NHp.Value != hp) pn.NHp.Value = hp;
            if (pn.NMp.Value != mp) pn.NMp.Value = mp;
            int sh = Mathf.CeilToInt(s.Shield); if (pn.NShield.Value != sh) pn.NShield.Value = sh;
        }

        void CheckVisits(PlayerNet pn, Instance it)
        {
            Vector3 p = pn.transform.position;
            Profile pr = pn.S.P; bool changed = false;
            if (it.Zone == 0 && (new Vector2(p.x - it.L.BossPos.x, p.z - it.L.BossPos.z)).magnitude < 14f) changed |= Event(pn, ObjType.Visit, "ruins", 1);
            if (it.L.OnBridge(p.x, p.z)) changed |= Event(pn, ObjType.Visit, "bridge", 1);
            if (it.Zone == 1 && it.Hf.RiverDist(p.x, p.z) < 12f) changed |= Event(pn, ObjType.Visit, "river", 1);
            if (it.Zone == 1 && p.z > 8f && Mathf.Abs(p.x) < 14f) changed |= Event(pn, ObjType.Visit, "keep", 1);
            if (changed) SendState(pn);
        }

        bool Event(PlayerNet pn, ObjType type, string target, int amt)
        {
            List<string> found = Rules.Event(pn.S.P, type, target, amt);
            for (int i = 0; i < found.Count; i++) { QuestDef q = Quests.Get(found[i]); if (q != null) Toast(pn, "Hidden quest discovered: " + q.Title); }
            return true;
        }

        // ============================================================ monster AI
        void TickMonster(MonsterNet m, Instance it, float dt, float now)
        {
            if (m == null || m.Def == null) return;
            MonsterDef d = m.Def;
            if (m.NDead.Value)
            {
                if (now >= m.RespawnAt)
                {
                    m.Hp = d.Hp; m.NHp.Value = d.Hp; m.NDead.Value = false; m.Enraged = false; m.Target = null;
                    m.transform.position = m.Home; m.NPos.Value = m.Home;
                }
                return;
            }
            PlayerNet best = null; float bd = d.Aggro * (m.Target != null ? 1.6f : 1f);
            for (int i = 0; i < it.Players.Count; i++)
            {
                PlayerNet p = it.Players[i];
                if (p.S == null || p.S.Dead || !p.S.Ready) continue;
                float dist = Vector3.Distance(p.transform.position, m.transform.position);
                if (dist < bd) { bd = dist; best = p; }
            }
            m.Target = best;
            Vector3 pos = m.transform.position;
            float speed = m.Speed * (m.Enraged ? 1.4f : 1f);
            float distHome = Vector3.Distance(pos, m.Home);
            Vector2 move = Vector2.zero; bool wantsMove = false;

            if (m.Target != null && distHome < 55f)
            {
                Vector3 tp = m.Target.transform.position;
                Vector2 to = new Vector2(tp.x - pos.x, tp.z - pos.z); float dist = to.magnitude;
                float stop = d.Ranged ? d.Range * 0.8f : d.Range * 0.85f;
                if (dist > stop) { move = to.normalized; wantsMove = true; }
                m.NYaw.Value = Mathf.Atan2(to.x, to.y) * Mathf.Rad2Deg;
                if (dist <= d.Range + 0.6f && now >= m.NextAtk) MonsterAttack(m, it, now);
                if (d.Boss) BossLogic(m, it, now);
            }
            else if (distHome > 4f)
            {
                Vector2 to = new Vector2(m.Home.x - pos.x, m.Home.z - pos.z);
                move = to.normalized; wantsMove = true; speed *= 1.3f;
                m.NYaw.Value = Mathf.Atan2(to.x, to.y) * Mathf.Rad2Deg;
                m.Hp = Mathf.Min(d.Hp, m.Hp + d.Hp * 0.05f * dt); m.NHp.Value = Mathf.CeilToInt(m.Hp);
            }
            else
            {
                m.WanderT -= dt;
                if (m.WanderT <= 0f) { m.WanderT = rng.Range(2f, 5f); m.WanderDir = rng.Next() < 0.4f ? Vector2.zero : new Vector2(rng.Range(-1f, 1f), rng.Range(-1f, 1f)).normalized; }
                if (m.WanderDir.sqrMagnitude > 0.1f && distHome < 7f) { move = m.WanderDir; wantsMove = true; speed *= 0.4f; m.NYaw.Value = Mathf.Atan2(move.x, move.y) * Mathf.Rad2Deg; }
            }
            if (wantsMove)
            {
                Vector2 np = new Vector2(pos.x, pos.z) + move * speed * dt;
                np = it.L.Resolve(np, 0.6f * d.Scale, it.DestroyedBlocks);
                if (!it.Hf.IsDeep(np.x, np.y)) pos = new Vector3(np.x, pos.y, np.y);
            }
            float gy = it.L.Ground(it.Hf, pos.x, pos.z);
            pos.y = d.Flying ? gy + 1.1f : gy;
            m.transform.position = pos;
            if ((pos - m.NPos.Value).sqrMagnitude > 0.0004f) m.NPos.Value = pos;
        }

        void MonsterAttack(MonsterNet m, Instance it, float now)
        {
            MonsterDef d = m.Def;
            m.NextAtk = now + d.AtkCd * (m.Enraged ? 0.7f : 1f);
            m.NAnim.Value = ((m.NAnim.Value >> 4) + 1) << 4 | 1;
            float dmg = d.Dmg * (m.Enraged ? 1.3f : 1f) * rng.Range(0.9f, 1.1f);
            Pending p = new Pending { At = now + (d.Ranged ? Vector3.Distance(m.transform.position, m.Target.transform.position) / 14f : 0.25f), Dmg = dmg, VictimP = m.Target, Atk = m, Kind = 1 };
            pending.Add(p);
            if (d.Ranged) Fx(it, 10, m.transform.position + Vector3.up * 1.2f, (m.Target.transform.position + Vector3.up - m.transform.position).normalized, 5);
        }

        void BossLogic(MonsterNet m, Instance it, float now)
        {
            if (!m.Enraged && m.Hp < m.Def.Hp * 0.4f) { m.Enraged = true; Float(it, m.transform.position + Vector3.up * 4f, "ENRAGED!", 3); }
            if (now >= m.NextSlam)
            {
                m.NextSlam = now + (m.Enraged ? 6f : 9f);
                Fx(it, 11, m.transform.position, Vector3.up, 3);
                pending.Add(new Pending { At = now + 1.0f, Atk = m, Kind = 2, Dmg = m.Def.Dmg * 1.6f });
            }
        }

        void ResolvePending(Pending p)
        {
            if (p.Kind == 1)
            {
                if (p.VictimP != null && p.VictimP.S != null && p.Atk != null && !p.Atk.NDead.Value)
                {
                    Vector3 a = p.Atk.transform.position, b = p.VictimP.transform.position;
                    if (!p.Atk.Def.Ranged && Vector3.Distance(a, b) > p.Atk.Def.Range + 2f) return;
                    HurtPlayer(p.VictimP, p.Dmg, p.Atk.Def.Name);
                }
            }
            else if (p.Kind == 2 && p.Atk != null && !p.Atk.NDead.Value)
            {
                Instance it = p.Atk.In;
                for (int i = 0; i < it.Players.Count; i++)
                    if (Vector3.Distance(it.Players[i].transform.position, p.Atk.transform.position) < 7f) HurtPlayer(it.Players[i], p.Dmg, p.Atk.Def.Name);
            }
            else if (p.Kind == 3 && p.Target != null && !p.Target.NDead.Value && p.Src != null)
            {
                float mult = Monsters.ElementMult(p.Target.Def, p.Element);
                HitMonster(p.Src, p.Target, p.Dmg * mult, p.Crit, mult > 1.05f ? 1 : (mult < 0.95f ? -1 : 0));
            }
        }

        // ============================================================ damage
        public void HurtPlayer(PlayerNet pn, float dmg, string src)
        {
            ServerPlayer s = pn.S;
            if (s == null || s.Dead || Time.time < s.InvulnUntil) return;
            float def = s.St.Def * 0.6f;
            dmg = Mathf.Max(1f, dmg - def);
            if (s.Block) dmg *= 0.35f;
            if (s.Shield > 0f) { float ab = Mathf.Min(s.Shield, dmg); s.Shield -= ab; dmg -= ab; }
            s.Hp -= dmg; s.LastHurt = Time.time;
            Float(s.In, pn.transform.position + Vector3.up * 2.2f, Mathf.CeilToInt(dmg).ToString(), 1);
            Anim(pn, 5);
            if (s.Hp <= 0f)
            {
                s.Hp = 0f; s.Dead = true; s.DeadUntil = Time.time + 4f; s.P.deaths++; s.Move = Vector2.zero;
                pn.NDead.Value = true; pn.NHp.Value = 0;
                Toast(pn, "You were defeated by " + src + ".");
                SendState(pn);
            }
        }

        void Respawn(PlayerNet pn)
        {
            ServerPlayer s = pn.S;
            Instance it = s.In;
            Vector3 sp = it.L.SpawnPoint; sp.y = it.L.Ground(it.Hf, sp.x, sp.z);
            pn.transform.position = sp; pn.NPos.Value = sp;
            s.Hp = s.St.MaxHp * 0.6f; s.Mp = s.St.MaxMp * 0.5f; s.Dead = false; pn.NDead.Value = false; s.InvulnUntil = Time.time + 2.5f;
            pn.ZoneClientRpc(it.Zone, it.Id, sp, "", To(pn));
        }

        public void OnRespawn(PlayerNet pn) { if (pn.S != null && pn.S.Dead && Time.time > pn.S.DeadUntil - 3f) { pn.S.DeadUntil = 0f; } }

        void HitMonster(PlayerNet src, MonsterNet m, float dmg, bool crit, int effect)
        {
            if (m.NDead.Value) return;
            Instance it = m.In;
            m.Hp -= dmg; m.LastAttacker = src; m.NHp.Value = Mathf.Max(0, Mathf.CeilToInt(m.Hp));
            m.NAnim.Value = ((m.NAnim.Value >> 4) + 1) << 4 | 2;
            if (m.Target == null) m.Target = src;
            Float(it, m.transform.position + Vector3.up * (1.8f * m.Def.Scale), Mathf.CeilToInt(dmg).ToString() + (crit ? "!" : "") + (effect > 0 ? " WEAK" : (effect < 0 ? " resist" : "")), crit || effect > 0 ? 2 : 0);
            Fx(it, 1, m.transform.position + Vector3.up, Vector3.up, src.S.P.element);
            if (m.Hp <= 0f) KillMonster(m, src);
        }

        void KillMonster(MonsterNet m, PlayerNet killer)
        {
            MonsterDef d = m.Def; Instance it = m.In;
            m.NDead.Value = true; m.RespawnAt = Time.time + (d.Boss ? 240f : rng.Range(25f, 45f)); m.Target = null;
            Fx(it, 12, m.transform.position, Vector3.up, 9);
            for (int i = 0; i < it.Players.Count; i++)
            {
                PlayerNet p = it.Players[i];
                if (p.S == null || p.S.Dead) continue;
                bool credit = p == killer || Vector3.Distance(p.transform.position, m.transform.position) < 14f;
                if (!credit) continue;
                Profile pr = p.S.P;
                float lvDiff = pr.level - d.Level;
                float xpMul = Mathf.Clamp(1.1f - lvDiff * 0.12f, 0.15f, 1.3f);
                int xp = Mathf.Max(1, Mathf.RoundToInt(d.XpReward * xpMul));
                pr.AddKill(d.Id);
                int gold = rng.Int(d.GoldMin, d.GoldMax + 1);
                pr.gold += gold;
                string loot = "";
                List<KeyValuePair<string, int>> drops = Loot.Roll(d, rng);
                for (int k = 0; k < drops.Count; k++) { pr.Add(drops[k].Key, drops[k].Value); loot += " " + Items.Name(drops[k].Key) + " x" + drops[k].Value; }
                Event(p, ObjType.Kill, d.Id, 1);
                Float(it, m.transform.position + Vector3.up * 3f, "+" + xp + " XP", 4);
                Toast(p, d.Name + " defeated  +" + xp + " XP  +" + gold + "g" + (loot.Length > 0 ? "  Loot:" + loot : ""));
                AddXp(p, xp);
                if (d.Id == "warden") Toast(p, "The Ancient Warden falls! Return to Elder Maren.");
                SendState(p);
            }
        }

        // ============================================================ progression
        void AddXp(PlayerNet pn, int xp)
        {
            Profile p = pn.S.P;
            if (p.level >= Rules.MaxLevel) return;
            p.xp += xp;
            bool up = false;
            while (p.level < Rules.MaxLevel && p.xp >= Rules.XpToNext(p.level)) { p.xp -= Rules.XpToNext(p.level); p.level++; up = true; }
            if (up) OnLevelUp(pn);
            CheckMilestones(pn);
        }

        void OnLevelUp(PlayerNet pn)
        {
            ServerPlayer s = pn.S;
            RecalcStats(pn); AutoLoadout(s.P);
            s.Hp = s.St.MaxHp; s.Mp = s.St.MaxMp;
            Fx(s.In, 8, pn.transform.position, Vector3.up, s.P.element);
            Toast(pn, "LEVEL UP! You are now level " + s.P.level + ".");
            if (s.P.level == 10) Toast(pn, "Breakthrough! Speak to Elder Maren to begin your promotion quest. Modes and the shop are now unlocked.");
        }

        void CheckMilestones(PlayerNet pn)
        {
            Profile p = pn.S.P;
            for (int i = 0; i < Milestones.All.Count; i++)
            {
                Milestone m = Milestones.All[i];
                if (p.Has(m.Id)) continue;
                bool ok = false;
                switch (m.Kind)
                {
                    case "kills": ok = p.totalKills >= m.Value; break;
                    case "crafted": ok = p.crafted >= m.Value; break;
                    case "herbs": ok = p.herbs >= m.Value; break;
                    case "chests": ok = p.chestsOpened >= m.Value; break;
                    case "level": ok = p.level >= m.Value; break;
                    case "promoted": ok = p.promoted; break;
                    case "kingdom": ok = p.kingdomEntered; break;
                }
                if (!ok) continue;
                p.milestones.Add(m.Id);
                int grade = m.ChestGrade;
                string extra = "";
                if (m.WorldFirst && ProfileStore.ClaimWorldFirst(m.Id)) { grade = Mathf.Min(10, grade + 2); extra = "  WORLD FIRST!"; }
                p.Add("chest_" + grade, 1); p.gold += m.Gold;
                Toast(pn, "Milestone: " + m.Title + extra + "  +" + m.Gold + "g  +" + Items.ChestGrades[grade] + " Chest");
            }
        }

        // ============================================================ combat requests
        public void OnAction(PlayerNet pn, int action, float yaw)
        {
            ServerPlayer s = pn.S;
            if (s == null || !s.Ready || s.Dead) return;
            float now = Time.time; s.Yaw = yaw; pn.NYaw.Value = yaw;
            Instance it = s.In;
            if (action == 0 || action == 1)
            {
                if (now < s.NextAttack) return;
                bool heavy = action == 1;
                WeaponType wt = Rules.WeaponOf(s.P);
                s.NextAttack = now + Rules.AttackCd(wt) * (heavy ? 1.7f : 1f);
                Anim(pn, heavy ? 2 : 1);
                float range = Rules.AttackRange(wt);
                float dmg = (s.St.Atk * Rules.DamageMult(wt)) * (heavy ? 2.1f : 1f);
                Vector3 fwd = Mover.Forward(yaw);
                if (Rules.IsRanged(wt))
                {
                    MonsterNet t = FindTarget(it, pn.transform.position, fwd, range, 18f, 1.4f);
                    Fx(it, 2, pn.transform.position + Vector3.up * 1.4f, fwd, s.P.element < 0 ? 4 : s.P.element);
                    if (t != null) QueueHit(pn, t, dmg, 0.15f + Vector3.Distance(t.transform.position, pn.transform.position) / 26f);
                }
                else
                {
                    Fx(it, 0, pn.transform.position + Vector3.up, fwd, s.P.element < 0 ? 4 : s.P.element);
                    for (int i = 0; i < it.Monsters.Count; i++)
                    {
                        MonsterNet m = it.Monsters[i];
                        if (m.NDead.Value) continue;
                        Vector3 to = m.transform.position - pn.transform.position; to.y = 0f;
                        float dist = to.magnitude - m.Def.Scale * 0.5f;
                        if (dist > range) continue;
                        if (dist > 1f && Vector3.Angle(fwd, to) > (heavy ? 95f : 70f)) continue;
                        QueueHit(pn, m, dmg, 0.18f);
                    }
                    DamageBlocks(it, pn.transform.position + fwd * 2f, 2.4f, dmg * 0.6f);
                }
            }
            else if (action == 2)
            {
                if (now < s.NextDodge) return;
                s.NextDodge = now + 1.1f; s.InvulnUntil = now + 0.45f;
                Anim(pn, 4);
                Vector2 dir = s.Move.sqrMagnitude > 0.05f ? s.Move.normalized : new Vector2(Mover.Forward(yaw).x, Mover.Forward(yaw).z);
                Vector3 pos = pn.transform.position;
                for (int i = 0; i < 4; i++) pos = Mover.Step(it.L, it.Hf, it.DestroyedBlocks, pos, dir, 20f, 0.06f);
                pn.transform.position = pos; pn.NPos.Value = pos;
            }
            else if (action == 3) Anim(pn, 6);
        }

        void QueueHit(PlayerNet src, MonsterNet m, float dmg, float delay, int element = -1)
        {
            bool crit = rng.Next() < 0.12f;
            float d = dmg * rng.Range(0.9f, 1.1f) * (crit ? 1.6f : 1f) - m.Def.Level * 0.3f;
            pending.Add(new Pending { At = Time.time + delay, Src = src, Target = m, Dmg = Mathf.Max(1f, d), Crit = crit, Kind = 3, Element = element });
        }

        MonsterNet FindTarget(Instance it, Vector3 pos, Vector3 fwd, float range, float halfAngle, float width)
        {
            MonsterNet best = null; float bd = 999f;
            for (int i = 0; i < it.Monsters.Count; i++)
            {
                MonsterNet m = it.Monsters[i];
                if (m.NDead.Value) continue;
                Vector3 to = m.transform.position - pos; to.y = 0f;
                float dist = to.magnitude;
                if (dist > range) continue;
                float ang = Vector3.Angle(fwd, to);
                float lateral = dist * Mathf.Sin(ang * Mathf.Deg2Rad);
                if (ang > 90f || (ang > halfAngle && lateral > width + m.Def.Scale * 0.5f)) continue;
                if (dist < bd) { bd = dist; best = m; }
            }
            return best;
        }

        public void OnCast(PlayerNet pn, string skillId, float yaw)
        {
            ServerPlayer s = pn.S;
            if (s == null || !s.Ready || s.Dead) return;
            SkillDef sk = Skills.Get(skillId);
            if (sk == null || !s.P.loadout.Contains(sk.Id) || sk.Level > s.P.level) return;
            if (sk.Element >= 0 && sk.Element != s.P.element) return;
            float now = Time.time; float until;
            if (s.Cd.TryGetValue(sk.Id, out until) && now < until) return;
            if (s.Mp < sk.Mp) { Toast(pn, "Not enough mana."); return; }
            s.Mp -= sk.Mp; s.Cd[sk.Id] = now + sk.Cd; s.Yaw = yaw; pn.NYaw.Value = yaw;
            Instance it = s.In; Vector3 fwd = Mover.Forward(yaw); Vector3 pos = pn.transform.position;
            int el = sk.Element < 0 ? (s.P.element < 0 ? 4 : s.P.element) : sk.Element;
            float baseDmg = s.St.Atk * sk.Mult + s.St.M * 1.5f;
            Anim(pn, 3);
            switch (sk.Kind)
            {
                case SkillKind.Melee:
                    {
                        Fx(it, 0, pos + Vector3.up, fwd, el);
                        for (int i = 0; i < it.Monsters.Count; i++)
                        {
                            MonsterNet m = it.Monsters[i]; if (m.NDead.Value) continue;
                            Vector3 to = m.transform.position - pos; to.y = 0f;
                            if (to.magnitude - m.Def.Scale * 0.5f > sk.Range || (to.magnitude > 1.2f && Vector3.Angle(fwd, to) > 80f)) continue;
                            QueueHit(pn, m, baseDmg, 0.2f, sk.Element);
                        }
                        break;
                    }
                case SkillKind.Projectile:
                    {
                        MonsterNet t = FindTarget(it, pos, fwd, sk.Range, 22f, sk.Radius + 0.8f);
                        Fx(it, 2, pos + Vector3.up * 1.4f, fwd, el);
                        if (t != null) QueueHit(pn, t, baseDmg, 0.2f + Vector3.Distance(t.transform.position, pos) / 24f, sk.Element);
                        break;
                    }
                case SkillKind.Nova:
                    {
                        Fx(it, 3, pos, Vector3.up, el);
                        for (int i = 0; i < it.Monsters.Count; i++)
                        {
                            MonsterNet m = it.Monsters[i]; if (m.NDead.Value) continue;
                            if (Vector3.Distance(m.transform.position, pos) - m.Def.Scale * 0.5f <= sk.Radius) QueueHit(pn, m, baseDmg, 0.25f, sk.Element);
                        }
                        DamageBlocks(it, pos, sk.Radius, baseDmg * 0.8f);
                        if (sk.Crater) TryCrater(it, pos + fwd * 0.5f, Mathf.Min(4f, sk.Radius * 0.55f));
                        break;
                    }
                case SkillKind.Dash:
                    {
                        Vector2 dir = new Vector2(fwd.x, fwd.z);
                        Vector3 np = pos;
                        float stepLen = sk.Range / 8f;
                        for (int i = 0; i < 8; i++) np = Mover.Step(it.L, it.Hf, it.DestroyedBlocks, np, dir, stepLen / 0.05f, 0.05f);
                        Fx(it, 4, pos + Vector3.up, (np - pos), el);
                        for (int i = 0; i < it.Monsters.Count; i++)
                        {
                            MonsterNet m = it.Monsters[i]; if (m.NDead.Value) continue;
                            Vector3 a = pos, b = np, c = m.transform.position;
                            Vector3 ab = b - a; float t = ab.sqrMagnitude < 0.01f ? 0f : Mathf.Clamp01(Vector3.Dot(c - a, ab) / ab.sqrMagnitude);
                            if (Vector3.Distance(a + ab * t, c) <= sk.Radius + m.Def.Scale * 0.5f) QueueHit(pn, m, baseDmg, 0.15f, sk.Element);
                        }
                        pn.transform.position = np; pn.NPos.Value = np; s.InvulnUntil = now + 0.3f;
                        if (sk.Crater) TryCrater(it, np, 2.8f);
                        break;
                    }
                case SkillKind.Heal:
                    {
                        Fx(it, 5, pos, Vector3.up, el);
                        for (int i = 0; i < it.Players.Count; i++)
                        {
                            PlayerNet o = it.Players[i];
                            if (o.S == null || o.S.Dead || Vector3.Distance(o.transform.position, pos) > sk.Radius) continue;
                            o.S.Hp = Mathf.Min(o.S.St.MaxHp, o.S.Hp + o.S.St.MaxHp * sk.Mult * 0.3f + s.St.M * 2f);
                            Float(it, o.transform.position + Vector3.up * 2.2f, "+HP", 5);
                        }
                        break;
                    }
                case SkillKind.Shield:
                    s.Shield = s.St.MaxHp * 0.25f * sk.Mult + s.St.T * 2f; s.ShieldUntil = now + 8f;
                    Fx(it, 6, pos, Vector3.up, el); break;
            }
            SendState(pn);
        }

        void TryCrater(Instance it, Vector3 p, float r)
        {
            if (Time.time - it.LastCrater < 1.5f) return;
            if (new Vector2(p.x, p.z).magnitude < it.L.Def.PlazaR + 3f) return;
            if (it.L.OnBridge(p.x, p.z) || it.Hf.IsWater(p.x, p.z)) return;
            it.LastCrater = Time.time;
            it.Hf.AddCrater(p.x, p.z, r, 0.9f);
            WorldEvt(it, 5, 0, p.x, p.z, r);
            Fx(it, 7, p, Vector3.up, 2);
        }

        void DamageBlocks(Instance it, Vector3 at, float radius, float dmg)
        {
            if (it.L.Blocks.Count == 0) return;
            for (int i = 0; i < it.L.Blocks.Count; i++)
            {
                BlockDef b = it.L.Blocks[i];
                if (it.DestroyedBlocks.Contains(b.Id)) continue;
                if (Vector3.Distance(new Vector3(b.Pos.x, at.y, b.Pos.z), at) > radius + 1.6f) continue;
                if (Mathf.Abs(b.Pos.y - at.y) > 5f + 2f) continue;
                float hp; if (!it.BlockHp.TryGetValue(b.Id, out hp)) hp = b.Hp;
                hp -= dmg;
                if (hp <= 0f) { it.DestroyedBlocks.Add(b.Id); it.BlockRestoreAt[b.Id] = Time.time + 150f; WorldEvt(it, 2, b.Id, 0, 0, 0); }
                else it.BlockHp[b.Id] = hp;
            }
        }

        // ============================================================ interaction
        public void OnInteract(PlayerNet pn)
        {
            ServerPlayer s = pn.S;
            if (s == null || !s.Ready || s.Dead) return;
            Instance it = s.In; Vector3 p = pn.transform.position; ZoneLayout L = it.L;
            float best = 3.6f; int kind = 0; int idx = -1;
            for (int i = 0; i < L.Npcs.Count; i++) { float d = Vector3.Distance(p, L.Npcs[i].Pos); if (d < best) { best = d; kind = 1; idx = i; } }
            float dp = Vector3.Distance(p, L.PortalPos); if (dp < best + 1f && dp < 4.5f) { best = dp; kind = 4; idx = 0; }
            for (int i = 0; i < L.Nodes.Count; i++)
            {
                NodeDef n = L.Nodes[i]; if (it.NodeBackAt.ContainsKey(n.Id) && it.NodeBackAt[n.Id] > Time.time) continue;
                float d = Vector3.Distance(p, n.Pos) - 0.4f; if (d < best) { best = d; kind = 2; idx = i; }
            }
            for (int i = 0; i < L.Chests.Count; i++)
            {
                ChestDef c = L.Chests[i]; if (it.ChestBackAt.ContainsKey(c.Id) && it.ChestBackAt[c.Id] > Time.time) continue;
                float d = Vector3.Distance(p, c.Pos) - 0.4f; if (d < best) { best = d; kind = 3; idx = i; }
            }
            if (kind == 1)
            {
                NpcDef n = L.Npcs[idx];
                if (Event(pn, ObjType.Talk, n.Id, 1)) { }
                pn.InteractClientRpc(1, n.Id, "", To(pn)); SendState(pn);
            }
            else if (kind == 2) Gather(pn, it, L.Nodes[idx]);
            else if (kind == 3) OpenWorldChest(pn, it, L.Chests[idx]);
            else if (kind == 4) UsePortal(pn, it);
            else Toast(pn, "Nothing to interact with here.");
        }

        void Gather(PlayerNet pn, Instance it, NodeDef n)
        {
            Profile pr = pn.S.P; string id = ""; int cnt = 1; string evtTarget = "";
            switch (n.Kind)
            {
                case NodeKind.Tree: id = "wood"; cnt = rng.Int(2, 4); evtTarget = "wood"; break;
                case NodeKind.Ore: id = rng.Next() < 0.12f ? "crystal_shard" : "iron_ore"; cnt = rng.Int(1, 4); evtTarget = "ore"; break;
                case NodeKind.Crystal: id = "crystal_shard"; cnt = rng.Int(1, 3); evtTarget = "crystal"; break;
                case NodeKind.Herb: { string[] h = { "herb_red", "herb_blue", "herb_green" }; id = h[rng.Int(0, 3)]; cnt = rng.Int(1, 3); evtTarget = "herb"; pr.herbs += cnt; break; }
            }
            if (Items.Get(id) == null) id = "wood";
            pr.Add(id, cnt);
            it.NodeBackAt[n.Id] = Time.time + (n.Kind == NodeKind.Herb ? 45f : 75f);
            WorldEvt(it, 4, n.Id, 0, 0, 0);
            Event(pn, ObjType.Harvest, evtTarget, cnt);
            Toast(pn, "Gathered " + Items.Name(id) + " x" + cnt);
            Float(it, n.Pos + Vector3.up * 1.5f, "+" + cnt + " " + Items.Name(id), 5);
            CheckMilestones(pn); SendState(pn);
        }

        void OpenWorldChest(PlayerNet pn, Instance it, ChestDef c)
        {
            Profile pr = pn.S.P;
            if (pr.level + 6 < c.Grade * 4) { Toast(pn, "This " + Items.ChestGrades[c.Grade] + " Chest is locked. Come back stronger (level " + Mathf.Max(1, c.Grade * 4 - 6) + "+)."); return; }
            it.ChestBackAt[c.Id] = Time.time + 360f;
            WorldEvt(it, 6, c.Id, 0, 0, 0);
            GrantChest(pn, c.Grade, c.Pos);
        }

        void GrantChest(PlayerNet pn, int grade, Vector3 pos)
        {
            Profile pr = pn.S.P;
            int gold; List<KeyValuePair<string, int>> loot = Loot.RollChest(grade, rng, out gold);
            pr.gold += gold; pr.chestsOpened++;
            StringBuilder b = new StringBuilder(Items.ChestGrades[grade] + " Chest:  " + gold + " gold");
            for (int i = 0; i < loot.Count; i++) { pr.Add(loot[i].Key, loot[i].Value); b.Append(",  " + Items.Name(loot[i].Key) + " x" + loot[i].Value); }
            if (loot.Count == 0) b.Append("  (nothing else inside)");
            Fx(pn.S.In, 9, pos, Vector3.up, 4);
            pn.InteractClientRpc(3, Items.ChestGrades[grade], b.ToString(), To(pn));
            Event(pn, ObjType.Chest, "any", 1);
            CheckMilestones(pn); SendState(pn);
        }

        void UsePortal(PlayerNet pn, Instance it)
        {
            Profile pr = pn.S.P;
            if (it.Zone == 1) { Toast(pn, "Returning to Dawnrest Village..."); Enter(pn, 0, false); return; }
            if (!pr.promoted) { Toast(pn, "The portal is sealed. Complete your promotion quest first."); return; }
            if (pr.Count("kingdom_token") < 1) { Toast(pn, "You need a Kingdom Token to use this portal."); return; }
            pr.Remove("kingdom_token", 1);
            Toast(pn, "The portal carries you to Aurelion Kingdom!");
            Enter(pn, 1, false);
        }

        // ============================================================ services
        public void OnCraft(PlayerNet pn, string recipeId)
        {
            ServerPlayer s = pn.S; if (s == null || !s.Ready || s.Dead) return;
            Recipe r = Recipes.Get(recipeId); if (r == null) return;
            Profile pr = s.P;
            bool near = false; Vector3 p = pn.transform.position;
            for (int i = 0; i < s.In.L.Npcs.Count; i++) { NpcDef n = s.In.L.Npcs[i]; if (n.Role == r.Station && Vector3.Distance(p, n.Pos) < 7f) near = true; }
            if (!near) { Toast(pn, "You must be near the " + r.Station + "."); return; }
            if (pr.level < r.Req) { Toast(pn, "Requires level " + r.Req + "."); return; }
            if (pr.gold < r.Gold) { Toast(pn, "Not enough gold."); return; }
            for (int i = 0; i < r.Mats.Length; i++) if (pr.Count(r.Mats[i]) < r.Counts[i]) { Toast(pn, "Missing materials."); return; }
            for (int i = 0; i < r.Mats.Length; i++) pr.Remove(r.Mats[i], r.Counts[i]);
            pr.gold -= r.Gold; pr.Add(r.Result, r.Qty); pr.crafted++;
            Event(pn, ObjType.Craft, r.Result, r.Qty);
            Toast(pn, "Crafted " + Items.Name(r.Result) + " x" + r.Qty);
            Fx(s.In, 9, pn.transform.position, Vector3.up, 1);
            CheckMilestones(pn); SendState(pn);
        }

        public void OnEquip(PlayerNet pn, string itemId)
        {
            ServerPlayer s = pn.S; if (s == null || !s.Ready) return;
            ItemDef d = Items.Get(itemId); Profile pr = s.P;
            if (d == null || !d.Equippable || pr.Count(itemId) < 1) return;
            if (pr.level < d.Req) { Toast(pn, "Requires level " + d.Req + "."); return; }
            if (d.Slot == 0) pr.weapon = itemId; else if (d.Slot == 1) pr.armor = itemId; else pr.accessory = itemId;
            RecalcStats(pn); SendState(pn);
        }

        public void OnUseItem(PlayerNet pn, string itemId)
        {
            ServerPlayer s = pn.S; if (s == null || !s.Ready || s.Dead) return;
            ItemDef d = Items.Get(itemId); Profile pr = s.P;
            if (d == null || pr.Count(itemId) < 1) return;
            if (d.Kind == ItemKind.Potion)
            {
                pr.Remove(itemId, 1);
                s.Hp = Mathf.Min(s.St.MaxHp, s.Hp + d.Heal); s.Mp = Mathf.Min(s.St.MaxMp, s.Mp + d.Mana);
                Fx(s.In, 5, pn.transform.position, Vector3.up, d.Heal > 0 ? 5 : 0);
            }
            else if (d.Kind == ItemKind.Pill)
            {
                if (pr.level < 1 || pr.level >= 10) { Toast(pn, "The LV Up Pill only works from level 1 to 9."); return; }
                pr.Remove(itemId, 1); pr.pillsUsed++; pr.xp = 0; pr.level++;
                OnLevelUp(pn); CheckMilestones(pn);
            }
            else if (d.Kind == ItemKind.Chest)
            {
                int g = 0; int.TryParse(itemId.Substring(6), out g);
                pr.Remove(itemId, 1); GrantChest(pn, Mathf.Clamp(g, 0, 10), pn.transform.position); return;
            }
            else return;
            SendState(pn);
        }

        public void OnClaim(PlayerNet pn, int what)
        {
            ServerPlayer s = pn.S; if (s == null || !s.Ready) return;
            Profile pr = s.P;
            if (what == 0)
            {
                if (!ProfileStore.PillAvailable) { Toast(pn, "The LV Up Pill is not available yet."); return; }
                if (pr.level < 1 || pr.level >= 10) { Toast(pn, "The LV Up Pill is for levels 1-9."); return; }
                long today = DateTime.UtcNow.Ticks / TimeSpan.TicksPerDay;
                if (pr.lastDaily == today) { Toast(pn, "You already claimed today's pill."); return; }
                pr.lastDaily = today; pr.Add("levelup_pill", 1);
                Toast(pn, "Received an LV Up Pill!");
                SendState(pn);
            }
        }

        public void OnEmote(PlayerNet pn, int id)
        {
            ServerPlayer s = pn.S; if (s == null || !s.Ready || s.Dead) return;
            if (Time.time - s.LastEmote < 1.5f) return; s.LastEmote = Time.time;
            Anim(pn, 7 + Mathf.Clamp(id, 0, 2));
            for (int i = 0; i < s.In.Players.Count; i++)
            {
                PlayerNet o = s.In.Players[i];
                if (o != pn && Vector3.Distance(o.transform.position, pn.transform.position) < 10f) { Event(pn, ObjType.Emote, "near", 1); SendState(pn); break; }
            }
        }

        public void OnSell(PlayerNet pn, string itemId, int n)
        {
            ServerPlayer s = pn.S; if (s == null || !s.Ready) return;
            ItemDef d = Items.Get(itemId); if (d == null || n < 1 || s.P.Count(itemId) < n) return;
            if (itemId == s.P.weapon || itemId == s.P.armor || itemId == s.P.accessory) { if (s.P.Count(itemId) <= n) return; }
            if (d.Kind == ItemKind.Token || d.Kind == ItemKind.Pill) return;
            s.P.Remove(itemId, n); s.P.gold += Mathf.Max(1, d.Value / 2) * n;
            SendState(pn);
        }

        public void OnChooseElement(PlayerNet pn, int element)
        {
            ServerPlayer s = pn.S; if (s == null || !s.Ready) return;
            if (s.P.element >= 0 || element < 0 || element > 9 || s.P.level < 3) return;
            s.P.element = element; AutoLoadout(s.P); RecalcStats(pn);
            Event(pn, ObjType.Choose, "element", 1);
            Toast(pn, "You awaken the " + ElementInfo.God(element) + " inheritance!");
            Fx(s.In, 8, pn.transform.position, Vector3.up, element);
            CheckMilestones(pn); SendState(pn);
        }

        public void OnLoadout(PlayerNet pn, string skillId, bool equip)
        {
            ServerPlayer s = pn.S; if (s == null || !s.Ready) return;
            SkillDef d = Skills.Get(skillId); if (d == null || d.Level > s.P.level) return;
            if (d.Element >= 0 && d.Element != s.P.element) return;
            bool has = s.P.loadout.Contains(skillId);
            if (equip && !has) { if (s.P.loadout.Count >= Skills.MaxLoadout) { Toast(pn, "You can equip at most " + Skills.MaxLoadout + " skills."); return; } s.P.loadout.Add(skillId); }
            else if (!equip && has) s.P.loadout.Remove(skillId);
            SendState(pn);
        }

        public void OnQuest(PlayerNet pn, string questId, int op)
        {
            ServerPlayer s = pn.S; if (s == null || !s.Ready) return;
            QuestDef q = Quests.Get(questId); if (q == null) return;
            Profile pr = s.P;
            if (op == 0)
            {
                if (!Rules.Available(pr, q)) return;
                if (!NearGiver(pn, q)) { Toast(pn, "Speak to " + q.Giver + " to accept this quest."); return; }
                QuestProg qp = Rules.Ensure(pr, q); qp.accepted = true;
                Event(pn, ObjType.Talk, q.Giver, 0);
                Toast(pn, "Quest accepted: " + q.Title);
            }
            else if (op == 1)
            {
                QuestProg qp = pr.Q(q.Id);
                if (qp == null || !qp.accepted && !q.Hidden || qp.done) return;
                if (!Rules.Complete(pr, q)) { Toast(pn, "Quest objectives are not complete."); return; }
                if (!q.Hidden && !NearGiver(pn, q)) { Toast(pn, "Return to " + q.Giver + " to turn in this quest."); return; }
                if (q.Consume) for (int i = 0; i < q.Objs.Count; i++) if (q.Objs[i].Type == ObjType.Gather) pr.Remove(q.Objs[i].Target, q.Objs[i].Count);
                qp.done = true; qp.accepted = false;
                pr.gold += q.Gold;
                for (int i = 0; i < q.Rewards.Count; i++) pr.Add(q.Rewards[i].id, q.Rewards[i].n);
                if (q.Promotion)
                {
                    if (!pr.promoted) { pr.promoted = true; ProfileStore.Data.promotedCount++; ProfileStore.Save(true); }
                    Toast(pn, "PROMOTION COMPLETE! The village portal now opens with your Kingdom Token.");
                }
                Toast(pn, "Quest complete: " + q.Title + "  +" + q.Xp + " XP  +" + q.Gold + "g");
                AddXp(pn, q.Xp);
            }
            else if (op == 2)
            {
                QuestProg qp = pr.Q(q.Id);
                if (qp != null && qp.accepted && !qp.done) { qp.accepted = false; qp.p = new int[q.Objs.Count]; }
            }
            SendState(pn);
        }

        bool NearGiver(PlayerNet pn, QuestDef q)
        {
            if (string.IsNullOrEmpty(q.Giver)) return true;
            Instance it = pn.S.In;
            for (int i = 0; i < it.L.Npcs.Count; i++) if (it.L.Npcs[i].Id == q.Giver && Vector3.Distance(it.L.Npcs[i].Pos, pn.transform.position) < 6f) return true;
            return false;
        }
    }
}
