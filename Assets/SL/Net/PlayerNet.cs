using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using SL.Core;
using SL.Actors;
using SL.Gameplay;
using SL.World;

namespace SL.Net
{
    /// <summary>Server-only state of one connected player.</summary>
    public sealed class ServerPlayer
    {
        public Profile P; public Instance In; public bool Ready;
        public Vector2 Move; public float Yaw; public bool Sprint, Block;
        public float Hp, Mp, Shield, ShieldUntil, InvulnUntil, NextAttack, NextDodge, DeadUntil, LastHurt, LastEmote, SaveAt, NextTick;
        public bool Dead; public Stats St;
        public readonly Dictionary<string, float> Cd = new Dictionary<string, float>();
        public int AnimCounter;
        public float LastRegen;
    }

    public static class ClientIdentity { public static string Name = "Adventurer"; public static uint Look; }

    /// <summary>Networked player. Server simulates; clients display and send intent.</summary>
    public sealed class PlayerNet : NetworkBehaviour
    {
        public static PlayerNet Local;
        public static readonly List<PlayerNet> All = new List<PlayerNet>();

        public NetworkVariable<Vector3> NPos = new NetworkVariable<Vector3>(Vector3.zero);
        public NetworkVariable<float> NYaw = new NetworkVariable<float>(0f);
        public NetworkVariable<int> NHp = new NetworkVariable<int>(100), NMaxHp = new NetworkVariable<int>(100), NMp = new NetworkVariable<int>(50), NMaxMp = new NetworkVariable<int>(50);
        public NetworkVariable<int> NLevel = new NetworkVariable<int>(0), NElement = new NetworkVariable<int>(-1), NZone = new NetworkVariable<int>(0), NAnim = new NetworkVariable<int>(0), NShield = new NetworkVariable<int>(0);
        public NetworkVariable<bool> NDead = new NetworkVariable<bool>(false), NBlock = new NetworkVariable<bool>(false);
        public NetworkVariable<uint> NLook = new NetworkVariable<uint>(0u);
        public NetworkVariable<FixedString64Bytes> NName = new NetworkVariable<FixedString64Bytes>();
        public NetworkVariable<FixedString32Bytes> NWeapon = new NetworkVariable<FixedString32Bytes>();

        public float LocalYaw;
        public ServerPlayer S;
        public Profile Prof; public bool PillAvail;
        public CharacterRig Rig; GameObject visual;
        int lastAnim = -1; uint builtLook; int builtElem = -99; string builtWeapon = ""; bool builtDead; Vector3 lastPos; float speedSmooth; bool hidden;

        public static event Action<string> ToastEvent;
        public static event Action ProfileEvent;
        public static event Action<int, string, string> InteractEvent;
        public static event Action<int, int, Vector3, string> ZoneEvent;

        public string DisplayName { get { return NName.Value.ToString(); } }
        public bool InMyInstance { get { return Local == null || NZone.Value == Local.NZone.Value; } }

        public override void OnNetworkSpawn()
        {
            All.Add(this);
            if (IsServer) S = new ServerPlayer();
            if (IsOwner)
            {
                Local = this;
                HelloServerRpc(ClientIdentity.Name, ClientIdentity.Look);
            }
            transform.position = NPos.Value;
        }

        public override void OnNetworkDespawn()
        {
            All.Remove(this);
            if (Local == this) Local = null;
            if (IsServer && ServerSim.I != null) ServerSim.I.OnLeave(this);
        }

        // ------------------------------------------------------------ client presentation
        void EnsureVisual()
        {
            string name = NName.Value.ToString();
            if (name.Length == 0) return;
            if (!NetworkManager.Singleton.IsClient) return;
            if (visual == null || builtLook != NLook.Value || builtElem != NElement.Value)
            {
                if (visual != null) Destroy(visual);
                builtLook = NLook.Value; builtElem = NElement.Value; builtWeapon = "";
                visual = CharacterBuilder.Build(Look.Unpack(builtLook), builtElem, name);
                visual.transform.SetParent(transform, false);
                Rig = visual.GetComponent<CharacterRig>();
                Rig.SetGlow(ElementInfo.Col(builtElem < 0 ? 4 : builtElem));
                builtDead = false; lastAnim = -1;
            }
            string w = NWeapon.Value.ToString();
            if (w != builtWeapon && Rig != null)
            {
                builtWeapon = w;
                ItemDef d = Items.Get(w);
                Rig.SetWeapon(d == null ? WeaponType.Sword : d.Wt, d == null ? 0 : d.Rarity, ElementInfo.Col(builtElem < 0 ? 4 : builtElem));
            }
        }

        void Update()
        {
            if (!IsSpawned) return;
            float dt = Time.deltaTime;
            EnsureVisual();
            bool authoritativeHere = IsServer;
            Vector3 target = NPos.Value;
            if (authoritativeHere) { if (S != null && S.In != null) target = transform.position; }
            else if (IsOwner)
            {
                float err = (transform.position - target).magnitude;
                if (err > 6f) transform.position = target;
                else if (err > 0.35f) transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-4f * dt));
            }
            else transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-14f * dt));

            float yaw = (IsOwner && !IsServer) ? LocalYaw : NYaw.Value;
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, 1f - Mathf.Exp(-14f * dt));

            Vector3 d = transform.position - lastPos; d.y = 0f;
            float spd = dt > 0f ? d.magnitude / dt : 0f; lastPos = transform.position;
            speedSmooth = Mathf.Lerp(speedSmooth, spd, 1f - Mathf.Exp(-12f * dt));
            hidden = !InMyInstance;
            if (visual != null)
            {
                if (visual.activeSelf == hidden) visual.SetActive(!hidden);
                if (Rig != null)
                {
                    Rig.SetMove(Mathf.Clamp01(speedSmooth / 6.5f), speedSmooth > 6.2f);
                    Rig.SetBlock(NBlock.Value);
                    if (NDead.Value != builtDead) { builtDead = NDead.Value; Rig.SetDead(builtDead); }
                    int a = NAnim.Value;
                    if (a != lastAnim)
                    {
                        if (lastAnim != -1 && (a & 15) != 0) Rig.Play(AnimOf(a & 15), (a & 15) >= 7 ? (a & 15) - 7 : 0);
                        lastAnim = a;
                    }
                }
            }
        }

        static Anim AnimOf(int id)
        {
            switch (id) { case 1: return Anim.Attack; case 2: return Anim.Heavy; case 3: return Anim.Cast; case 4: return Anim.Dodge; case 5: return Anim.Hit; case 6: return Anim.Jump; default: return Anim.Emote; }
        }

        // ------------------------------------------------------------ owner -> server
        float nextInputSend;
        public void SendInput(Vector2 move, float yaw, bool sprint, bool block)
        {
            if (Time.time < nextInputSend) return;
            nextInputSend = Time.time + 0.05f;
            InputServerRpc(move, yaw, sprint, block);
        }

        [ServerRpc] void HelloServerRpc(string name, uint look) { if (ServerSim.I != null) ServerSim.I.OnHello(this, name, look); }
        [ServerRpc] void InputServerRpc(Vector2 move, float yaw, bool sprint, bool block) { if (S != null && S.Ready) { S.Move = Vector2.ClampMagnitude(move, 1f); S.Yaw = yaw; S.Sprint = sprint; S.Block = block; } }
        [ServerRpc] public void ActionServerRpc(int action, float yaw) { if (ServerSim.I != null) ServerSim.I.OnAction(this, action, yaw); }
        [ServerRpc] public void CastServerRpc(string skillId, float yaw) { if (ServerSim.I != null) ServerSim.I.OnCast(this, skillId, yaw); }
        [ServerRpc] public void InteractServerRpc() { if (ServerSim.I != null) ServerSim.I.OnInteract(this); }
        [ServerRpc] public void CraftServerRpc(string recipeId) { if (ServerSim.I != null) ServerSim.I.OnCraft(this, recipeId); }
        [ServerRpc] public void EquipServerRpc(string itemId) { if (ServerSim.I != null) ServerSim.I.OnEquip(this, itemId); }
        [ServerRpc] public void UseItemServerRpc(string itemId) { if (ServerSim.I != null) ServerSim.I.OnUseItem(this, itemId); }
        [ServerRpc] public void QuestServerRpc(string questId, int op) { if (ServerSim.I != null) ServerSim.I.OnQuest(this, questId, op); }
        [ServerRpc] public void ChooseElementServerRpc(int element) { if (ServerSim.I != null) ServerSim.I.OnChooseElement(this, element); }
        [ServerRpc] public void LoadoutServerRpc(string skillId, bool equip) { if (ServerSim.I != null) ServerSim.I.OnLoadout(this, skillId, equip); }
        [ServerRpc] public void ClaimServerRpc(int what) { if (ServerSim.I != null) ServerSim.I.OnClaim(this, what); }
        [ServerRpc] public void EmoteServerRpc(int id) { if (ServerSim.I != null) ServerSim.I.OnEmote(this, id); }
        [ServerRpc] public void RespawnServerRpc() { if (ServerSim.I != null) ServerSim.I.OnRespawn(this); }
        [ServerRpc] public void SellServerRpc(string itemId, int n) { if (ServerSim.I != null) ServerSim.I.OnSell(this, itemId, n); }

        // ------------------------------------------------------------ server -> clients
        [ClientRpc] public void StateClientRpc(string json, bool pill, ClientRpcParams p = default)
        {
            if (!IsOwner) return;
            Profile np = Profile.FromJson(json);
            if (np == null) return;
            Prof = np; PillAvail = pill;
            if (ProfileEvent != null) ProfileEvent();
        }
        [ClientRpc] public void ToastClientRpc(string msg, ClientRpcParams p = default) { if (ToastEvent != null) ToastEvent(msg); }
        [ClientRpc] public void FxClientRpc(int fx, Vector3 pos, Vector3 dir, int element, ClientRpcParams p = default) { if (ClientWorld.I != null) ClientWorld.I.Fx(fx, pos, dir, element); }
        [ClientRpc] public void FloatClientRpc(Vector3 pos, string text, int kind, ClientRpcParams p = default) { if (ClientWorld.I != null) ClientWorld.I.FloatText(pos, text, kind); }
        [ClientRpc] public void InteractClientRpc(int kind, string id, string extra, ClientRpcParams p = default) { if (InteractEvent != null) InteractEvent(kind, id, extra); }
        [ClientRpc] public void ZoneClientRpc(int zone, int inst, Vector3 pos, string sync, ClientRpcParams p = default)
        {
            if (!IsOwner) return;
            if (ZoneEvent != null) ZoneEvent(zone, inst, pos, sync);
        }
        [ClientRpc] public void WorldEvtClientRpc(int kind, int id, float a, float b, float c, ClientRpcParams p = default) { if (ClientWorld.I != null) ClientWorld.I.WorldEvent(kind, id, a, b, c); }
    }
}
