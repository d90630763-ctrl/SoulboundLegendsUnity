using UnityEngine;
using SL.Core;

namespace SL.Actors
{
    /// <summary>Plain data the creature builder needs (kept free of gameplay assemblies).</summary>
    public sealed class CreatureStyle
    {
        public string Plan; public float Scale; public int ColA, ColB, ColC; public bool Ranged, Flying, Boss; public string Id;
    }

    public sealed class CreatureRig : MonoBehaviour
    {
        public string Plan; public bool Boss, Ranged;
        public Transform Body, Head, Tail;
        public Transform[] Legs = new Transform[0], Arms = new Transform[0], Wings = new Transform[0];
        public float BaseY;
        float phase, move, moveT, atk, hit, t; bool dead; float deadT;
        Vector3 bodyScale = Vector3.one;

        void Start() { if (Body != null) { BaseY = Body.localPosition.y; bodyScale = Body.localScale; } }
        public void SetMove(float v) { moveT = Mathf.Clamp01(v); }
        public void Attack() { atk = 1f; }
        public void Hit() { hit = 1f; }
        public void SetDead(bool d) { if (d && !dead) deadT = 0f; dead = d; }

        void Update()
        {
            if (Body == null) return;
            float dt = Time.deltaTime; t += dt;
            move = Mathf.Lerp(move, moveT, 1f - Mathf.Exp(-8f * dt));
            phase += dt * (3f + move * 7f);
            atk = Mathf.Max(0f, atk - dt * 2.2f); hit = Mathf.Max(0f, hit - dt * 4f);
            float s = Mathf.Sin(phase), a = Mathf.Sin(atk * Mathf.PI);

            if (dead)
            {
                deadT = Mathf.Min(1f, deadT + dt * 1.6f);
                Body.localRotation = Quaternion.Euler(0f, 0f, 90f * deadT);
                Body.localPosition = new Vector3(0f, Mathf.Lerp(BaseY, BaseY * 0.3f, deadT), 0f);
                return;
            }
            Body.localRotation = Quaternion.Euler(-hit * 14f, 0f, 0f);
            switch (Plan)
            {
                case "blob":
                    {
                        float hop = Mathf.Abs(Mathf.Sin(phase * 0.8f)) * (0.2f + move * 0.5f);
                        Body.localPosition = new Vector3(0f, BaseY + hop + a * 0.1f, 0f);
                        float sq = 1f + Mathf.Sin(phase * 1.6f) * 0.08f - a * 0.2f;
                        Body.localScale = new Vector3(bodyScale.x / sq, bodyScale.y * sq, bodyScale.z / sq);
                        break;
                    }
                case "quad":
                case "drake":
                    {
                        Body.localPosition = new Vector3(0f, BaseY + Mathf.Abs(Mathf.Cos(phase)) * 0.05f * move, 0f);
                        for (int i = 0; i < Legs.Length; i++) if (Legs[i] != null) Legs[i].localRotation = Quaternion.Euler(Mathf.Sin(phase + (i % 2 == 0 ? 0f : Mathf.PI) + (i >= 2 ? 1.5f : 0f)) * 38f * move, 0f, 0f);
                        if (Head != null) Head.localRotation = Quaternion.Euler(a * -30f + Mathf.Sin(t * 1.3f) * 3f, 0f, 0f);
                        if (Tail != null) Tail.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 3f) * 18f, 0f);
                        for (int i = 0; i < Wings.Length; i++) if (Wings[i] != null) Wings[i].localRotation = Quaternion.Euler(0f, 0f, (i == 0 ? 1f : -1f) * (10f + Mathf.Sin(t * 3f) * 12f + a * 30f));
                        break;
                    }
                case "float":
                    {
                        Body.localPosition = new Vector3(0f, BaseY + Mathf.Sin(t * 2f) * 0.18f, 0f);
                        Body.localRotation = Quaternion.Euler(move * 12f - a * 18f, Mathf.Sin(t) * 12f, Mathf.Sin(t * 1.4f) * 5f);
                        for (int i = 0; i < Arms.Length; i++) if (Arms[i] != null) Arms[i].localRotation = Quaternion.Euler(-30f * a + Mathf.Sin(t * 2f + i) * 10f, 0f, (i == 0 ? 1f : -1f) * 12f);
                        if (Head != null) Head.Rotate(0f, 40f * dt, 0f);
                        break;
                    }
                case "flyer":
                    {
                        Body.localPosition = new Vector3(0f, BaseY + Mathf.Sin(t * 5f) * 0.12f, 0f);
                        for (int i = 0; i < Wings.Length; i++) if (Wings[i] != null) Wings[i].localRotation = Quaternion.Euler(0f, 0f, (i == 0 ? 1f : -1f) * Mathf.Sin(t * 9f) * 38f);
                        break;
                    }
                default: // heavy biped
                    {
                        Body.localPosition = new Vector3(0f, BaseY + Mathf.Abs(Mathf.Cos(phase)) * 0.08f * move, 0f);
                        Body.localRotation = Quaternion.Euler(-hit * 10f + a * 10f, Mathf.Sin(phase) * 5f * move, 0f);
                        if (Legs.Length >= 2) { Legs[0].localRotation = Quaternion.Euler(s * 28f * move, 0f, 0f); Legs[1].localRotation = Quaternion.Euler(-s * 28f * move, 0f, 0f); }
                        if (Arms.Length >= 2)
                        {
                            Arms[0].localRotation = Quaternion.Euler(-s * 25f * move - a * 150f, 0f, 6f);
                            Arms[1].localRotation = Quaternion.Euler(s * 25f * move - a * 150f, 0f, -6f);
                        }
                        if (Head != null) Head.localRotation = Quaternion.Euler(a * 14f, 0f, 0f);
                        break;
                    }
            }
        }
    }

    public static class CreatureBuilder
    {
        static Color H(int c) { return SLMath.Hex(c); }

        static GameObject Part(Transform parent, string name, Vector3 pos, MeshBuilder mb, bool glow, Color gc)
        {
            GameObject g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            if (mb != null)
            {
                g.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh(name);
                g.AddComponent<MeshRenderer>().sharedMaterial = glow ? SLMat.Lit(Color.white, 0.4f, 0f, gc * 1.6f) : SLMat.Lit(Color.white, 0.3f, 0f);
            }
            return g;
        }

        public static GameObject Build(CreatureStyle d)
        {
            GameObject root = new GameObject("Mob_" + d.Id);
            Transform body = new GameObject("body").transform; body.SetParent(root.transform, false);
            CreatureRig rig = root.AddComponent<CreatureRig>();
            rig.Plan = d.Plan; rig.Body = body; rig.Boss = d.Boss; rig.Ranged = d.Ranged;
            Color A = H(d.ColA), B = H(d.ColB), C = H(d.ColC);
            switch (d.Plan)
            {
                case "blob": Blob(body, A, B, C, rig); break;
                case "quad": Quad(body, A, B, C, rig, false); break;
                case "drake": Quad(body, A, B, C, rig, true); break;
                case "float": Floater(body, A, B, C, rig, d.Ranged); break;
                case "flyer": Flyer(body, A, B, C, rig); break;
                default: Heavy(body, A, B, C, rig, d.Boss); break;
            }
            rig.BaseY = body.localPosition.y;
            root.transform.localScale = Vector3.one * d.Scale;
            return root;
        }

        static void Blob(Transform body, Color A, Color B, Color C, CreatureRig rig)
        {
            body.localPosition = new Vector3(0f, 0.5f, 0f);
            MeshBuilder m = new MeshBuilder();
            m.Ellipsoid(Vector3.zero, new Vector3(0.62f, 0.52f, 0.62f), A, 12, 8);
            for (int i = 0; i < 6; i++) { float a = i * 1.1f; m.Push(new Vector3(Mathf.Cos(a) * 0.3f, 0.42f + (i % 2) * 0.04f, Mathf.Sin(a) * 0.3f), Quaternion.Euler(Mathf.Sin(a) * 25f, 0, -Mathf.Cos(a) * 25f), Vector3.one); m.Cone(Vector3.zero, 0.22f, 0.05f, 5, B); m.Pop(); }
            m.Ellipsoid(new Vector3(0, -0.28f, 0), new Vector3(0.66f, 0.12f, 0.66f), C, 12, 4);
            Part(body, "mesh", Vector3.zero, m, false, B);
            MeshBuilder e = new MeshBuilder();
            for (int s = -1; s <= 1; s += 2) { e.Ellipsoid(new Vector3(s * 0.2f, 0.08f, 0.5f), new Vector3(0.1f, 0.12f, 0.06f), Color.white, 8, 4); }
            Part(body, "eyes", Vector3.zero, e, false, B);
            MeshBuilder p = new MeshBuilder();
            for (int s = -1; s <= 1; s += 2) p.Ellipsoid(new Vector3(s * 0.2f, 0.07f, 0.54f), new Vector3(0.05f, 0.07f, 0.03f), Color.black, 6, 3);
            Part(body, "pupils", Vector3.zero, p, false, B);
            MeshBuilder g = new MeshBuilder(); g.Ellipsoid(new Vector3(0, -0.02f, 0), new Vector3(0.25f, 0.2f, 0.25f), B, 8, 5);
            Part(body, "core", Vector3.zero, g, true, B);
        }

        static void Leg(Transform body, string n, Vector3 pos, float len, float r, Color c, Color paw, out Transform t)
        {
            MeshBuilder m = new MeshBuilder();
            m.Loft(new[] { new Vector3(0, 0.05f, 0), new Vector3(0, -len * 0.5f, 0.02f), new Vector3(0, -len, 0.0f) }, new[] { r, r * 0.8f, r * 0.55f }, new[] { r, r * 0.8f, r * 0.55f }, 7, c, false, false);
            m.Ellipsoid(new Vector3(0, -len, 0.05f), new Vector3(r * 0.8f, r * 0.45f, r * 1.3f), paw, 7, 4);
            t = Part(body, n, pos, m, false, c).transform;
        }

        static void Quad(Transform body, Color A, Color B, Color C, CreatureRig rig, bool drake)
        {
            float k = drake ? 1.25f : 1f;
            body.localPosition = new Vector3(0f, 0.95f * k, 0f);
            MeshBuilder m = new MeshBuilder();
            m.Ellipsoid(new Vector3(0, 0, -0.1f), new Vector3(0.36f, 0.34f, 0.72f) * k, A, 12, 7);
            m.Ellipsoid(new Vector3(0, 0.04f, 0.45f * k), new Vector3(0.34f, 0.36f, 0.4f) * k, A, 10, 6);
            m.Ellipsoid(new Vector3(0, -0.18f * k, 0.05f), new Vector3(0.3f, 0.18f, 0.6f) * k, B, 10, 5);
            for (int i = 0; i < 6; i++) { m.Push(new Vector3(0, 0.34f * k, -0.35f * k + i * 0.18f * k), Quaternion.Euler(-15f, 0, 0), Vector3.one); m.Cone(Vector3.zero, drake ? 0.3f : 0.16f, 0.06f, 5, C); m.Pop(); }
            Part(body, "mesh", Vector3.zero, m, false, C);
            MeshBuilder h = new MeshBuilder();
            h.Ellipsoid(new Vector3(0, 0, 0.08f), new Vector3(0.26f, 0.24f, 0.28f) * k, A, 10, 6);
            h.Push(new Vector3(0, -0.06f * k, 0.3f * k), Quaternion.Euler(90f, 0, 0), Vector3.one);
            h.Loft(new[] { Vector3.zero, new Vector3(0, 0.18f * k, 0), new Vector3(0, 0.34f * k, 0) }, new[] { 0.13f * k, 0.1f * k, 0.06f * k }, new[] { 0.1f * k, 0.08f * k, 0.05f * k }, 8, B, false, true);
            h.Pop();
            for (int s = -1; s <= 1; s += 2)
            {
                h.Push(new Vector3(s * 0.14f * k, 0.22f * k, -0.02f), Quaternion.Euler(-10f, 0, -s * 15f), Vector3.one); h.Cone(Vector3.zero, drake ? 0.34f : 0.2f, 0.07f, 5, drake ? B : A); h.Pop();
            }
            Transform head = Part(body, "head", new Vector3(0, 0.22f * k, 0.78f * k), h, false, C).transform;
            MeshBuilder ey = new MeshBuilder();
            for (int s = -1; s <= 1; s += 2) ey.Ellipsoid(new Vector3(s * 0.12f * k, 0.07f * k, 0.28f * k), new Vector3(0.04f, 0.03f, 0.03f), B, 6, 3);
            Part(head, "eyes", Vector3.zero, ey, true, B);
            rig.Head = head;
            MeshBuilder t = new MeshBuilder();
            t.Loft(new[] { new Vector3(0, 0, 0), new Vector3(0, -0.1f, -0.4f * k), new Vector3(0, -0.05f, -0.8f * k), new Vector3(0, 0.1f, -1.1f * k) }, new[] { 0.1f * k, 0.08f * k, 0.05f * k, 0.01f }, new[] { 0.1f * k, 0.08f * k, 0.05f * k, 0.01f }, 7, A, false, true);
            rig.Tail = Part(body, "tail", new Vector3(0, 0.05f, -0.75f * k), t, false, A).transform;
            Transform[] legs = new Transform[4];
            float ll = 0.75f * k;
            Leg(body, "legFL", new Vector3(-0.22f * k, -0.1f, 0.45f * k), ll, 0.09f * k, A, C, out legs[0]);
            Leg(body, "legFR", new Vector3(0.22f * k, -0.1f, 0.45f * k), ll, 0.09f * k, A, C, out legs[1]);
            Leg(body, "legBL", new Vector3(-0.22f * k, -0.1f, -0.5f * k), ll, 0.11f * k, A, C, out legs[2]);
            Leg(body, "legBR", new Vector3(0.22f * k, -0.1f, -0.5f * k), ll, 0.11f * k, A, C, out legs[3]);
            rig.Legs = legs;
            if (drake)
            {
                Transform[] wings = new Transform[2];
                for (int s = -1; s <= 1; s += 2)
                {
                    MeshBuilder w = new MeshBuilder();
                    w.Strip(new[] { new Vector3(0, 0, 0), new Vector3(s * 0.9f, 0.5f, -0.1f), new Vector3(s * 1.8f, 0.3f, -0.3f) }, new[] { new Vector3(0, 0, -0.6f), new Vector3(s * 0.9f, 0.1f, -0.8f), new Vector3(s * 1.7f, -0.2f, -1.0f) }, B);
                    w.Box(new Vector3(s * 0.9f, 0.3f, -0.1f), new Vector3(1.8f, 0.06f, 0.06f), C);
                    wings[s < 0 ? 0 : 1] = Part(body, s < 0 ? "wingL" : "wingR", new Vector3(s * 0.25f, 0.28f, 0.0f), w, false, B).transform;
                }
                rig.Wings = wings;
            }
        }

        static void Floater(Transform body, Color A, Color B, Color C, CreatureRig rig, bool petals)
        {
            body.localPosition = new Vector3(0f, 1.3f, 0f);
            MeshBuilder m = new MeshBuilder();
            if (petals)
            {
                m.Ellipsoid(Vector3.zero, new Vector3(0.38f, 0.38f, 0.38f), A, 10, 6);
                for (int i = 0; i < 8; i++) { m.Push(Vector3.zero, Quaternion.Euler(0, i * 45f, 50f), Vector3.one); m.Cone(new Vector3(0, 0.2f, 0), 0.5f, 0.12f, 5, i % 2 == 0 ? B : A); m.Pop(); }
                m.Loft(new[] { new Vector3(0, -0.3f, 0), new Vector3(0, -0.7f, 0), new Vector3(0, -1.1f, 0) }, new[] { 0.14f, 0.08f, 0.01f }, new[] { 0.14f, 0.08f, 0.01f }, 6, C, false, true);
                Part(body, "mesh", Vector3.zero, m, false, B);
                MeshBuilder g = new MeshBuilder(); g.Ellipsoid(Vector3.zero, new Vector3(0.26f, 0.26f, 0.26f), B, 8, 5);
                rig.Head = Part(body, "core", Vector3.zero, g, true, B).transform;
            }
            else
            {
                m.Loft(new[] { new Vector3(0, 0.55f, 0), new Vector3(0, 0.2f, 0), new Vector3(0, -0.4f, 0), new Vector3(0, -1.0f, 0) }, new[] { 0.26f, 0.42f, 0.34f, 0.02f }, new[] { 0.24f, 0.36f, 0.3f, 0.02f }, 10, A, false, true);
                m.Ellipsoid(new Vector3(0, 0.72f, 0), new Vector3(0.24f, 0.26f, 0.24f), C, 10, 6);
                m.Push(new Vector3(0, 0.76f, -0.03f), Quaternion.Euler(-30f, 0, 0), Vector3.one); m.Cone(Vector3.zero, 0.34f, 0.27f, 8, A); m.Pop();
                Part(body, "mesh", Vector3.zero, m, false, B);
                MeshBuilder e = new MeshBuilder();
                for (int s = -1; s <= 1; s += 2) e.Ellipsoid(new Vector3(s * 0.08f, 0.72f, 0.18f), new Vector3(0.035f, 0.05f, 0.03f), B, 6, 3);
                Part(body, "eyes", Vector3.zero, e, true, B);
                Transform[] arms = new Transform[2];
                for (int s = -1; s <= 1; s += 2)
                {
                    MeshBuilder a = new MeshBuilder();
                    a.Loft(new[] { new Vector3(0, 0, 0), new Vector3(s * 0.1f, -0.3f, 0.12f), new Vector3(s * 0.12f, -0.6f, 0.3f) }, new[] { 0.06f, 0.04f, 0.015f }, new[] { 0.06f, 0.04f, 0.015f }, 6, A, false, true);
                    arms[s < 0 ? 0 : 1] = Part(body, "arm", new Vector3(s * 0.35f, 0.4f, 0.1f), a, false, B).transform;
                }
                rig.Arms = arms;
            }
        }

        static void Flyer(Transform body, Color A, Color B, Color C, CreatureRig rig)
        {
            body.localPosition = new Vector3(0f, 1.6f, 0f);
            MeshBuilder m = new MeshBuilder();
            m.Ellipsoid(Vector3.zero, new Vector3(0.28f, 0.3f, 0.5f), A, 10, 6);
            m.Ellipsoid(new Vector3(0, 0.12f, 0.5f), new Vector3(0.18f, 0.18f, 0.2f), B, 8, 5);
            m.Push(new Vector3(0, 0.1f, 0.72f), Quaternion.Euler(90f, 0, 0), Vector3.one); m.Cone(Vector3.zero, 0.2f, 0.07f, 5, C); m.Pop();
            m.Loft(new[] { new Vector3(0, -0.05f, -0.45f), new Vector3(0, -0.1f, -0.75f), new Vector3(0, -0.12f, -1.0f) }, new[] { 0.1f, 0.14f, 0.1f }, new[] { 0.02f, 0.02f, 0.02f }, 6, B, false, true);
            for (int s = -1; s <= 1; s += 2) { m.Push(new Vector3(s * 0.12f, -0.3f, 0.1f), Quaternion.identity, Vector3.one); m.Cone(Vector3.zero, -0.2f, 0.03f, 4, C); m.Pop(); }
            Part(body, "mesh", Vector3.zero, m, false, B);
            MeshBuilder e = new MeshBuilder();
            for (int s = -1; s <= 1; s += 2) e.Ellipsoid(new Vector3(s * 0.08f, 0.16f, 0.62f), new Vector3(0.03f, 0.03f, 0.02f), Color.yellow, 6, 3);
            Part(body, "eyes", Vector3.zero, e, true, new Color(1f, 0.9f, 0.2f));
            Transform[] wings = new Transform[2];
            for (int s = -1; s <= 1; s += 2)
            {
                MeshBuilder w = new MeshBuilder();
                for (int f = 0; f < 5; f++) { w.Push(new Vector3(s * (0.15f + f * 0.32f), 0, -f * 0.12f), Quaternion.Euler(0, 0, s * f * 8f), Vector3.one); w.Box(new Vector3(s * 0.25f, 0, 0), new Vector3(0.6f, 0.03f, 0.3f - f * 0.03f), f % 2 == 0 ? B : A); w.Pop(); }
                wings[s < 0 ? 0 : 1] = Part(body, s < 0 ? "wingL" : "wingR", new Vector3(s * 0.2f, 0.12f, 0f), w, false, B).transform;
            }
            rig.Wings = wings;
        }

        static void Heavy(Transform body, Color A, Color B, Color C, CreatureRig rig, bool boss)
        {
            body.localPosition = new Vector3(0f, 1.45f, 0f);
            MeshBuilder m = new MeshBuilder();
            m.Ellipsoid(new Vector3(0, 0.1f, 0), new Vector3(0.62f, 0.7f, 0.42f), A, 10, 6);
            m.Ellipsoid(new Vector3(0, -0.55f, 0), new Vector3(0.45f, 0.3f, 0.34f), A, 9, 5);
            for (int i = 0; i < 5; i++) { m.Push(new Vector3(-0.35f + i * 0.17f, 0.55f - Mathf.Abs(i - 2) * 0.05f, -0.3f), Quaternion.Euler(-35f, 0, 0), Vector3.one); m.Cone(Vector3.zero, boss ? 0.5f : 0.3f, 0.09f, 5, B); m.Pop(); }
            for (int s = -1; s <= 1; s += 2) m.Ellipsoid(new Vector3(s * 0.7f, 0.55f, 0), new Vector3(0.3f, 0.26f, 0.3f), B, 8, 5);
            Part(body, "mesh", Vector3.zero, m, false, C);
            MeshBuilder g = new MeshBuilder();
            g.Box(new Vector3(0, 0.15f, 0.4f), new Vector3(0.28f, 0.28f, 0.04f), C);
            g.Box(new Vector3(0, 0.15f, 0.41f), new Vector3(0.06f, 0.5f, 0.04f), C);
            Part(body, "runes", Vector3.zero, g, true, C);
            MeshBuilder h = new MeshBuilder();
            h.Ellipsoid(Vector3.zero, new Vector3(0.24f, 0.26f, 0.26f), B, 9, 5);
            if (boss) { for (int s = -1; s <= 1; s += 2) { h.Push(new Vector3(s * 0.2f, 0.18f, 0), Quaternion.Euler(0, 0, -s * 40f), Vector3.one); h.Cone(Vector3.zero, 0.55f, 0.07f, 5, A); h.Pop(); } }
            Transform head = Part(body, "head", new Vector3(0, 0.95f, 0.05f), h, false, B).transform;
            MeshBuilder ey = new MeshBuilder();
            for (int s = -1; s <= 1; s += 2) ey.Box(new Vector3(s * 0.1f, 0.03f, 0.22f), new Vector3(0.09f, 0.04f, 0.03f), C);
            Part(head, "eyes", Vector3.zero, ey, true, C);
            rig.Head = head;
            Transform[] arms = new Transform[2], legs = new Transform[2];
            for (int s = -1; s <= 1; s += 2)
            {
                MeshBuilder a = new MeshBuilder();
                a.Loft(new[] { new Vector3(0, 0.1f, 0), new Vector3(0, -0.5f, 0.05f), new Vector3(0, -1.0f, 0.1f) }, new[] { 0.2f, 0.17f, 0.15f }, new[] { 0.2f, 0.17f, 0.15f }, 8, A, false, false);
                a.Ellipsoid(new Vector3(0, -1.1f, 0.12f), new Vector3(0.26f, 0.24f, 0.26f), B, 8, 5);
                arms[s < 0 ? 0 : 1] = Part(body, "arm", new Vector3(s * 0.85f, 0.5f, 0), a, false, B).transform;
                MeshBuilder l = new MeshBuilder();
                l.Loft(new[] { new Vector3(0, 0, 0), new Vector3(0, -0.6f, 0), new Vector3(0, -1.1f, 0) }, new[] { 0.22f, 0.19f, 0.17f }, new[] { 0.22f, 0.19f, 0.17f }, 8, A, false, false);
                l.Box(new Vector3(0, -1.2f, 0.1f), new Vector3(0.36f, 0.16f, 0.5f), B);
                legs[s < 0 ? 0 : 1] = Part(body, "leg", new Vector3(s * 0.3f, -0.8f, 0), l, false, B).transform;
            }
            rig.Arms = arms; rig.Legs = legs;
            if (boss)
            {
                MeshBuilder cape = new MeshBuilder();
                cape.Strip(new[] { new Vector3(-0.55f, 0.6f, -0.3f), new Vector3(-0.7f, -0.6f, -0.45f), new Vector3(-0.8f, -1.6f, -0.5f) }, new[] { new Vector3(0.55f, 0.6f, -0.3f), new Vector3(0.7f, -0.6f, -0.45f), new Vector3(0.8f, -1.6f, -0.5f) }, C * 0.6f);
                Part(body, "cape", Vector3.zero, cape, false, C);
            }
        }
    }
}
