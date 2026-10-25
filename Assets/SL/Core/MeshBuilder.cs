using System.Collections.Generic;
using UnityEngine;

namespace SL.Core
{
    /// <summary>
    /// Procedural mesh builder. Shapes are added with vertex colours so a whole world chunk can be a single mesh.
    /// Optionally records bone weights (rigid skinning) for skinned characters.
    /// </summary>
    public sealed class MeshBuilder
    {
        public readonly List<Vector3> V = new List<Vector3>();
        public readonly List<Vector3> N = new List<Vector3>();
        public readonly List<Color> C = new List<Color>();
        public readonly List<Vector2> UV = new List<Vector2>();
        public readonly List<int> T = new List<int>();
        public readonly List<BoneWeight> W = new List<BoneWeight>();
        public int Bone = 0;
        public int Bone2 = -1;
        public float Blend = 0f;
        public float[] RingBlend;
        public bool Skinned = false;

        Matrix4x4 xf = Matrix4x4.identity;
        readonly Stack<Matrix4x4> stack = new Stack<Matrix4x4>();

        public int Count { get { return V.Count; } }

        public void Push(Matrix4x4 m) { stack.Push(xf); xf = xf * m; }
        public void Push(Vector3 pos, float yawDeg) { Push(Matrix4x4.TRS(pos, Quaternion.Euler(0f, yawDeg, 0f), Vector3.one)); }
        public void Push(Vector3 pos, Quaternion rot, Vector3 scale) { Push(Matrix4x4.TRS(pos, rot, scale)); }
        public void Pop() { xf = stack.Pop(); }

        public Vector3 Tx(Vector3 p) { return xf.MultiplyPoint3x4(p); }

        public int Vert(Vector3 p, Color c)
        {
            V.Add(xf.MultiplyPoint3x4(p));
            C.Add(c);
            UV.Add(Vector2.zero);
            BoneWeight bw = new BoneWeight();
            if (Bone2 >= 0 && Blend > 0.001f) { bw.boneIndex0 = Bone; bw.weight0 = 1f - Blend; bw.boneIndex1 = Bone2; bw.weight1 = Blend; }
            else { bw.boneIndex0 = Bone; bw.weight0 = 1f; }
            W.Add(bw);
            return V.Count - 1;
        }

        public int Vert(Vector3 p, Color c, Vector2 uv)
        {
            int i = Vert(p, c);
            UV[i] = uv;
            return i;
        }

        public void Tri(int a, int b, int c, Vector3 inside)
        {
            Vector3 n = Vector3.Cross(V[b] - V[a], V[c] - V[a]);
            Vector3 mid = (V[a] + V[b] + V[c]) / 3f;
            if (Vector3.Dot(n, mid - inside) >= 0f) { T.Add(a); T.Add(b); T.Add(c); }
            else { T.Add(a); T.Add(c); T.Add(b); }
        }

        public void Quad(int a, int b, int c, int d, Vector3 inside)
        {
            Vector3 n = Vector3.Cross(V[b] - V[a], V[c] - V[a]) + Vector3.Cross(V[c] - V[a], V[d] - V[a]);
            Vector3 mid = (V[a] + V[b] + V[c] + V[d]) * 0.25f;
            if (Vector3.Dot(n, mid - inside) >= 0f)
            { T.Add(a); T.Add(b); T.Add(c); T.Add(a); T.Add(c); T.Add(d); }
            else
            { T.Add(a); T.Add(c); T.Add(b); T.Add(a); T.Add(d); T.Add(c); }
        }

        /// <summary>Flat-shaded box centred on c.</summary>
        public void Box(Vector3 c, Vector3 size, Color col)
        {
            Vector3 h = size * 0.5f;
            Vector3 inside = xf.MultiplyPoint3x4(c);
            for (int axis = 0; axis < 3; axis++)
            {
                for (int sgn = -1; sgn <= 1; sgn += 2)
                {
                    Vector3 n = Vector3.zero; n[axis] = sgn;
                    int u = (axis + 1) % 3, v = (axis + 2) % 3;
                    Vector3 o = c + new Vector3(n.x * h.x, n.y * h.y, n.z * h.z);
                    Vector3 du = Vector3.zero; du[u] = h[u];
                    Vector3 dv = Vector3.zero; dv[v] = h[v];
                    int a = Vert(o - du - dv, col), b = Vert(o - du + dv, col), cc = Vert(o + du + dv, col), d = Vert(o + du - dv, col);
                    Quad(a, b, cc, d, inside);
                }
            }
        }

        /// <summary>Box from a base-centre point (sits on the ground).</summary>
        public void BoxBase(Vector3 baseCentre, Vector3 size, Color col)
        {
            Box(baseCentre + new Vector3(0f, size.y * 0.5f, 0f), size, col);
        }

        /// <summary>Smooth lofted tube through ring centres with elliptical radii (rings horizontal).</summary>
        public void Loft(Vector3[] centres, float[] rx, float[] rz, int seg, Color col, bool capA, bool capB)
        {
            int n = centres.Length;
            int[][] ring = new int[n][];
            for (int i = 0; i < n; i++)
            {
                ring[i] = new int[seg];
                if (RingBlend != null && i < RingBlend.Length) Blend = RingBlend[i];
                for (int k = 0; k < seg; k++)
                {
                    float a = (float)k / seg * Mathf.PI * 2f;
                    Vector3 p = centres[i] + new Vector3(Mathf.Cos(a) * Mathf.Max(rx[i], 0.0001f), 0f, Mathf.Sin(a) * Mathf.Max(rz[i], 0.0001f));
                    ring[i][k] = Vert(p, col);
                }
            }
            for (int i = 0; i < n - 1; i++)
            {
                Vector3 inside = xf.MultiplyPoint3x4((centres[i] + centres[i + 1]) * 0.5f);
                for (int k = 0; k < seg; k++)
                {
                    int k2 = (k + 1) % seg;
                    Quad(ring[i][k], ring[i + 1][k], ring[i + 1][k2], ring[i][k2], inside);
                }
            }
            RingBlend = null; Blend = 0f;
            if (capA) Cap(ring[0], centres[0], col, centres[Mathf.Min(1, n - 1)]);
            if (capB) Cap(ring[n - 1], centres[n - 1], col, centres[Mathf.Max(0, n - 2)]);
        }

        void Cap(int[] ringIdx, Vector3 centre, Color col, Vector3 towards)
        {
            int c = Vert(centre, col);
            Vector3 inside = xf.MultiplyPoint3x4(towards);
            for (int k = 0; k < ringIdx.Length; k++)
                Tri(c, ringIdx[k], ringIdx[(k + 1) % ringIdx.Length], inside);
        }

        public void Ellipsoid(Vector3 c, Vector3 r, Color col, int seg, int rings)
        {
            int n = rings + 1;
            Vector3[] cs = new Vector3[n]; float[] rx = new float[n]; float[] rz = new float[n];
            for (int i = 0; i < n; i++)
            {
                float phi = -Mathf.PI * 0.5f + Mathf.PI * i / rings;
                cs[i] = c + new Vector3(0f, r.y * Mathf.Sin(phi), 0f);
                float f = Mathf.Cos(phi);
                rx[i] = r.x * f; rz[i] = r.z * f;
            }
            Loft(cs, rx, rz, seg, col, false, false);
        }

        public void Cylinder(Vector3 baseCentre, float height, float r0, float r1, int seg, Color col, bool caps)
        {
            Vector3[] cs = { baseCentre, baseCentre + new Vector3(0f, height, 0f) };
            Loft(cs, new[] { r0, r1 }, new[] { r0, r1 }, seg, col, caps && r0 > 0.001f, caps && r1 > 0.001f);
        }

        public void Cone(Vector3 baseCentre, float height, float r, int seg, Color col)
        {
            Cylinder(baseCentre, height, r, 0f, seg, col, true);
        }

        /// <summary>Extruded triangle (gable roof). Ridge runs along local X, centred on c at ridge height.</summary>
        public void Gable(Vector3 baseCentre, float width, float depth, float rise, Color col)
        {
            float hw = width * 0.5f, hd = depth * 0.5f;
            Vector3 inside = xf.MultiplyPoint3x4(baseCentre + new Vector3(0f, rise * 0.3f, 0f));
            int a = Vert(baseCentre + new Vector3(-hw, 0f, -hd), col), b = Vert(baseCentre + new Vector3(hw, 0f, -hd), col);
            int c = Vert(baseCentre + new Vector3(hw, 0f, hd), col), d = Vert(baseCentre + new Vector3(-hw, 0f, hd), col);
            int r0 = Vert(baseCentre + new Vector3(-hw, rise, 0f), col), r1 = Vert(baseCentre + new Vector3(hw, rise, 0f), col);
            // slopes
            Quad(a, r0, r1, b, inside);
            Quad(d, c, r1, r0, inside);
            // gable ends
            Tri(a, d, r0, inside);
            Tri(b, r1, c, inside);
        }

        /// <summary>Double-sided flat strip (cloth, ribbon, grass blade) between two rows of points.</summary>
        public void Strip(Vector3[] left, Vector3[] right, Color col)
        {
            int n = left.Length;
            int[] l = new int[n], r = new int[n];
            for (int i = 0; i < n; i++) { l[i] = Vert(left[i], col); r[i] = Vert(right[i], col); }
            for (int i = 0; i < n - 1; i++)
            {
                T.Add(l[i]); T.Add(l[i + 1]); T.Add(r[i + 1]);
                T.Add(l[i]); T.Add(r[i + 1]); T.Add(r[i]);
                T.Add(l[i]); T.Add(r[i + 1]); T.Add(l[i + 1]);
                T.Add(l[i]); T.Add(r[i]); T.Add(r[i + 1]);
            }
        }

        public Mesh ToMesh(string name)
        {
            Mesh m = new Mesh();
            m.name = name;
            if (V.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(V);
            m.SetColors(C);
            m.SetUVs(0, UV);
            m.SetTriangles(T, 0);
            if (N.Count == V.Count) m.SetNormals(N); else m.RecalculateNormals();
            if (Skinned) m.boneWeights = W.ToArray();
            m.RecalculateBounds();
            return m;
        }
    }
}
