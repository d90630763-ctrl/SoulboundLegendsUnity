using System;
using System.Collections.Generic;
using UnityEngine;
using SL.Core;

namespace SL.World
{
    public sealed class ZoneDef
    {
        public int Id; public string Name; public int Seed;
        public float Radius, PlazaR, WaterY, RiverBase, RiverAmp, RiverFreq, RiverW;
        public int Capacity;

        static ZoneDef village, kingdom;
        public static ZoneDef Get(int id)
        {
            if (id == 1)
            {
                if (kingdom == null) kingdom = new ZoneDef { Id = 1, Name = "Aurelion Kingdom", Seed = 7001, Radius = 150f, PlazaR = 46f, WaterY = 0f, RiverBase = 95f, RiverAmp = 14f, RiverFreq = 0.016f, RiverW = 8f, Capacity = 24 };
                return kingdom;
            }
            if (village == null) village = new ZoneDef { Id = 0, Name = "Dawnrest Village", Seed = 1337, Radius = 115f, PlazaR = 26f, WaterY = 0f, RiverBase = 50f, RiverAmp = 14f, RiverFreq = 0.02f, RiverW = 6f, Capacity = 12 };
            return village;
        }
    }

    public struct Crater { public float x, z, r, d; }

    /// <summary>Deterministic terrain height. The same code runs on the server (per instance) and on clients.</summary>
    public sealed class HeightField
    {
        public readonly ZoneDef Z;
        public readonly List<Crater> Craters = new List<Crater>();
        public HeightField(ZoneDef z) { Z = z; }

        public float RiverCentre(float x) { return Z.RiverBase + Mathf.Sin(x * Z.RiverFreq + Z.Seed * 0.1f) * Z.RiverAmp; }
        public float RiverDist(float x, float z) { return Mathf.Abs(z - RiverCentre(x)); }

        public float Base(float x, float z)
        {
            float r = Mathf.Sqrt(x * x + z * z);
            int s = Z.Seed;
            float h = 3.2f + SLMath.Fbm(x * 0.010f, z * 0.010f, s, 4) * 11f + SLMath.Fbm(x * 0.05f, z * 0.05f, s + 7, 3) * 2.0f;
            float mount = SLMath.Smooth(Z.Radius * 0.66f, Z.Radius * 0.98f, r);
            h += mount * (24f + SLMath.Fbm(x * 0.03f, z * 0.03f, s + 3, 3) * 34f);
            float flat = 1f - SLMath.Smooth(Z.PlazaR, Z.PlazaR + 22f, r);
            h = Mathf.Lerp(h, 2.4f, flat);
            float rd = RiverDist(x, z);
            float carve = 1f - SLMath.Smooth(Z.RiverW * 0.45f, Z.RiverW + 10f, rd);
            float edge = 1f - SLMath.Smooth(Z.Radius * 0.88f, Z.Radius * 0.98f, r);
            h = Mathf.Lerp(h, Z.WaterY - 1.8f, carve * carve * edge * (1f - flat));
            return h;
        }

        public float At(float x, float z)
        {
            float h = Base(x, z);
            for (int i = 0; i < Craters.Count; i++)
            {
                Crater c = Craters[i];
                float dx = x - c.x, dz = z - c.z;
                float d2 = dx * dx + dz * dz;
                if (d2 < c.r * c.r)
                {
                    float t = 1f - d2 / (c.r * c.r);
                    h -= c.d * t * t * (3f - 2f * t) ;
                }
            }
            return h;
        }

        public bool IsWater(float x, float z) { return At(x, z) < Z.WaterY - 0.05f; }
        public bool IsDeep(float x, float z) { return At(x, z) < Z.WaterY - 1.1f; }

        public void AddCrater(float x, float z, float r, float d)
        {
            if (Craters.Count >= 48) Craters.RemoveAt(0);
            Craters.Add(new Crater { x = x, z = z, r = r, d = d });
        }
    }

    public struct Bridge { public float x, z, hw, hd, y; }

    public struct Obstacle
    {
        public float x, z, hx, hz, yaw; public bool round; public int blockId;
    }

    public enum NodeKind { Tree, Ore, Crystal, Herb }
    public sealed class NodeDef { public int Id; public NodeKind Kind; public Vector3 Pos; }
    public sealed class BlockDef { public int Id; public Vector3 Pos; public Vector3 Size; public float Yaw; public float Hp; }
    public sealed class NpcDef { public string Id, Name, Role; public Vector3 Pos; public float Yaw; public int Look; }
    public sealed class BuildingDef { public string Kind; public Vector3 Pos; public float Yaw; public float W, D; public int Variant; }
    public sealed class ChestDef { public int Id, Grade; public Vector3 Pos; }
    public sealed class SpawnDef { public string Type; public Vector3 Pos; }

    /// <summary>Static content of a zone (buildings, NPCs, nodes, spawns). Pure data, no GameObjects.</summary>
    public sealed class ZoneLayout
    {
        public ZoneDef Def;
        public HeightField Base;
        public readonly List<Obstacle> Obstacles = new List<Obstacle>();
        public readonly List<NodeDef> Nodes = new List<NodeDef>();
        public readonly List<BlockDef> Blocks = new List<BlockDef>();
        public readonly List<NpcDef> Npcs = new List<NpcDef>();
        public readonly List<BuildingDef> Buildings = new List<BuildingDef>();
        public readonly List<ChestDef> Chests = new List<ChestDef>();
        public readonly List<SpawnDef> Spawns = new List<SpawnDef>();
        public readonly List<Vector2> Road = new List<Vector2>();
        public Vector3 SpawnPoint, PortalPos, BossPos;
        public readonly List<Bridge> Bridges = new List<Bridge>();

        /// <summary>Walking height: terrain, or a bridge deck where one exists.</summary>
        public float Ground(HeightField hf, float x, float z)
        {
            float h = hf.At(x, z);
            for (int i = 0; i < Bridges.Count; i++)
            {
                Bridge b = Bridges[i];
                if (Mathf.Abs(x - b.x) < b.hw && Mathf.Abs(z - b.z) < b.hd) return Mathf.Max(h, b.y);
            }
            return h;
        }

        public bool OnBridge(float x, float z)
        {
            for (int i = 0; i < Bridges.Count; i++)
            {
                Bridge b = Bridges[i];
                if (Mathf.Abs(x - b.x) < b.hw + 0.3f && Mathf.Abs(z - b.z) < b.hd) return true;
            }
            return false;
        }

        static readonly Dictionary<int, ZoneLayout> cache = new Dictionary<int, ZoneLayout>();
        public static ZoneLayout Get(int zone)
        {
            ZoneLayout l;
            if (!cache.TryGetValue(zone, out l)) { l = new ZoneLayout(zone); cache[zone] = l; }
            return l;
        }

        ZoneLayout(int zone)
        {
            Def = ZoneDef.Get(zone);
            Base = new HeightField(Def);
            if (zone == 1) GenKingdom(); else GenVillage();
            GenNodes();
            GenChests();
        }

        void GenChests()
        {
            DetRng rng = new DetRng(Def.Seed + 444);
            int[] grades; int count;
            if (Def.Id == 0) { count = 16; grades = new[] { 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 3, 3, 4 }; }
            else { count = 26; grades = new[] { 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 5, 5, 5, 6, 6, 7, 8, 9 }; }
            for (int i = 0; i < count; i++)
            {
                float lo = Def.Id == 0 ? 20f : 30f, hi = Def.Id == 0 ? 100f : 120f;
                float g = grades[i];
                Vector2 start = Polar(rng, lo + g * 6f, Mathf.Min(hi, lo + 30f + g * 10f), 0f, 360f);
                if (Def.Id == 0 && i == count - 1) start = new Vector2(BossPos.x + 4f, BossPos.z + 4f);
                Vector2 q = FindOpen(start, 3f);
                Chests.Add(new ChestDef { Id = i, Grade = grades[i], Pos = P(q.x, q.y) });
                Obstacles.Add(new Obstacle { x = q.x, z = q.y, hx = 0.7f, hz = 0.7f, round = true, blockId = -1 });
            }
        }

        // ---------- helpers ----------
        public float RoadDist(float x, float z)
        {
            float best = 999f;
            for (int i = 0; i < Road.Count - 1; i++)
            {
                Vector2 a = Road[i], b = Road[i + 1], p = new Vector2(x, z);
                Vector2 ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.001f, ab.sqrMagnitude));
                float d = (p - (a + ab * t)).magnitude;
                if (d < best) best = d;
            }
            return best;
        }

        Vector3 P(float x, float z) { return new Vector3(x, Base.At(x, z), z); }

        void Add(string kind, float x, float z, float yaw, float w, float d, int variant, bool solid)
        {
            Vector3 p = P(x, z);
            Buildings.Add(new BuildingDef { Kind = kind, Pos = p, Yaw = yaw, W = w, D = d, Variant = variant });
            if (solid) Obstacles.Add(new Obstacle { x = x, z = z, hx = w * 0.5f, hz = d * 0.5f, yaw = yaw * Mathf.Deg2Rad, round = false, blockId = -1 });
        }

        void Ring(string kind, float x, float z, float r)
        {
            Buildings.Add(new BuildingDef { Kind = kind, Pos = P(x, z), Yaw = 0f, W = r * 2f, D = r * 2f, Variant = 0 });
            Obstacles.Add(new Obstacle { x = x, z = z, hx = r, hz = r, yaw = 0f, round = true, blockId = -1 });
        }

        void Npc(string id, string name, string role, float x, float z, float yaw, int look)
        {
            Npcs.Add(new NpcDef { Id = id, Name = name, Role = role, Pos = P(x, z), Yaw = yaw, Look = look });
        }

        void Spawn(string type, float x, float z) { Spawns.Add(new SpawnDef { Type = type, Pos = P(x, z) }); }

        public bool Free(float x, float z, float margin)
        {
            float r = Mathf.Sqrt(x * x + z * z);
            if (r > Def.Radius * 0.84f) return false;
            float h = Base.At(x, z);
            if (h < Def.WaterY + 0.7f) return false;
            if (RoadDist(x, z) < 3f) return false;
            for (int i = 0; i < Obstacles.Count; i++)
            {
                Obstacle o = Obstacles[i];
                float m = Mathf.Max(o.hx, o.hz) + margin;
                if (Mathf.Abs(x - o.x) < m && Mathf.Abs(z - o.z) < m) return false;
            }
            return true;
        }

        // ---------- Village ----------
        void GenVillage()
        {
            float rc30 = Base.RiverCentre(30f);
            Road.Add(new Vector2(0f, 0f)); Road.Add(new Vector2(14f, rc30 * 0.45f)); Road.Add(new Vector2(30f, rc30 - 14f));
            Road.Add(new Vector2(30f, rc30 + 14f)); Road.Add(new Vector2(38f, rc30 + 40f));

            SpawnPoint = P(0f, 8f);
            Ring("fountain", 0f, 0f, 2.6f);
            PortalPos = P(0f, -17f);
            Buildings.Add(new BuildingDef { Kind = "portal", Pos = PortalPos, Yaw = 0f, W = 5f, D = 5f, Variant = 0 });
            Obstacles.Add(new Obstacle { x = 0f, z = -17f, hx = 3.2f, hz = 0.8f, yaw = 0f, round = false, blockId = -1 });

            Add("hall", -16f, 12f, 120f, 11f, 8f, 0, true);
            Add("smith", 16f, 12f, -120f, 9f, 7f, 0, true);
            Add("alchemist", 17f, -8f, -75f, 6f, 6f, 0, true);
            Add("inn", -17f, -10f, 75f, 12f, 8f, 0, true);
            DetRng rng = new DetRng(Def.Seed + 5);
            for (int i = 0; i < 9; i++)
            {
                float a = (i / 9f) * 360f + rng.Range(-12f, 12f);
                float d = rng.Range(33f, 44f);
                float x = Mathf.Cos(a * Mathf.Deg2Rad) * d, z = Mathf.Sin(a * Mathf.Deg2Rad) * d;
                if (!Free(x, z, 4f) || Base.RiverDist(x, z) < 14f) continue;
                Add("house", x, z, -a + 90f, rng.Range(6f, 8f), rng.Range(5.5f, 7f), i % 4, true);
            }
            Ring("well", 6f, -7f, 1.3f);
            Add("stall", -7f, 7f, 20f, 3.2f, 2.4f, 0, true);
            Add("stall", 8f, 7f, -20f, 3.2f, 2.4f, 1, true);
            Add("stall", 0f, 12.5f, 0f, 3.2f, 2.4f, 2, true);
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                Buildings.Add(new BuildingDef { Kind = "lamp", Pos = P(Mathf.Cos(a) * 10f, Mathf.Sin(a) * 10f), Variant = 0 });
            }
            float deck = Mathf.Max(Base.At(30f, rc30 - 13f), Base.At(30f, rc30 + 13f)) + 0.25f;
            Bridges.Add(new Bridge { x = 30f, z = rc30, hw = 2.5f, hd = 13f, y = deck });
            Buildings.Add(new BuildingDef { Kind = "bridge", Pos = new Vector3(30f, deck, rc30), Yaw = 0f, W = 5f, D = 26f, Variant = 0 });
            Buildings.Add(new BuildingDef { Kind = "lamp", Pos = P(33f, rc30 - 13f), Variant = 0 });

            // Trial grounds (promotion boss arena), north-west outskirts
            Vector2 tg = FindOpen(new Vector2(-62f, -48f), 14f);
            BossPos = P(tg.x, tg.y);
            Buildings.Add(new BuildingDef { Kind = "ruins", Pos = BossPos, Yaw = 0f, W = 22f, D = 22f, Variant = 0 });
            for (int k = 0; k < 8; k++)
            {
                float pa = k * Mathf.PI / 4f;
                Obstacles.Add(new Obstacle { x = BossPos.x + Mathf.Cos(pa) * 8f, z = BossPos.z + Mathf.Sin(pa) * 8f, hx = 0.8f, hz = 0.8f, round = true, blockId = -1 });
            }

            Npc("elder", "Elder Maren", "quest", -9f, 4f, 130f, 1);
            Npc("smith", "Borin the Smith", "smith", 11f, 8f, -90f, 2);
            Npc("alchemist", "Sylva the Alchemist", "alchemist", 13f, -6f, -110f, 3);
            Npc("innkeeper", "Tamsin", "lore", -12f, -6f, 90f, 4);
            Npc("guard", "Guard Roan", "guard", 26f, rc30 - 17f, 0f, 5);

            for (int i = 0; i < 10; i++) { Vector2 q = FindOpen(Polar(rng, 36f, 80f, 0, 360f), 6f); Spawn("gloop", q.x, q.y); }
            for (int i = 0; i < 6; i++) { Vector2 q = FindOpen(Polar(rng, 52f, 86f, 0, 360f), 6f); Spawn("fang", q.x, q.y); }
            for (int i = 0; i < 4; i++) { Vector2 q = FindOpen(Polar(rng, 60f, 88f, 0, 360f), 6f); Spawn("spitter", q.x, q.y); }
            for (int i = 0; i < 2; i++) { Vector2 q = FindOpen(Polar(rng, 70f, 90f, 0, 360f), 6f); Spawn("golem", q.x, q.y); }
        }

        Vector2 Polar(DetRng rng, float r0, float r1, float a0, float a1)
        {
            float a = rng.Range(a0, a1) * Mathf.Deg2Rad, r = rng.Range(r0, r1);
            return new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
        }

        Vector2 FindOpen(Vector2 start, float margin)
        {
            Vector2 p = start;
            for (int i = 0; i < 60; i++)
            {
                if (Free(p.x, p.y, margin)) return p;
                float a = i * 2.399f; float r = 3f + i * 1.3f;
                p = start + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
            }
            return start;
        }

        // ---------- Kingdom ----------
        void GenKingdom()
        {
            float S = 36f;
            SpawnPoint = P(0f, -26f);
            PortalPos = P(0f, -22f);
            Buildings.Add(new BuildingDef { Kind = "portal", Pos = PortalPos, Yaw = 0f, W = 5f, D = 5f, Variant = 1 });
            Obstacles.Add(new Obstacle { x = 0f, z = -22f, hx = 3.2f, hz = 0.8f, yaw = 0f, round = false, blockId = -1 });
            Ring("fountain", 0f, -6f, 3f);
            int id = 0;
            for (int side = 0; side < 4; side++)
            {
                for (int i = 0; i < 24; i++)
                {
                    float t = -S + 1.5f + i * 3f;
                    float x, z, yaw;
                    if (side == 0) { x = t; z = -S; yaw = 0f; }
                    else if (side == 1) { x = t; z = S; yaw = 0f; }
                    else if (side == 2) { x = -S; z = t; yaw = 90f; }
                    else { x = S; z = t; yaw = 90f; }
                    if (side == 0 && Mathf.Abs(t) < 5.5f) continue;
                    for (int row = 0; row < 2; row++)
                    {
                        float gy = Base.At(x, z);
                        BlockDef b = new BlockDef { Id = id++, Pos = new Vector3(x, gy + 1.5f + row * 3f, z), Size = new Vector3(3f, 3f, 2.4f), Yaw = yaw, Hp = 260f };
                        Blocks.Add(b);
                        if (row == 0) Obstacles.Add(new Obstacle { x = x, z = z, hx = 1.5f, hz = 1.2f, yaw = yaw * Mathf.Deg2Rad, round = false, blockId = b.Id });
                    }
                }
            }
            float[] cx = { -S, S, -S, S }; float[] cz = { -S, -S, S, S };
            for (int i = 0; i < 4; i++) { Buildings.Add(new BuildingDef { Kind = "tower", Pos = P(cx[i], cz[i]), W = 8f, D = 8f }); Obstacles.Add(new Obstacle { x = cx[i], z = cz[i], hx = 4.2f, hz = 4.2f, round = true, blockId = -1 }); }
            Buildings.Add(new BuildingDef { Kind = "keep", Pos = P(0f, 20f), W = 20f, D = 14f });
            Obstacles.Add(new Obstacle { x = 0f, z = 20f, hx = 10f, hz = 7f, yaw = 0f, round = false, blockId = -1 });
            Add("smith", -22f, 4f, 90f, 9f, 7f, 1, true);
            Add("alchemist", 22f, 4f, -90f, 6f, 6f, 1, true);
            Road.Add(new Vector2(0f, -S - 30f)); Road.Add(new Vector2(0f, -S)); Road.Add(new Vector2(0f, 8f));
            DetRng rng = new DetRng(Def.Seed + 5);
            for (int i = 0; i < 14; i++)
            {
                float a = i / 14f * 360f + rng.Range(-8f, 8f), d = rng.Range(56f, 72f);
                float x = Mathf.Cos(a * Mathf.Deg2Rad) * d, z = Mathf.Sin(a * Mathf.Deg2Rad) * d;
                if (!Free(x, z, 4f) || Base.RiverDist(x, z) < 14f) continue;
                Add("house", x, z, -a + 90f, rng.Range(6f, 8f), rng.Range(5.5f, 7f), i % 4, true);
            }
            Npc("kguide", "Seneschal Valen", "quest", 6f, -14f, 200f, 6);
            Npc("ksmith", "Hale the Armorer", "smith", -17f, 2f, 90f, 2);
            Npc("kalch", "Orla the Brewer", "alchemist", 17f, 2f, -90f, 3);
            Npc("kcook", "Mabel the Cook", "quest", -12f, -14f, 160f, 4);
            Npc("kfisher", "Pike the Fisher", "quest", 12f, -14f, 200f, 5);
            Npc("kfarmer", "Dorn the Farmer", "quest", -24f, -8f, 120f, 1);
            Npc("khunter", "Kessa the Hunter", "quest", 24f, -8f, -120f, 6);
            Npc("kminer", "Gruff the Miner", "quest", -27f, -20f, 120f, 2);
            Npc("kmerchant", "Zara the Merchant", "quest", 27f, -20f, -120f, 3);
            Npc("kscribe", "Ilyan the Scribe", "quest", -8f, 8f, 180f, 4);
            Npc("kcaptain", "Captain Marek", "quest", 8f, 8f, 180f, 5);
            Npc("kjeweler", "Opal the Jeweler", "quest", -26f, 12f, 120f, 1);
            Npc("ktailor", "Wilm the Tailor", "quest", 26f, 12f, -120f, 6);
            Npc("kbard", "Lyra the Bard", "quest", -4f, -18f, 180f, 3);
            Npc("kpriest", "Brother Aldous", "quest", 4f, -18f, 180f, 2);
            string[] kt = { "fang", "golem", "wraith", "brute", "harpy", "drakeling", "spitter" };
            int[] kc = { 8, 6, 8, 6, 6, 3, 4 };
            for (int t = 0; t < kt.Length; t++)
                for (int i = 0; i < kc[t]; i++) { Vector2 q = FindOpen(Polar(rng, 52f, 112f, 0, 360f), 6f); Spawn(kt[t], q.x, q.y); }
        }

        // ---------- Resource nodes ----------
        void GenNodes()
        {
            DetRng rng = new DetRng(Def.Seed + 99);
            int id = 0;
            NodeKind[] kinds = { NodeKind.Tree, NodeKind.Ore, NodeKind.Crystal, NodeKind.Herb };
            int[] counts = Def.Id == 1 ? new[] { 20, 22, 14, 30 } : new[] { 26, 14, 7, 28 };
            float[] r0 = { 30f, 55f, 70f, 12f }; float[] r1 = { 90f, 98f, 100f, 80f };
            for (int k = 0; k < 4; k++)
            {
                for (int i = 0; i < counts[k]; i++)
                {
                    for (int tries = 0; tries < 30; tries++)
                    {
                        float hi = r1[k]; if (k == 3 && i < counts[k] * 0.6f) hi = 48f;
                        Vector2 q = Polar(rng, r0[k], hi, 0f, 360f);
                        if (!Free(q.x, q.y, 3f)) continue;
                        Nodes.Add(new NodeDef { Id = id++, Kind = kinds[k], Pos = P(q.x, q.y) });
                        Obstacles.Add(new Obstacle { x = q.x, z = q.y, hx = 0.6f, hz = 0.6f, round = true, blockId = -1 });
                        break;
                    }
                }
            }
        }

        // ---------- Collision ----------
        public Vector2 Resolve(Vector2 p, float rad, HashSet<int> destroyed)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < Obstacles.Count; i++)
                {
                    Obstacle o = Obstacles[i];
                    if (o.blockId >= 0 && destroyed != null && destroyed.Contains(o.blockId)) continue;
                    float m = Mathf.Max(o.hx, o.hz) + rad + 0.1f;
                    if (Mathf.Abs(p.x - o.x) > m || Mathf.Abs(p.y - o.z) > m) continue;
                    Vector2 d = new Vector2(p.x - o.x, p.y - o.z);
                    if (o.round)
                    {
                        float dist = d.magnitude, min = o.hx + rad;
                        if (dist < min) { if (dist < 0.0001f) d = Vector2.right; else d /= dist; p = new Vector2(o.x, o.z) + d * min; }
                    }
                    else
                    {
                        float c = Mathf.Cos(-o.yaw), s = Mathf.Sin(-o.yaw);
                        float lx = d.x * c - d.y * s, lz = d.x * s + d.y * c;
                        float dx = Mathf.Abs(lx) - (o.hx + rad), dz = Mathf.Abs(lz) - (o.hz + rad);
                        if (dx < 0f && dz < 0f)
                        {
                            if (dx > dz) lx = Mathf.Sign(lx == 0f ? 1f : lx) * (o.hx + rad); else lz = Mathf.Sign(lz == 0f ? 1f : lz) * (o.hz + rad);
                            float c2 = Mathf.Cos(o.yaw), s2 = Mathf.Sin(o.yaw);
                            p = new Vector2(o.x + lx * c2 - lz * s2, o.z + lx * s2 + lz * c2);
                        }
                    }
                }
            }
            return p;
        }
    }
}
