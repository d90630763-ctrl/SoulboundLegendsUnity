using UnityEngine;
using SL.Core;

namespace SL.World
{
    /// <summary>Procedural architecture. Solid parts go to one mesh, emissive parts (windows, forge, lamps) to another.</summary>
    public static class BuildingKit
    {
        static Color H(int c) { return SLMath.Hex(c); }
        static readonly Color Stone = SLMath.Hex(0x8f8a80), StoneDark = SLMath.Hex(0x6c6861), Wood = SLMath.Hex(0x5a3d26), WoodLight = SLMath.Hex(0x8a6540);
        static readonly Color Glass = SLMath.Hex(0xffd58a);

        static void Foundation(MeshBuilder s, BuildingDef b, HeightField hf, out float fy)
        {
            float w = b.W * 0.5f, d = b.D * 0.5f;
            float c = Mathf.Cos(b.Yaw * Mathf.Deg2Rad), sn = Mathf.Sin(b.Yaw * Mathf.Deg2Rad);
            float maxH = -999f, minH = 999f;
            for (int i = 0; i < 4; i++)
            {
                float lx = (i & 1) == 0 ? -w : w, lz = (i & 2) == 0 ? -d : d;
                float wx = b.Pos.x + lx * c + lz * sn, wz = b.Pos.z - lx * sn + lz * c;
                float h = hf.At(wx, wz) - b.Pos.y;
                if (h > maxH) maxH = h; if (h < minH) minH = h;
            }
            fy = Mathf.Max(maxH, 0f) + 0.35f;
            float bottom = Mathf.Min(minH, 0f) - 0.6f;
            s.Box(new Vector3(0f, (fy + bottom) * 0.5f, 0f), new Vector3(b.W + 0.5f, fy - bottom, b.D + 0.5f), Stone);
        }

        public static void Build(MeshBuilder s, MeshBuilder g, BuildingDef b, HeightField hf)
        {
            switch (b.Kind)
            {
                case "house": House(s, g, b, hf, 0); break;
                case "hall": House(s, g, b, hf, 10); break;
                case "inn": House(s, g, b, hf, 20); break;
                case "smith": Smith(s, g, b, hf); break;
                case "alchemist": Alchemist(s, g, b, hf); break;
                case "fountain": Fountain(s, g, b); break;
                case "well": Well(s, b); break;
                case "stall": Stall(s, b); break;
                case "lamp": Lamp(s, g, b); break;
                case "bridge": Bridge(s, b); break;
                case "ruins": Ruins(s, g, b); break;
                case "tower": Tower(s, g, b); break;
                case "keep": Keep(s, g, b, hf); break;
            }
        }

        // ---------------------------------------------------------------- houses
        static void House(MeshBuilder s, MeshBuilder g, BuildingDef b, HeightField hf, int kind)
        {
            float w = b.W, d = b.D;
            Color[] walls = { H(0xe8dcc0), H(0xd9c7a1), H(0xcfd6c9), H(0xe3cfc0) };
            Color[] roofs = { H(0x8b3d2e), H(0x5c6b7a), H(0x7a4a2d), H(0x4e6b46) };
            Color wall = walls[b.Variant % 4], roof = roofs[(b.Variant + 1) % 4];
            if (kind == 10) { wall = H(0xcfc7b8); roof = H(0x3d4f73); }
            if (kind == 20) { wall = H(0xd8c19a); roof = H(0x6e3b28); }
            float h = kind == 20 ? 5.8f : (kind == 10 ? 4.4f : 3.1f + (b.Variant % 2) * 0.4f);
            s.Push(b.Pos, b.Yaw);
            float fy; Foundation(s, b, hf, out fy);
            s.BoxBase(new Vector3(0f, fy, 0f), new Vector3(w, h, d), wall);
            // timber frame
            for (int i = 0; i < 4; i++)
            {
                float px = (i & 1) == 0 ? -w * 0.5f : w * 0.5f, pz = (i & 2) == 0 ? -d * 0.5f : d * 0.5f;
                s.BoxBase(new Vector3(px, fy, pz), new Vector3(0.34f, h + 0.1f, 0.34f), Wood);
            }
            s.Box(new Vector3(0f, fy + h, d * 0.5f), new Vector3(w + 0.3f, 0.28f, 0.34f), Wood);
            s.Box(new Vector3(0f, fy + h, -d * 0.5f), new Vector3(w + 0.3f, 0.28f, 0.34f), Wood);
            s.Box(new Vector3(0f, fy + h * 0.5f, d * 0.5f + 0.02f), new Vector3(w, 0.2f, 0.3f), Wood);
            if (kind == 20) s.Box(new Vector3(0f, fy + h * 0.5f, d * 0.5f + 0.02f), new Vector3(w + 0.2f, 0.22f, 0.34f), Wood);
            // roof
            s.Gable(new Vector3(0f, fy + h, 0f), w + 1.4f, d + 1.4f, 1.6f + w * 0.12f, roof);
            s.Box(new Vector3(0f, fy + h + 1.6f + w * 0.12f, 0f), new Vector3(w + 1.6f, 0.2f, 0.3f), Wood);
            // door
            s.Box(new Vector3(0f, fy + 1.05f, d * 0.5f + 0.03f), new Vector3(1.25f, 2.1f, 0.14f), H(0x3e2a1b));
            s.Box(new Vector3(-0.7f, fy + 1.1f, d * 0.5f + 0.06f), new Vector3(0.14f, 2.3f, 0.18f), Wood);
            s.Box(new Vector3(0.7f, fy + 1.1f, d * 0.5f + 0.06f), new Vector3(0.14f, 2.3f, 0.18f), Wood);
            s.Box(new Vector3(0f, fy + 2.25f, d * 0.5f + 0.06f), new Vector3(1.6f, 0.16f, 0.18f), Wood);
            s.Box(new Vector3(0f, fy - 0.1f, d * 0.5f + 0.55f), new Vector3(1.8f, 0.3f, 0.9f), StoneDark);
            s.Box(new Vector3(0f, fy - 0.3f, d * 0.5f + 1.05f), new Vector3(2.0f, 0.3f, 0.7f), StoneDark);
            g.Push(b.Pos, b.Yaw);
            int floors = kind == 20 ? 2 : 1;
            for (int f = 0; f < floors; f++)
            {
                float wy = fy + 1.7f + f * 2.6f;
                g.Box(new Vector3(-w * 0.28f, wy, d * 0.5f + 0.03f), new Vector3(0.8f, 0.8f, 0.1f), Glass);
                g.Box(new Vector3(w * 0.28f, wy, d * 0.5f + 0.03f), new Vector3(0.8f, 0.8f, 0.1f), Glass);
                g.Box(new Vector3(w * 0.5f + 0.03f, wy, 0f), new Vector3(0.1f, 0.8f, 0.8f), Glass);
                g.Box(new Vector3(-w * 0.5f - 0.03f, wy, 0f), new Vector3(0.1f, 0.8f, 0.8f), Glass);
            }
            g.Box(new Vector3(1.2f, fy + 2.0f, d * 0.5f + 0.2f), new Vector3(0.2f, 0.3f, 0.2f), Glass);
            g.Pop();
            for (int f = 0; f < floors; f++)
            {
                float wy = fy + 1.7f + f * 2.6f;
                s.Box(new Vector3(-w * 0.28f, wy, d * 0.5f + 0.05f), new Vector3(1.05f, 0.12f, 0.1f), Wood);
                s.Box(new Vector3(w * 0.28f, wy, d * 0.5f + 0.05f), new Vector3(1.05f, 0.12f, 0.1f), Wood);
            }
            // chimney
            s.BoxBase(new Vector3(w * 0.3f, fy + h - 0.3f, -d * 0.2f), new Vector3(0.8f, 3.0f, 0.8f), StoneDark);
            if (kind == 20)
            {
                s.Box(new Vector3(0f, fy + 2.8f, d * 0.5f + 0.9f), new Vector3(w * 0.7f, 0.2f, 1.6f), WoodLight);
                s.Box(new Vector3(0f, fy + 3.2f, d * 0.5f + 1.65f), new Vector3(w * 0.7f, 0.9f, 0.12f), Wood);
                s.Box(new Vector3(w * 0.5f + 0.9f, fy + 3.4f, d * 0.5f + 0.4f), new Vector3(0.12f, 0.12f, 1.4f), Wood);
                s.Box(new Vector3(w * 0.5f + 1.7f, fy + 3.0f, d * 0.5f + 1.1f), new Vector3(1.4f, 1.0f, 0.1f), WoodLight);
            }
            if (kind == 10)
            {
                for (int i = -1; i <= 1; i += 2)
                    s.Box(new Vector3(i * 1.6f, fy + 3.2f, d * 0.5f + 0.2f), new Vector3(0.8f, 2.6f, 0.06f), H(0xa8262e));
                s.Cone(new Vector3(-w * 0.5f, fy + h, -d * 0.5f), 3.2f, 1.2f, 8, roof);
                s.Cone(new Vector3(w * 0.5f, fy + h, -d * 0.5f), 3.2f, 1.2f, 8, roof);
            }
            s.Pop();
        }

        static void Smith(MeshBuilder s, MeshBuilder g, BuildingDef b, HeightField hf)
        {
            House(s, g, b, hf, 0);
            float w = b.W;
            s.Push(b.Pos, b.Yaw);
            float fy = 0.4f;
            s.BoxBase(new Vector3(w * 0.5f + 1.5f, fy - 0.3f, 0.5f), new Vector3(2.2f, 1.2f, 2.0f), StoneDark);
            s.BoxBase(new Vector3(w * 0.5f + 1.5f, fy + 0.9f, 0.5f), new Vector3(0.9f, 3.2f, 0.9f), StoneDark);
            s.BoxBase(new Vector3(w * 0.5f + 1.5f, fy - 0.3f, 3.0f), new Vector3(0.6f, 0.9f, 0.6f), H(0x3a3a40));
            s.Box(new Vector3(w * 0.5f + 1.5f, fy + 0.7f, 3.0f), new Vector3(1.3f, 0.3f, 0.5f), H(0x4a4a52));
            s.Box(new Vector3(w * 0.5f + 2.1f, fy + 0.75f, 3.0f), new Vector3(0.5f, 0.2f, 0.25f), H(0x4a4a52));
            for (int i = 0; i < 4; i++)
                s.BoxBase(new Vector3(w * 0.5f + 0.5f + (i & 1) * 2.0f, fy - 0.3f, -0.5f + (i >> 1) * 4.0f), new Vector3(0.22f, 2.6f, 0.22f), Wood);
            s.Box(new Vector3(w * 0.5f + 1.5f, fy + 2.35f, 1.5f), new Vector3(2.8f, 0.2f, 4.6f), H(0x6b4a31));
            s.Pop();
            g.Push(b.Pos, b.Yaw);
            g.Box(new Vector3(w * 0.5f + 1.5f, fy + 0.95f, 0.5f), new Vector3(1.5f, 0.12f, 1.5f), H(0xff6a20));
            g.Pop();
        }

        static void Alchemist(MeshBuilder s, MeshBuilder g, BuildingDef b, HeightField hf)
        {
            float r = b.W * 0.5f;
            s.Push(b.Pos, b.Yaw);
            float fy; Foundation(s, b, hf, out fy);
            s.Cylinder(new Vector3(0f, fy, 0f), 3.4f, r, r * 0.92f, 12, H(0xc9b79a), true);
            s.Cone(new Vector3(0f, fy + 3.3f, 0f), 3.4f, r + 0.9f, 12, H(0x4f3a73));
            s.Cone(new Vector3(0f, fy + 6.5f, 0f), 1.4f, 0.35f, 6, H(0xd9b84a));
            s.Box(new Vector3(0f, fy + 1.05f, r + 0.02f), new Vector3(1.1f, 2.1f, 0.14f), H(0x3e2a1b));
            s.Ellipsoid(new Vector3(r + 1.4f, fy + 0.6f, 0.2f), new Vector3(0.9f, 0.65f, 0.9f), H(0x2f2f35), 10, 6);
            for (int i = 0; i < 3; i++) s.BoxBase(new Vector3(-r - 0.9f, fy, -1.5f + i * 1.5f), new Vector3(0.12f, 1.9f, 0.12f), Wood);
            s.Box(new Vector3(-r - 0.9f, fy + 1.7f, 0f), new Vector3(0.12f, 0.12f, 3.2f), Wood);
            s.Pop();
            g.Push(b.Pos, b.Yaw);
            g.Ellipsoid(new Vector3(r + 1.4f, fy + 1.0f, 0.2f), new Vector3(0.75f, 0.12f, 0.75f), H(0x7dff6a), 10, 3);
            g.Box(new Vector3(0f, fy + 1.9f, -r - 0.02f), new Vector3(0.8f, 0.9f, 0.1f), Glass);
            g.Box(new Vector3(r + 0.02f, fy + 1.9f, 0f), new Vector3(0.1f, 0.9f, 0.8f), Glass);
            g.Ellipsoid(new Vector3(0f, fy + 7.9f, 0f), new Vector3(0.28f, 0.28f, 0.28f), H(0xbfa0ff), 8, 4);
            g.Pop();
        }

        static void Fountain(MeshBuilder s, MeshBuilder g, BuildingDef b)
        {
            float r = b.W * 0.5f;
            s.Push(b.Pos, 0f);
            s.Cylinder(new Vector3(0f, -0.5f, 0f), 1.2f, r, r, 16, Stone, true);
            s.Cylinder(new Vector3(0f, 0.6f, 0f), 0.2f, r - 0.1f, r - 0.1f, 16, StoneDark, true);
            s.Cylinder(new Vector3(0f, 0.4f, 0f), 2.4f, 0.4f, 0.28f, 8, Stone, true);
            s.Ellipsoid(new Vector3(0f, 2.9f, 0f), new Vector3(0.95f, 0.4f, 0.95f), Stone, 12, 5);
            s.Ellipsoid(new Vector3(0f, 3.5f, 0f), new Vector3(0.25f, 0.35f, 0.25f), H(0xd9b84a), 8, 4);
            s.Pop();
            g.Push(b.Pos, 0f);
            g.Cylinder(new Vector3(0f, 0.62f, 0f), 0.06f, r - 0.2f, r - 0.2f, 16, H(0x6fc4ff), true);
            g.Pop();
        }

        static void Well(MeshBuilder s, BuildingDef b)
        {
            s.Push(b.Pos, 0f);
            s.Cylinder(new Vector3(0f, -0.4f, 0f), 1.5f, 1.2f, 1.2f, 12, Stone, true);
            for (int i = -1; i <= 1; i += 2) s.BoxBase(new Vector3(i * 1.05f, 1.0f, 0f), new Vector3(0.2f, 2.0f, 0.2f), Wood);
            s.Gable(new Vector3(0f, 3.0f, 0f), 3.0f, 2.2f, 0.9f, H(0x7a4a2d));
            s.Pop();
        }

        static void Stall(MeshBuilder s, BuildingDef b)
        {
            Color[] a = { H(0xb03a3a), H(0x3a6bb0), H(0xc9a23a) };
            Color c = a[b.Variant % 3];
            s.Push(b.Pos, b.Yaw);
            for (int i = 0; i < 4; i++) s.BoxBase(new Vector3((i & 1) == 0 ? -1.5f : 1.5f, 0f, (i & 2) == 0 ? -1.1f : 1.1f), new Vector3(0.14f, 2.4f, 0.14f), Wood);
            for (int i = 0; i < 6; i++)
            {
                Color sc = (i & 1) == 0 ? c : H(0xf1ead8);
                s.Push(new Vector3(-1.25f + i * 0.5f, 2.55f, 0f), Quaternion.Euler(18f, 0f, 0f), Vector3.one);
                s.Box(Vector3.zero, new Vector3(0.5f, 0.07f, 2.8f), sc);
                s.Pop();
            }
            s.Box(new Vector3(0f, 0.5f, 0.2f), new Vector3(2.8f, 1.0f, 0.9f), WoodLight);
            s.Box(new Vector3(-0.8f, 1.15f, 0.1f), new Vector3(0.5f, 0.3f, 0.5f), H(0xc9803a));
            s.Box(new Vector3(0.3f, 1.12f, 0.2f), new Vector3(0.45f, 0.25f, 0.45f), H(0x7aa83a));
            s.Pop();
        }

        static void Lamp(MeshBuilder s, MeshBuilder g, BuildingDef b)
        {
            s.Push(b.Pos, 0f);
            s.Cylinder(Vector3.zero, 3.0f, 0.1f, 0.07f, 6, H(0x2b2b30), true);
            s.Box(new Vector3(0f, 3.3f, 0f), new Vector3(0.4f, 0.12f, 0.4f), H(0x2b2b30));
            s.Pop();
            g.Push(b.Pos, 0f);
            g.Ellipsoid(new Vector3(0f, 3.1f, 0f), new Vector3(0.18f, 0.22f, 0.18f), Glass, 8, 4);
            g.Pop();
        }

        static void Bridge(MeshBuilder s, BuildingDef b)
        {
            s.Push(b.Pos, b.Yaw);
            float half = b.D * 0.5f;
            int n = Mathf.RoundToInt(b.D / 0.5f);
            for (int i = 0; i < n; i++)
                s.Box(new Vector3(0f, -0.09f, -half + 0.25f + i * 0.5f), new Vector3(b.W, 0.18f, 0.46f), SLMath.Jitter(WoodLight, 0.12f, SLMath.Hash(i, 3, 9)));
            for (int sd = -1; sd <= 1; sd += 2)
            {
                s.Box(new Vector3(sd * (b.W * 0.5f - 0.06f), 1.0f, 0f), new Vector3(0.12f, 0.12f, b.D), Wood);
                s.Box(new Vector3(sd * (b.W * 0.5f - 0.06f), 0.45f, 0f), new Vector3(0.1f, 0.1f, b.D), Wood);
                for (int i = 0; i <= 8; i++) s.BoxBase(new Vector3(sd * (b.W * 0.5f - 0.06f), -0.1f, -half + 0.2f + i * (b.D - 0.4f) / 8f), new Vector3(0.16f, 1.1f, 0.16f), Wood);
            }
            for (int i = 0; i < 4; i++) for (int sd = -1; sd <= 1; sd += 2)
                    s.Cylinder(new Vector3(sd * 1.8f, -4f, -half * 0.7f + i * half * 0.47f), 4f, 0.28f, 0.28f, 6, Wood, false);
            s.Pop();
        }

        static void Ruins(MeshBuilder s, MeshBuilder g, BuildingDef b)
        {
            s.Push(b.Pos, 0f);
            s.Cylinder(new Vector3(0f, -0.3f, 0f), 0.6f, 11f, 11f, 20, SLMath.Hex(0x7a766d), true);
            s.Cylinder(new Vector3(0f, 0.25f, 0f), 0.15f, 6.5f, 6.5f, 20, SLMath.Hex(0x69655d), true);
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI / 4f; float hgt = 2.2f + SLMath.Hash(k, 1, 77) * 3.8f;
                Vector3 p = new Vector3(Mathf.Cos(a) * 8f, 0.2f, Mathf.Sin(a) * 8f);
                s.Cylinder(p, hgt, 0.75f, 0.6f, 8, SLMath.Jitter(Stone, 0.12f, SLMath.Hash(k, 2, 7)), true);
                if (k % 2 == 0) s.Box(p + new Vector3(0f, hgt + 0.2f, 0f), new Vector3(1.8f, 0.4f, 1.8f), StoneDark);
            }
            s.Push(new Vector3(3f, 0.5f, -3f), Quaternion.Euler(0f, 30f, 82f), Vector3.one);
            s.Cylinder(Vector3.zero, 4f, 0.7f, 0.65f, 8, Stone, true);
            s.Pop();
            s.Box(new Vector3(0f, 0.8f, 0f), new Vector3(2.4f, 1.2f, 1.2f), StoneDark);
            s.Pop();
            g.Push(b.Pos, 0f);
            g.Box(new Vector3(0f, 1.5f, 0f), new Vector3(1.6f, 0.2f, 0.6f), H(0x8a6bff));
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI / 4f + Mathf.PI / 8f;
                g.Box(new Vector3(Mathf.Cos(a) * 5.2f, 0.36f, Mathf.Sin(a) * 5.2f), new Vector3(0.7f, 0.06f, 0.7f), H(0x8a6bff));
            }
            g.Pop();
        }

        static void Tower(MeshBuilder s, MeshBuilder g, BuildingDef b)
        {
            s.Push(b.Pos, 0f);
            s.Cylinder(new Vector3(0f, -1.2f, 0f), 14f, 4.0f, 3.5f, 12, Stone, true);
            s.Cylinder(new Vector3(0f, 12.6f, 0f), 0.7f, 4.5f, 4.5f, 12, StoneDark, true);
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f;
                s.Box(new Vector3(Mathf.Cos(a) * 4.1f, 13.6f, Mathf.Sin(a) * 4.1f), new Vector3(1.0f, 1.0f, 1.0f), Stone);
            }
            s.Cone(new Vector3(0f, 13.2f, 0f), 6f, 3.2f, 12, H(0x3d4f73));
            s.Pop();
            g.Push(b.Pos, 0f);
            for (int i = 0; i < 4; i++) { float a = i * Mathf.PI / 2f; g.Box(new Vector3(Mathf.Cos(a) * 3.6f, 8f, Mathf.Sin(a) * 3.6f), new Vector3(0.4f, 1.2f, 0.4f), Glass); }
            g.Pop();
        }

        static void Keep(MeshBuilder s, MeshBuilder g, BuildingDef b, HeightField hf)
        {
            float w = b.W, d = b.D;
            s.Push(b.Pos, 0f);
            float fy; Foundation(s, b, hf, out fy);
            s.BoxBase(new Vector3(0f, fy, 0f), new Vector3(w, 12f, d), H(0xb9b3a6));
            s.BoxBase(new Vector3(0f, fy + 12f, 0f), new Vector3(w + 1.2f, 0.8f, d + 1.2f), StoneDark);
            for (int i = 0; i < 4; i++)
            {
                float px = (i & 1) == 0 ? -w * 0.5f : w * 0.5f, pz = (i & 2) == 0 ? -d * 0.5f : d * 0.5f;
                s.Cylinder(new Vector3(px, fy, pz), 17f, 2.0f, 1.8f, 10, Stone, true);
                s.Cone(new Vector3(px, fy + 17f, pz), 4.5f, 2.6f, 10, H(0x3d4f73));
            }
            for (int i = -4; i <= 4; i++) s.Box(new Vector3(i * 2.2f, fy + 13.6f, -d * 0.5f - 0.5f), new Vector3(1.2f, 1.2f, 1.2f), Stone);
            s.Box(new Vector3(0f, fy + 2.2f, -d * 0.5f - 0.05f), new Vector3(3.4f, 4.4f, 0.4f), H(0x3e2a1b));
            for (int i = -1; i <= 1; i += 2) s.Box(new Vector3(i * 5f, fy + 8f, -d * 0.5f - 0.05f), new Vector3(1.8f, 5.0f, 0.1f), H(0xa8262e));
            s.Pop();
            g.Push(b.Pos, 0f);
            for (int i = -2; i <= 2; i++) g.Box(new Vector3(i * 3.2f, fy + 7f, -d * 0.5f - 0.05f), new Vector3(0.7f, 1.4f, 0.1f), Glass);
            g.Pop();
        }

        /// <summary>Visual for one destructible wall block (own mesh so it can be removed).</summary>
        public static Mesh BlockMesh(BlockDef bd)
        {
            MeshBuilder m = new MeshBuilder();
            float n = SLMath.Hash(bd.Id, 5, 41);
            Color c = SLMath.Jitter(H(0xa49e92), 0.14f, n);
            m.Box(Vector3.zero, bd.Size, c);
            bool top = bd.Pos.y > (bd.Pos.y - 1.5f) && (bd.Id % 2 == 1);
            if (top)
            {
                m.Box(new Vector3(-0.8f, bd.Size.y * 0.5f + 0.35f, 0f), new Vector3(0.9f, 0.7f, bd.Size.z * 0.7f), c);
                m.Box(new Vector3(0.8f, bd.Size.y * 0.5f + 0.35f, 0f), new Vector3(0.9f, 0.7f, bd.Size.z * 0.7f), c);
            }
            return m.ToMesh("SL_Block");
        }
    }
}
