using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SL.Core;
using SL.World;
using SL.Gameplay;

namespace SL.Net
{
    /// <summary>One running copy of a zone (village or kingdom). Players are spread over instances so many can play at once.</summary>
    public sealed class Instance
    {
        public int Zone, Id;
        public ZoneLayout L; public HeightField Hf;
        public readonly List<PlayerNet> Players = new List<PlayerNet>();
        public readonly List<MonsterNet> Monsters = new List<MonsterNet>();
        public readonly HashSet<int> DestroyedBlocks = new HashSet<int>();
        public readonly Dictionary<int, float> BlockHp = new Dictionary<int, float>();
        public readonly Dictionary<int, float> BlockRestoreAt = new Dictionary<int, float>();
        public readonly Dictionary<int, float> NodeBackAt = new Dictionary<int, float>();
        public readonly Dictionary<int, float> ChestBackAt = new Dictionary<int, float>();
        public float LastCrater, EmptySince = -1f;
        public int Key { get { return Zone * 1000 + Id; } }
        public int Capacity { get { return L.Def.Capacity; } }
        public bool Full { get { return Players.Count >= Capacity; } }

        public Instance(int zone, int id)
        {
            Zone = zone; Id = id; L = ZoneLayout.Get(zone); Hf = new HeightField(L.Def);
        }
    }

    public sealed class InstanceManager
    {
        public readonly List<Instance> All = new List<Instance>();

        public Instance Find(int key) { for (int i = 0; i < All.Count; i++) if (All[i].Key == key) return All[i]; return null; }

        /// <summary>Villages fill the first open instance; kingdoms spread players evenly so no city is crowded.</summary>
        public Instance Pick(int zone, out bool created)
        {
            created = false;
            Instance best = null; int count = 0;
            for (int i = 0; i < All.Count; i++)
            {
                Instance it = All[i];
                if (it.Zone != zone) continue;
                count++;
                if (it.Full) continue;
                if (best == null) { best = it; if (zone == 0) break; }
                else if (zone == 1 && it.Players.Count < best.Players.Count) best = it;
            }
            if (best == null) { best = new Instance(zone, count + 1); All.Add(best); created = true; }
            return best;
        }
    }

    [Serializable] public class StoreData { public List<Profile> profiles = new List<Profile>(); public List<string> worldFirsts = new List<string>(); public int promotedCount; }

    /// <summary>Simple JSON persistence for the host. A real database server replaces this later.</summary>
    public static class ProfileStore
    {
        static StoreData data;
        static float lastSave;
        static string Path { get { return System.IO.Path.Combine(Application.persistentDataPath, "sl_server.json"); } }

        public static void Load()
        {
            data = null;
            try { if (File.Exists(Path)) data = JsonUtility.FromJson<StoreData>(File.ReadAllText(Path)); } catch (Exception e) { SLLog.W("store load failed: " + e.Message); }
            if (data == null) data = new StoreData();
        }

        public static StoreData Data { get { if (data == null) Load(); return data; } }

        public static Profile Get(string name)
        {
            for (int i = 0; i < Data.profiles.Count; i++) if (Data.profiles[i].name == name) return Data.profiles[i];
            return null;
        }

        public static Profile GetOrCreate(string name, uint look)
        {
            Profile p = Get(name);
            if (p == null) { p = Profile.Fresh(name, look); Data.profiles.Add(p); Save(true); }
            return p;
        }

        public static void Save(bool force)
        {
            if (!force && Time.realtimeSinceStartup - lastSave < 10f) return;
            lastSave = Time.realtimeSinceStartup;
            try { File.WriteAllText(Path, JsonUtility.ToJson(Data)); } catch (Exception e) { SLLog.W("store save failed: " + e.Message); }
        }

        public const int PillPromotedThreshold = 5;
        public static bool PillAvailable { get { return Data.promotedCount >= PillPromotedThreshold; } }

        /// <summary>Returns true the first time anyone in the world earns this milestone.</summary>
        public static bool ClaimWorldFirst(string id)
        {
            if (Data.worldFirsts.Contains(id)) return false;
            Data.worldFirsts.Add(id); Save(true); return true;
        }
    }
}
