using System;
using System.Reflection;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using SL.Core;

namespace SL.Net
{
    /// <summary>Creates the NetworkManager at runtime (no scene setup needed) and starts host / client / dedicated server.</summary>
    public sealed class NetBootstrap : MonoBehaviour
    {
        public static NetBootstrap I;
        public static GameObject PlayerPrefab, MonsterPrefab;
        public static string Address = "127.0.0.1";
        public static ushort Port = 7777;
        public static event Action Connected, Disconnected;
        public NetworkManager Nm; UnityTransport utp;
        public bool IsRunning { get { return Nm != null && Nm.IsListening; } }

        public static NetBootstrap Create()
        {
            if (I != null) return I;
            GameObject g = new GameObject("SL_Network");
            g.SetActive(false);
            DontDestroyOnLoad(g);
            I = g.AddComponent<NetBootstrap>();
            I.utp = g.AddComponent<UnityTransport>();
            I.Nm = g.AddComponent<NetworkManager>();
            NetworkConfig cfg = new NetworkConfig();
            cfg.NetworkTransport = I.utp; cfg.EnableSceneManagement = false; cfg.ForceSamePrefabs = false; cfg.ConnectionApproval = false; cfg.TickRate = 30;
            I.Nm.NetworkConfig = cfg;
            g.SetActive(true);
            I.LoadPrefabs();
            I.Nm.OnClientConnectedCallback += I.OnConnected;
            I.Nm.OnClientDisconnectCallback += I.OnDisconnected;
            return I;
        }

        void LoadPrefabs()
        {
            PlayerPrefab = Resources.Load<GameObject>("SL/Net/PlayerNet");
            MonsterPrefab = Resources.Load<GameObject>("SL/Net/MonsterNet");
            if (PlayerPrefab == null) { SLLog.W("PlayerNet prefab asset missing - using runtime fallback."); PlayerPrefab = RuntimePrefab("PlayerNet", typeof(PlayerNet)); }
            if (MonsterPrefab == null) { SLLog.W("MonsterNet prefab asset missing - using runtime fallback."); MonsterPrefab = RuntimePrefab("MonsterNet", typeof(MonsterNet)); }
            Nm.AddNetworkPrefab(PlayerPrefab);
            Nm.AddNetworkPrefab(MonsterPrefab);
        }

        static GameObject RuntimePrefab(string name, Type comp)
        {
            GameObject g = new GameObject(name);
            g.SetActive(false);
            DontDestroyOnLoad(g);
            NetworkObject no = g.AddComponent<NetworkObject>();
            g.AddComponent(comp);
            uint hash = 17;
            for (int i = 0; i < name.Length; i++) hash = hash * 31u + name[i];
            FieldInfo f = typeof(NetworkObject).GetField("GlobalObjectIdHash", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null) f.SetValue(no, hash); else SLLog.E("Could not set GlobalObjectIdHash on runtime prefab.");
            return g;
        }

        /// <summary>Instantiate a registered network prefab (handles the inactive runtime-fallback case).</summary>
        public static GameObject Make(GameObject prefab, Vector3 pos)
        {
            GameObject go = Instantiate(prefab, pos, Quaternion.identity);
            if (!go.activeSelf) go.SetActive(true);
            return go;
        }

        void OnConnected(ulong id)
        {
            if (Nm.IsServer)
            {
                GameObject go = Make(PlayerPrefab, Vector3.zero);
                go.GetComponent<NetworkObject>().SpawnAsPlayerObject(id);
            }
            if (id == Nm.LocalClientId && Connected != null) Connected();
        }

        void OnDisconnected(ulong id)
        {
            if (Nm != null && !Nm.IsServer && id == Nm.LocalClientId && Disconnected != null) Disconnected();
        }

        public bool StartHost()
        {
            ServerSim.Create();
            utp.SetConnectionData("127.0.0.1", Port, "0.0.0.0");
            return Nm.StartHost();
        }

        public bool StartServer()
        {
            ServerSim.Create();
            utp.SetConnectionData("0.0.0.0", Port, "0.0.0.0");
            return Nm.StartServer();
        }

        public bool StartClient(string address)
        {
            if (string.IsNullOrEmpty(address)) address = "127.0.0.1";
            Address = address;
            utp.SetConnectionData(address, Port);
            return Nm.StartClient();
        }

        public void Stop()
        {
            if (Nm != null && Nm.IsListening) Nm.Shutdown();
        }
    }
}
