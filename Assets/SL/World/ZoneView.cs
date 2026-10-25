using System.Collections.Generic;
using UnityEngine;
using SL.Core;

namespace SL.World
{
    /// <summary>Client-side visual world for one zone: terrain chunks, props, buildings, water, walls, resource nodes and chests.</summary>
    public sealed class ZoneView
    {
        public static readonly int[] ChestHex = { 0x9b7448, 0x4a4a55, 0xcd7f32, 0xd0d4da, 0xffc940, 0x9fe8ff, 0x3ddc84, 0x3a6bff, 0xe0245e, 0x7fffd4, 0xfff7c2 };
        public GameObject Root;
        public ZoneLayout Layout;
        public HeightField Hf;
        public readonly Dictionary<int, GameObject> BlockObjs = new Dictionary<int, GameObject>();
        public readonly Dictionary<int, GameObject> NodeObjs = new Dictionary<int, GameObject>();
        public readonly Dictionary<int, GameObject> ChestLids = new Dictionary<int, GameObject>();
        public readonly Dictionary<int, GameObject> ChestGlows = new Dictionary<int, GameObject>();
        GameObject portalDisc; Material portalMat;

        const float CS = 48f;
        const int RES = 32;
        int nChunks; float half;
        GameObject[] chunkObjs;
        GameObject[] propObjs;

        static Color Hx(int c) { return SLMath.Hex(c); }

        public static ZoneView Build(int zone)
        {
            ZoneView v = new ZoneView();
            v.Layout = ZoneLayout.Get(zone);
            v.Hf = new HeightField(v.Layout.Def);
            v.Create();
            return v;
        }

        void Create()
        {
            Root = new GameObject("SL_Zone_" + Layout.Def.Name);
            nChunks = Mathf.CeilToInt(Layout.Def.Radius * 2f / CS);
            half = nChunks * CS * 0.5f;
            chunkObjs = new GameObject[nChunks * nChunks];
            propObjs = new GameObject[nChunks * nChunks];
            for (int cz = 0; cz < nChunks; cz++)
                for (int cx = 0; cx < nChunks; cx++) BuildChunk(cx, cz, true);
            BuildWater();
            BuildBuildings();
            BuildBlocks();
            BuildNodes();
            BuildChests();
            BuildNpcMarkers();
        }

        // ------------------------------------------------------------ terrain
        Color TerrainColor(float x, float z, float h, float slope)
        {
            ZoneDef Z = Layout.Def;
            float n = SLMath.Noise(x * 0.15f, z * 0.15f, Z.Seed + 1);
            Color grass = Color.Lerp(Hx(0x4a7f35), Hx(0x86b84e), n);
            float forest = SLMath.Fbm(x * 0.02f, z * 0.02f, Z.Seed + 11, 2);
            grass = Color.Lerp(grass, Hx(0x2f5f2a), SLMath.Smooth(0.0f, 0.18f, forest));
            Color c = grass;
            float sand = 1f - SLMath.Smooth(Z.WaterY + 0.2f, Z.WaterY + 1.0f, h);
            c = Color.Lerp(c, Hx(0xd8c58e), sand);
            Color rockC = Color.Lerp(Hx(0x7d7a74), Hx(0x9b958a), n);
            c = Color.Lerp(c, rockC, SLMath.Smooth(0.28f, 0.6f, slope));
            c = Color.Lerp(c, rockC, SLMath.Smooth(15f, 26f, h));
            c = Color.Lerp(c, Hx(0xf0f4f8), SLMath.Smooth(32f, 40f, h));
            c = Color.Lerp(c, Hx(0x3a6f6f), SLMath.Smooth(Z.WaterY, Z.WaterY - 1.8f, h));
            float road = 1f - SLMath.Smooth(1.5f, 2.8f, Layout.RoadDist(x, z));
            c = Color.Lerp(c, SLMath.Jitter(Hx(0x9b7a52), 0.1f, n), road);
            float r = Mathf.Sqrt(x * x + z * z);
            float plaza = 1f - SLMath.Smooth(Z.PlazaR * 0.5f, Z.PlazaR * 0.62f, r);
            if (plaza > 0f)
            {
                bool chk = ((Mathf.FloorToInt(x / 2f) + Mathf.FloorToInt(z / 2f)) & 1) == 0;
                c = Color.Lerp(c, chk ? Hx(0xb9b3a6) : Hx(0xa9a396), plaza);
            }
            for (int i = 0; i < Hf.Craters.Count; i++)
            {
                Crater cr = Hf.Craters[i]; float dx = x - cr.x, dz = z - cr.z;
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d < cr.r * 1.15f) c = Color.Lerp(c, Hx(0x2f2723), 0.8f * (1f - SLMath.Smooth(cr.r * 0.4f, cr.r * 1.15f, d)));
            }
            return c;
        }

        Mesh MakeChunkMesh(int cx, int cz)
        {
            MeshBuilder mb = new MeshBuilder();
            float x0 = -half + cx * CS, z0 = -half + cz * CS, step = CS / RES;
            for (int j = 0; j <= RES; j++)
                for (int i = 0; i <= RES; i++)
                {
                    float x = x0 + i * step, z = z0 + j * step;
                    float h = Hf.At(x, z);
                    float hx = Hf.At(x + 0.6f, z) - Hf.At(x - 0.6f, z), hz = Hf.At(x, z + 0.6f) - Hf.At(x, z - 0.6f);
                    Vector3 nrm = new Vector3(-hx, 1.2f, -hz).normalized;
                    mb.V.Add(new Vector3(x, h, z)); mb.N.Add(nrm); mb.UV.Add(Vector2.zero);
                    mb.C.Add(TerrainColor(x, z, h, 1f - nrm.y));
                    mb.W.Add(new BoneWeight());
                }
            int row = RES + 1;
            for (int j = 0; j < RES; j++)
                for (int i = 0; i < RES; i++)
                {
                    int a = j * row + i, b = (j + 1) * row + i, c = (j + 1) * row + i + 1, d = j * row + i + 1;
                    mb.T.Add(a); mb.T.Add(b); mb.T.Add(c); mb.T.Add(a); mb.T.Add(c); mb.T.Add(d);
                }
            return mb.ToMesh("SL_Terrain");
        }

        void BuildChunk(int cx, int cz, bool props)
        {
            int idx = cz * nChunks + cx;
            Mesh m = MakeChunkMesh(cx, cz);
            GameObject go = chunkObjs[idx];
            if (go == null)
            {
                go = new GameObject("Chunk_" + cx + "_" + cz);
                go.transform.SetParent(Root.transform, false);
                go.AddComponent<MeshFilter>(); go.AddComponent<MeshRenderer>().sharedMaterial = SLMat.Vertex();
                go.AddComponent<MeshCollider>();
                chunkObjs[idx] = go;
            }
            MeshFilter mf = go.GetComponent<MeshFilter>();
            Mesh old = mf.sharedMesh;
            mf.sharedMesh = m;
            go.GetComponent<MeshCollider>().sharedMesh = m;
            if (old != null) Object.Destroy(old);
            if (props) BuildProps(cx, cz, idx);
        }

        public void AddCrater(float x, float z, float r, float d)
        {
            Hf.AddCrater(x, z, r, d);
            int c0 = Mathf.Clamp(Mathf.FloorToInt((x - r + half) / CS), 0, nChunks - 1), c1 = Mathf.Clamp(Mathf.FloorToInt((x + r + half) / CS), 0, nChunks - 1);
            int r0 = Mathf.Clamp(Mathf.FloorToInt((z - r + half) / CS), 0, nChunks - 1), r1 = Mathf.Clamp(Mathf.FloorToInt((z + r + half) / CS), 0, nChunks - 1);
            for (int cz = r0; cz <= r1; cz++) for (int cx = c0; cx <= c1; cx++) BuildChunk(cx, cz, false);
        }

        // ------------------------------------------------------------ props
        void BuildProps(int cx, int cz, int idx)
        {
            if (propObjs[idx] != null) Object.Destroy(propObjs[idx]);
            MeshBuilder mb = new MeshBuilder();
            float x0 = -half + cx * CS, z0 = -half + cz * CS;
            int seed = Layout.Def.Seed;
            for (float gx = 0f; gx < CS; gx += 4f)
                for (float gz = 0f; gz < CS; gz += 4f)
                {
                    int ix = Mathf.FloorToInt((x0 + gx) / 4f), iz = Mathf.FloorToInt((z0 + gz) / 4f);
                    float x = x0 + gx + SLMath.Hash(ix, iz, seed + 21) * 3.4f, z = z0 + gz + SLMath.Hash(ix, iz, seed + 22) * 3.4f;
                    float forest = SLMath.Fbm(x * 0.02f, z * 0.02f, seed + 11, 2);
                    float p = SLMath.Hash(ix, iz, seed + 23);
                    float chance = forest > 0.0f ? 0.5f : 0.05f;
                    float h = Hf.Base(x, z);
                    float r = Mathf.Sqrt(x * x + z * z);
                    if (r > Layout.Def.Radius * 0.86f) { chance = 0f; }
                    if (p > chance) { if (p < chance + 0.05f && h > 1.2f && h < 18f && Layout.Free(x, z, 1f)) Rock(mb, new Vector3(x, h, z), 0.6f + SLMath.Hash(ix, iz, 5) * 1.2f, ix + iz); continue; }
                    if (h < Layout.Def.WaterY + 0.8f || h > 24f || !Layout.Free(x, z, 1.5f)) continue;
                    float sc = 0.8f + SLMath.Hash(ix, iz, seed + 24) * 0.7f;
                    if (h > 11f || SLMath.Hash(ix, iz, seed + 25) < 0.35f) Pine(mb, new Vector3(x, h, z), sc, ix * 7 + iz); else Oak(mb, new Vector3(x, h, z), sc, ix * 7 + iz);
                }
            for (float gx = 0f; gx < CS; gx += 2f)
                for (float gz = 0f; gz < CS; gz += 2f)
                {
                    int ix = Mathf.FloorToInt((x0 + gx) / 2f), iz = Mathf.FloorToInt((z0 + gz) / 2f);
                    if (SLMath.Hash(ix, iz, seed + 31) > 0.4f) continue;
                    float x = x0 + gx + SLMath.Hash(ix, iz, seed + 32) * 1.8f, z = z0 + gz + SLMath.Hash(ix, iz, seed + 33) * 1.8f;
                    float h = Hf.Base(x, z);
                    float r = Mathf.Sqrt(x * x + z * z);
                    if (h < Layout.Def.WaterY + 0.6f || h > 14f || r < Layout.Def.PlazaR * 0.7f || r > Layout.Def.Radius * 0.88f) continue;
                    if (Layout.RoadDist(x, z) < 2.4f) continue;
                    Tuft(mb, new Vector3(x, h - 0.05f, z), 0.35f + SLMath.Hash(ix, iz, 34) * 0.4f, ix + iz * 3, SLMath.Hash(ix, iz, 35) < 0.08f);
                }
            GameObject go = new GameObject("Props_" + cx + "_" + cz);
            go.transform.SetParent(Root.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("SL_Props");
            go.AddComponent<MeshRenderer>().sharedMaterial = SLMat.Vertex();
            propObjs[idx] = go;
        }

        public static void Oak(MeshBuilder mb, Vector3 p, float s, int seed)
        {
            Color bark = SLMath.Jitter(Hx(0x6b4a2e), 0.15f, SLMath.Hash(seed, 1, 3));
            Color leaf = SLMath.Jitter(Hx(0x3f8a3a), 0.2f, SLMath.Hash(seed, 2, 3));
            mb.Push(p, SLMath.Hash(seed, 3, 3) * 360f);
            Vector3[] cs = { new Vector3(0, -0.2f, 0), new Vector3(0.05f, 1.2f * s, 0), new Vector3(0, 2.4f * s, 0.05f) };
            mb.Loft(cs, new[] { 0.34f * s, 0.24f * s, 0.15f * s }, new[] { 0.34f * s, 0.24f * s, 0.15f * s }, 6, bark, false, false);
            mb.Ellipsoid(new Vector3(0f, 3.3f * s, 0f), new Vector3(1.9f * s, 1.5f * s, 1.9f * s), leaf, 8, 4);
            mb.Ellipsoid(new Vector3(1.0f * s, 2.8f * s, 0.4f * s), new Vector3(1.3f * s, 1.1f * s, 1.3f * s), SLMath.Jitter(leaf, 0.12f, 0.8f), 7, 3);
            mb.Ellipsoid(new Vector3(-0.9f * s, 2.9f * s, -0.5f * s), new Vector3(1.3f * s, 1.1f * s, 1.3f * s), SLMath.Jitter(leaf, 0.12f, 0.2f), 7, 3);
            mb.Pop();
        }

        public static void Pine(MeshBuilder mb, Vector3 p, float s, int seed)
        {
            Color bark = Hx(0x5a3d26);
            Color leaf = SLMath.Jitter(Hx(0x2a6a45), 0.18f, SLMath.Hash(seed, 2, 5));
            mb.Push(p, SLMath.Hash(seed, 3, 5) * 360f);
            mb.Cylinder(new Vector3(0, -0.2f, 0), 1.6f * s, 0.26f * s, 0.18f * s, 5, bark, false);
            mb.Cone(new Vector3(0, 1.0f * s, 0), 2.2f * s, 1.6f * s, 7, leaf);
            mb.Cone(new Vector3(0, 2.3f * s, 0), 2.0f * s, 1.2f * s, 7, SLMath.Jitter(leaf, 0.1f, 0.7f));
            mb.Cone(new Vector3(0, 3.5f * s, 0), 1.8f * s, 0.8f * s, 7, SLMath.Jitter(leaf, 0.1f, 0.3f));
            mb.Pop();
        }

        public static void Rock(MeshBuilder mb, Vector3 p, float s, int seed)
        {
            Color c = SLMath.Jitter(Hx(0x85817a), 0.15f, SLMath.Hash(seed, 7, 7));
            mb.Push(p, SLMath.Hash(seed, 8, 7) * 360f);
            mb.Ellipsoid(new Vector3(0f, 0.3f * s, 0f), new Vector3(1.0f * s, 0.7f * s, 0.8f * s), c, 6, 3);
            mb.Ellipsoid(new Vector3(0.6f * s, 0.2f * s, 0.3f * s), new Vector3(0.6f * s, 0.45f * s, 0.55f * s), SLMath.Jitter(c, 0.1f, 0.9f), 6, 3);
            mb.Pop();
        }

        static void Tuft(MeshBuilder mb, Vector3 p, float h, int seed, bool flower)
        {
            Color baseC = Hx(0x2f6a2a), tipC = SLMath.Jitter(Hx(0x8ccf4e), 0.15f, SLMath.Hash(seed, 1, 9));
            for (int b = 0; b < 3; b++)
            {
                float a = (b / 3f + SLMath.Hash(seed, b, 9)) * Mathf.PI;
                Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.12f;
                Vector3 lean = new Vector3(Mathf.Sin(a * 3f), 0f, Mathf.Cos(a * 3f)) * h * 0.25f;
                int v0 = mb.Vert(p - d, baseC), v1 = mb.Vert(p + d, baseC), v2 = mb.Vert(p + new Vector3(0f, h, 0f) + lean, tipC);
                mb.T.Add(v0); mb.T.Add(v1); mb.T.Add(v2); mb.T.Add(v0); mb.T.Add(v2); mb.T.Add(v1);
            }
            if (flower)
            {
                Color fc = SLMath.Hash(seed, 4, 9) < 0.5f ? Hx(0xf4d84a) : Hx(0xf08ac0);
                mb.Ellipsoid(p + new Vector3(0f, h + 0.08f, 0f), new Vector3(0.1f, 0.07f, 0.1f), fc, 5, 2);
            }
        }

        // ------------------------------------------------------------ water & buildings
        void BuildWater()
        {
            MeshBuilder mb = new MeshBuilder();
            int n = 24; float R = Layout.Def.Radius * 1.05f;
            for (int j = 0; j <= n; j++)
                for (int i = 0; i <= n; i++)
                {
                    mb.V.Add(new Vector3(-R + 2f * R * i / n, Layout.Def.WaterY, -R + 2f * R * j / n));
                    mb.N.Add(Vector3.up); mb.C.Add(Color.white); mb.UV.Add(Vector2.zero); mb.W.Add(new BoneWeight());
                }
            for (int j = 0; j < n; j++) for (int i = 0; i < n; i++)
                {
                    int a = j * (n + 1) + i, b = (j + 1) * (n + 1) + i, c = b + 1, d = a + 1;
                    mb.T.Add(a); mb.T.Add(b); mb.T.Add(c); mb.T.Add(a); mb.T.Add(c); mb.T.Add(d);
                }
            GameObject go = new GameObject("Water");
            go.transform.SetParent(Root.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("SL_Water");
            go.AddComponent<MeshRenderer>().sharedMaterial = SLMat.Water();
        }

        void BuildBuildings()
        {
            MeshBuilder solid = new MeshBuilder(), glow = new MeshBuilder();
            for (int i = 0; i < Layout.Buildings.Count; i++)
            {
                BuildingDef b = Layout.Buildings[i];
                if (b.Kind == "portal") { BuildPortal(b); continue; }
                BuildingKit.Build(solid, glow, b, Hf);
            }
            // castle gate lintel (kingdom)
            if (Layout.Def.Id == 1)
            {
                float gy = Hf.At(0f, -36f);
                solid.Box(new Vector3(0f, gy + 7.2f, -36f), new Vector3(12f, 2.4f, 2.6f), SLMath.Hex(0x8f8a80));
                solid.Box(new Vector3(-5.7f, gy + 3f, -36f), new Vector3(1.4f, 6f, 2.6f), SLMath.Hex(0x6c6861));
                solid.Box(new Vector3(5.7f, gy + 3f, -36f), new Vector3(1.4f, 6f, 2.6f), SLMath.Hex(0x6c6861));
            }
            GameObject s = new GameObject("Buildings"); s.transform.SetParent(Root.transform, false);
            s.AddComponent<MeshFilter>().sharedMesh = solid.ToMesh("SL_Buildings");
            s.AddComponent<MeshRenderer>().sharedMaterial = SLMat.Vertex();
            GameObject g = new GameObject("BuildingGlow"); g.transform.SetParent(Root.transform, false);
            g.AddComponent<MeshFilter>().sharedMesh = glow.ToMesh("SL_Glow");
            g.AddComponent<MeshRenderer>().sharedMaterial = SLMat.Lit(Color.white, 0.3f, 0f, new Color(1f, 0.72f, 0.38f) * 1.1f);
        }

        void BuildPortal(BuildingDef b)
        {
            MeshBuilder mb = new MeshBuilder();
            mb.Push(b.Pos, 0f);
            Color st = SLMath.Hex(0x8f8a80);
            mb.Box(new Vector3(-2.6f, 2.6f, 0f), new Vector3(0.9f, 5.2f, 1.0f), st);
            mb.Box(new Vector3(2.6f, 2.6f, 0f), new Vector3(0.9f, 5.2f, 1.0f), st);
            for (int i = 0; i <= 10; i++)
            {
                float a = Mathf.PI * i / 10f;
                mb.Box(new Vector3(Mathf.Cos(a) * 2.6f, 5.2f + Mathf.Sin(a) * 2.2f, 0f), new Vector3(0.95f, 0.95f, 1.0f), st);
            }
            mb.Box(new Vector3(0f, 0.2f, 0f), new Vector3(6.4f, 0.4f, 2.2f), SLMath.Hex(0x6c6861));
            mb.Pop();
            GameObject frame = new GameObject("PortalFrame"); frame.transform.SetParent(Root.transform, false);
            frame.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("SL_Portal");
            frame.AddComponent<MeshRenderer>().sharedMaterial = SLMat.Vertex();
            MeshBuilder d = new MeshBuilder();
            d.Push(b.Pos + new Vector3(0f, 4.7f, 0f), Quaternion.Euler(90f, 0f, 0f), Vector3.one);
            d.Cylinder(Vector3.zero, 0.2f, 2.5f, 2.5f, 20, Color.white, true);
            d.Pop();
            portalDisc = new GameObject("PortalDisc"); portalDisc.transform.SetParent(Root.transform, false);
            portalDisc.AddComponent<MeshFilter>().sharedMesh = d.ToMesh("SL_PortalDisc");
            portalMat = new Material(SLMat.Lit(Color.white, 0.5f, 0f, Color.black));
            portalDisc.AddComponent<MeshRenderer>().sharedMaterial = portalMat;
            SetPortalActive(false);
        }

        public void SetPortalActive(bool on)
        {
            if (portalMat == null) return;
            portalMat.SetColor("_Color", on ? new Color(0.5f, 0.9f, 1f) : new Color(0.25f, 0.25f, 0.3f));
            portalMat.SetColor("_Emission", on ? new Color(0.4f, 0.9f, 1.6f) : new Color(0.02f, 0.02f, 0.03f));
        }

        // ------------------------------------------------------------ blocks, nodes, chests
        void BuildBlocks()
        {
            Material m = SLMat.Vertex();
            for (int i = 0; i < Layout.Blocks.Count; i++)
            {
                BlockDef b = Layout.Blocks[i];
                GameObject go = new GameObject("Block_" + b.Id);
                go.transform.SetParent(Root.transform, false);
                go.transform.position = b.Pos; go.transform.rotation = Quaternion.Euler(0f, b.Yaw, 0f);
                go.AddComponent<MeshFilter>().sharedMesh = BuildingKit.BlockMesh(b);
                go.AddComponent<MeshRenderer>().sharedMaterial = m;
                BlockObjs[b.Id] = go;
            }
        }

        public void SetBlockDestroyed(int id, bool fx)
        {
            GameObject go;
            if (!BlockObjs.TryGetValue(id, out go) || go == null || !go.activeSelf) return;
            go.SetActive(false);
            if (fx) Debris(go.transform.position, SLMath.Hex(0xa49e92), 7);
        }

        public void RestoreBlock(int id) { GameObject go; if (BlockObjs.TryGetValue(id, out go) && go != null) go.SetActive(true); }

        public static void Debris(Vector3 pos, Color c, int n)
        {
            for (int i = 0; i < n; i++)
            {
                GameObject d = new GameObject("Debris");
                d.transform.position = pos + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
                d.transform.rotation = Random.rotation;
                MeshBuilder mb = new MeshBuilder();
                float s = Random.Range(0.25f, 0.6f);
                mb.Box(Vector3.zero, new Vector3(s, s, s), SLMath.Jitter(c, 0.2f, Random.value));
                d.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("SL_Debris");
                d.AddComponent<MeshRenderer>().sharedMaterial = SLMat.Vertex();
                d.AddComponent<BoxCollider>().size = new Vector3(s, s, s);
                Rigidbody rb = d.AddComponent<Rigidbody>();
                rb.AddExplosionForce(250f, pos, 6f);
                Object.Destroy(d, 4f);
            }
        }

        void BuildNodes()
        {
            for (int i = 0; i < Layout.Nodes.Count; i++)
            {
                NodeDef n = Layout.Nodes[i];
                MeshBuilder mb = new MeshBuilder();
                bool emissive = false;
                switch (n.Kind)
                {
                    case NodeKind.Tree:
                        Oak(mb, Vector3.zero, 1.15f, n.Id);
                        mb.Box(new Vector3(0.3f, 0.7f, 0f), new Vector3(0.1f, 0.5f, 0.3f), SLMath.Hex(0xe0c89a));
                        break;
                    case NodeKind.Ore:
                        Rock(mb, Vector3.zero, 1.2f, n.Id);
                        for (int k = 0; k < 5; k++) mb.Ellipsoid(new Vector3(Mathf.Cos(k * 1.3f) * 0.7f, 0.5f + (k % 2) * 0.2f, Mathf.Sin(k * 1.3f) * 0.6f), new Vector3(0.18f, 0.14f, 0.18f), SLMath.Hex(0xd88a3c), 5, 2);
                        break;
                    case NodeKind.Crystal:
                        emissive = true;
                        for (int k = 0; k < 5; k++) { float a = k * 1.25f; mb.Push(new Vector3(Mathf.Cos(a) * 0.4f, 0f, Mathf.Sin(a) * 0.4f), Quaternion.Euler(Mathf.Sin(a) * 15f, 0f, Mathf.Cos(a) * 15f), Vector3.one); mb.Cone(Vector3.zero, 0.9f + (k % 3) * 0.4f, 0.22f, 5, SLMath.Hex(0x7fd0ff)); mb.Pop(); }
                        break;
                    default:
                        {
                            Color fl = (n.Id % 3 == 0) ? SLMath.Hex(0xe0455a) : ((n.Id % 3 == 1) ? SLMath.Hex(0x5a8aff) : SLMath.Hex(0x8ae06a));
                            mb.Ellipsoid(new Vector3(0f, 0.35f, 0f), new Vector3(0.7f, 0.4f, 0.7f), SLMath.Hex(0x3f7a38), 7, 3);
                            for (int k = 0; k < 6; k++) mb.Ellipsoid(new Vector3(Mathf.Cos(k) * 0.45f, 0.6f + (k % 2) * 0.12f, Mathf.Sin(k) * 0.45f), new Vector3(0.11f, 0.11f, 0.11f), fl, 5, 2);
                            break;
                        }
                }
                GameObject go = new GameObject("Node_" + n.Id);
                go.transform.SetParent(Root.transform, false);
                go.transform.position = n.Pos;
                go.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("SL_Node");
                go.AddComponent<MeshRenderer>().sharedMaterial = emissive ? SLMat.Lit(Color.white, 0.6f, 0f, new Color(0.2f, 0.6f, 1f)) : SLMat.Vertex();
                NodeObjs[n.Id] = go;
            }
        }

        public void SetNodeActive(int id, bool active) { GameObject go; if (NodeObjs.TryGetValue(id, out go) && go != null) go.SetActive(active); }

        void BuildChests()
        {
            for (int i = 0; i < Layout.Chests.Count; i++)
            {
                ChestDef c = Layout.Chests[i];
                Color metal = SLMath.Hex(ChestHex[Mathf.Clamp(c.Grade, 0, 10)]);
                Color wood = SLMath.Hex(0x6b4528);
                MeshBuilder body = new MeshBuilder();
                body.Box(new Vector3(0f, 0.35f, 0f), new Vector3(1.1f, 0.7f, 0.75f), wood);
                body.Box(new Vector3(-0.4f, 0.36f, 0f), new Vector3(0.12f, 0.76f, 0.8f), metal);
                body.Box(new Vector3(0.4f, 0.36f, 0f), new Vector3(0.12f, 0.76f, 0.8f), metal);
                GameObject go = new GameObject("Chest_" + c.Id);
                go.transform.SetParent(Root.transform, false);
                go.transform.position = c.Pos; go.transform.rotation = Quaternion.Euler(0f, SLMath.Hash(c.Id, 1, 5) * 360f, 0f);
                go.AddComponent<MeshFilter>().sharedMesh = body.ToMesh("SL_Chest");
                go.AddComponent<MeshRenderer>().sharedMaterial = SLMat.Vertex();
                MeshBuilder lid = new MeshBuilder();
                lid.Box(new Vector3(0f, 0.1f, 0.375f), new Vector3(1.12f, 0.22f, 0.77f), wood);
                lid.Box(new Vector3(-0.4f, 0.11f, 0.375f), new Vector3(0.13f, 0.26f, 0.8f), metal);
                lid.Box(new Vector3(0.4f, 0.11f, 0.375f), new Vector3(0.13f, 0.26f, 0.8f), metal);
                lid.Box(new Vector3(0f, 0.02f, 0.78f), new Vector3(0.18f, 0.2f, 0.06f), metal);
                GameObject lg = new GameObject("Lid"); lg.transform.SetParent(go.transform, false);
                lg.transform.localPosition = new Vector3(0f, 0.7f, -0.375f);
                lg.AddComponent<MeshFilter>().sharedMesh = lid.ToMesh("SL_ChestLid");
                lg.AddComponent<MeshRenderer>().sharedMaterial = SLMat.Vertex();
                ChestLids[c.Id] = lg;
                if (c.Grade >= 3)
                {
                    MeshBuilder gl = new MeshBuilder();
                    gl.Ellipsoid(new Vector3(0f, 1.3f, 0f), new Vector3(0.12f, 0.12f, 0.12f), metal, 6, 3);
                    GameObject g = new GameObject("Glint"); g.transform.SetParent(go.transform, false);
                    g.AddComponent<MeshFilter>().sharedMesh = gl.ToMesh("SL_Glint");
                    g.AddComponent<MeshRenderer>().sharedMaterial = SLMat.Lit(Color.white, 0.5f, 0f, metal * 1.5f);
                    ChestGlows[c.Id] = g;
                }
            }
        }

        public void SetChestOpened(int id, bool opened)
        {
            GameObject lid, glow;
            if (ChestLids.TryGetValue(id, out lid) && lid != null) lid.transform.localRotation = Quaternion.Euler(opened ? -110f : 0f, 0f, 0f);
            if (ChestGlows.TryGetValue(id, out glow) && glow != null) glow.SetActive(!opened);
        }

        void BuildNpcMarkers() { }

        public void Destroy() { if (Root != null) Object.Destroy(Root); }
    }
}
