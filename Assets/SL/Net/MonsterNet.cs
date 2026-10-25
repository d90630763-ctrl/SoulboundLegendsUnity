using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using SL.Core;
using SL.Actors;
using SL.Gameplay;

namespace SL.Net
{
    /// <summary>Networked monster. The server runs the AI (ServerSim); clients only render it.</summary>
    public sealed class MonsterNet : NetworkBehaviour
    {
        public static readonly List<MonsterNet> All = new List<MonsterNet>();
        public NetworkVariable<Vector3> NPos = new NetworkVariable<Vector3>(Vector3.zero);
        public NetworkVariable<float> NYaw = new NetworkVariable<float>(0f);
        public NetworkVariable<int> NType = new NetworkVariable<int>(-1), NHp = new NetworkVariable<int>(1), NMaxHp = new NetworkVariable<int>(1), NZone = new NetworkVariable<int>(0), NAnim = new NetworkVariable<int>(0);
        public NetworkVariable<bool> NDead = new NetworkVariable<bool>(false);

        // server-only
        public MonsterDef Def; public Instance In; public Vector3 Home; public float Hp, NextAtk, RespawnAt, WanderT, SlamAt, NextSlam, LastHit, Speed;
        public PlayerNet Target; public Vector2 WanderDir; public int SpawnIndex; public bool Enraged, Dying;
        public PlayerNet LastAttacker;

        CreatureRig rig; GameObject visual; int builtType = -2; Vector3 lastPos; float spd; int lastAnim = -1; bool builtDead;
        public MonsterDef ClientDef { get { return NType.Value < 0 || NType.Value >= Monsters.All.Count ? null : Monsters.All[NType.Value]; } }

        public override void OnNetworkSpawn() { All.Add(this); transform.position = NPos.Value; }
        public override void OnNetworkDespawn() { All.Remove(this); }

        void Update()
        {
            if (!IsSpawned || !NetworkManager.Singleton.IsClient) return;
            float dt = Time.deltaTime;
            int t = NType.Value;
            if (t >= 0 && t != builtType)
            {
                builtType = t;
                if (visual != null) Destroy(visual);
                MonsterDef d = Monsters.All[Mathf.Clamp(t, 0, Monsters.All.Count - 1)];
                CreatureStyle st = new CreatureStyle { Plan = d.Plan, Scale = d.Scale, ColA = d.ColA, ColB = d.ColB, ColC = d.ColC, Ranged = d.Ranged, Flying = d.Flying, Boss = d.Boss, Id = d.Id };
                visual = CreatureBuilder.Build(st);
                visual.transform.SetParent(transform, false);
                rig = visual.GetComponent<CreatureRig>();
            }
            if (IsServer) { /* server writes transform directly */ }
            else transform.position = Vector3.Lerp(transform.position, NPos.Value, 1f - Mathf.Exp(-12f * dt));
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, NYaw.Value, 0f), 1f - Mathf.Exp(-10f * dt));
            Vector3 dv = transform.position - lastPos; dv.y = 0f; lastPos = transform.position;
            spd = Mathf.Lerp(spd, dt > 0f ? dv.magnitude / dt : 0f, 1f - Mathf.Exp(-10f * dt));
            if (visual == null) return;
            bool show = PlayerNet.Local == null || PlayerNet.Local.NZone.Value == NZone.Value;
            if (visual.activeSelf != show) visual.SetActive(show);
            if (rig == null) return;
            rig.SetMove(Mathf.Clamp01(spd / 5f));
            if (NDead.Value != builtDead) { builtDead = NDead.Value; rig.SetDead(builtDead); }
            int a = NAnim.Value;
            if (a != lastAnim) { if (lastAnim != -1) { if ((a & 15) == 1) rig.Attack(); else if ((a & 15) == 2) rig.Hit(); } lastAnim = a; }
        }
    }
}
