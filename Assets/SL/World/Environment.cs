using System.Collections.Generic;
using UnityEngine;
using SL.Core;

namespace SL.World
{
    /// <summary>Sky dome, sun/moon, day-night cycle, fog, rain, clouds and distant floating islands.</summary>
    public sealed class SkyEnvironment : MonoBehaviour
    {
        public static SkyEnvironment I;
        public float Hour = 9f;
        public float HoursPerRealMinute = 4f;
        public bool Lobby;
        Light sun; Transform dome, sunDisc, moonDisc, islands; Material skyMat;
        ParticleSystem rain; Camera cam;
        float weather;

        public static SkyEnvironment Create(Camera camera)
        {
            if (I != null) Destroy(I.gameObject);
            GameObject go = new GameObject("SL_Sky");
            I = go.AddComponent<SkyEnvironment>();
            I.cam = camera;
            I.Init();
            return I;
        }

        void Init()
        {
            Light existing = FindFirstObjectByType<Light>();
            if (existing != null && existing.type == LightType.Directional) sun = existing;
            else { GameObject l = new GameObject("Sun"); sun = l.AddComponent<Light>(); sun.type = LightType.Directional; }
            sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.7f;
            skyMat = SLMat.Sky();
            MeshBuilder mb = new MeshBuilder();
            mb.Ellipsoid(Vector3.zero, new Vector3(450f, 450f, 450f), Color.white, 16, 10);
            GameObject d = new GameObject("SkyDome");
            d.transform.SetParent(transform, false);
            d.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("SL_SkyDome");
            MeshRenderer mr = d.AddComponent<MeshRenderer>(); mr.sharedMaterial = skyMat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            dome = d.transform;
            sunDisc = Disc("Sun", new Color(1f, 0.95f, 0.7f), 26f);
            moonDisc = Disc("Moon", new Color(0.8f, 0.85f, 1f), 16f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.farClipPlane = 900f; cam.nearClipPlane = 0.2f; cam.backgroundColor = new Color(0.4f, 0.55f, 0.75f); }
            MakeRain();
            islands = new GameObject("FarIslands").transform; islands.SetParent(transform, false);
        }

        Transform Disc(string n, Color c, float size)
        {
            MeshBuilder mb = new MeshBuilder();
            mb.Ellipsoid(Vector3.zero, new Vector3(size, size, size), Color.white, 10, 6);
            GameObject g = new GameObject(n); g.transform.SetParent(transform, false);
            g.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("SL_" + n);
            MeshRenderer mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = SLMat.Lit(c, 0f, 0f, c * 1.5f); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            return g.transform;
        }

        void MakeRain()
        {
            GameObject g = new GameObject("Rain"); g.transform.SetParent(transform, false);
            rain = g.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule m = rain.main;
            m.startLifetime = 1.0f; m.startSpeed = 0f; m.startSize = 0.07f; m.maxParticles = 1500; m.loop = true;
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.gravityModifier = 6f;
            m.startColor = new Color(0.7f, 0.8f, 1f, 0.5f);
            ParticleSystem.ShapeModule sh = rain.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(40f, 1f, 40f);
            ParticleSystem.EmissionModule em = rain.emission; em.rateOverTime = 0f;
            ParticleSystemRenderer r = g.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = SLMat.Particle(new Color(0.7f, 0.85f, 1f, 0.6f));
            r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = 4f; r.velocityScale = 0.02f;
        }

        static readonly Color[] topK = { new Color(0.02f, 0.03f, 0.1f), new Color(0.16f, 0.2f, 0.45f), new Color(0.15f, 0.4f, 0.78f), new Color(0.12f, 0.35f, 0.72f), new Color(0.25f, 0.2f, 0.45f), new Color(0.02f, 0.03f, 0.1f) };
        static readonly Color[] horK = { new Color(0.05f, 0.07f, 0.16f), new Color(0.95f, 0.6f, 0.4f), new Color(0.72f, 0.86f, 0.98f), new Color(0.75f, 0.87f, 0.96f), new Color(0.95f, 0.5f, 0.35f), new Color(0.05f, 0.07f, 0.16f) };
        static readonly float[] keyH = { 0f, 6f, 9f, 16f, 19f, 24f };

        static Color Key(Color[] a, float h)
        {
            for (int i = 0; i < keyH.Length - 1; i++)
                if (h >= keyH[i] && h <= keyH[i + 1]) return Color.Lerp(a[i], a[i + 1], (h - keyH[i]) / (keyH[i + 1] - keyH[i]));
            return a[0];
        }

        void Update()
        {
            if (cam == null) return;
            if (!Lobby) Hour = (Hour + Time.deltaTime * HoursPerRealMinute / 60f) % 24f;
            float h = Hour;
            Color top = Key(topK, h), hor = Key(horK, h);
            weather = Mathf.Clamp01((SLMath.Noise(Time.time * 0.01f, 3f, 5) - 0.5f) * 3f + 0.2f);
            if (Lobby) weather = 0f;
            float grey = weather * 0.6f;
            top = Color.Lerp(top, new Color(0.35f, 0.38f, 0.42f) * Mathf.Clamp01(top.maxColorComponent + 0.3f), grey);
            hor = Color.Lerp(hor, new Color(0.5f, 0.52f, 0.56f) * Mathf.Clamp01(hor.maxColorComponent + 0.15f), grey);
            skyMat.SetColor("_Top", top); skyMat.SetColor("_Horizon", hor); skyMat.SetColor("_Bottom", hor * 0.6f);
            float ang = (h - 6f) / 24f * 360f;
            Quaternion rot = Quaternion.Euler(ang, -35f, 0f);
            sun.transform.rotation = rot;
            float day = Mathf.Clamp01(Mathf.Sin((h - 6f) / 12f * Mathf.PI));
            sun.intensity = Mathf.Lerp(0.05f, 1.15f, day) * (1f - grey * 0.5f);
            sun.color = Color.Lerp(new Color(1f, 0.55f, 0.35f), new Color(1f, 0.96f, 0.86f), Mathf.Clamp01(day * 2f));
            if (h < 5.5f || h > 18.5f) { sun.color = new Color(0.45f, 0.55f, 0.85f); sun.intensity = 0.25f; }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(new Color(0.12f, 0.14f, 0.25f), new Color(0.55f, 0.62f, 0.7f), day) * (1f - grey * 0.25f);
            Color fog = Color.Lerp(hor, top, 0.2f);
            RenderSettings.fogColor = fog; cam.backgroundColor = fog;
            RenderSettings.fogStartDistance = Mathf.Lerp(90f, 25f, weather); RenderSettings.fogEndDistance = Mathf.Lerp(380f, 130f, weather);
            Vector3 cp = cam.transform.position;
            dome.position = cp;
            Vector3 sd = Quaternion.Euler(ang, -35f, 0f) * Vector3.forward;
            sunDisc.position = cp - sd * 380f; moonDisc.position = cp + sd * 380f;
            sunDisc.gameObject.SetActive(day > 0.02f); moonDisc.gameObject.SetActive(day < 0.6f);
            if (islands != null) islands.position = new Vector3(cp.x * 0.94f, 0f, cp.z * 0.94f);
            ParticleSystem.EmissionModule em = rain.emission;
            em.rateOverTime = weather > 0.55f ? (weather - 0.5f) * 1800f : 0f;
            rain.transform.position = cp + new Vector3(0f, 14f, 0f);
        }

        public void SetHour(float h) { Hour = h; }
        public float Weather { get { return weather; } }

        public void BuildFarIslands(int count, float radius, int seed)
        {
            for (int i = islands.childCount - 1; i >= 0; i--) Destroy(islands.GetChild(i).gameObject);
            DetRng rng = new DetRng(seed);
            for (int i = 0; i < count; i++)
            {
                float a = (i + rng.Range(-0.3f, 0.3f)) / count * Mathf.PI * 2f, r = radius * rng.Range(0.9f, 1.4f);
                Vector3 p = new Vector3(Mathf.Cos(a) * r, rng.Range(25f, 110f), Mathf.Sin(a) * r);
                GameObject g = FloatingIsland.Build(rng.Range(18f, 40f), rng.Next() < 0.5f, i + seed, true);
                g.transform.SetParent(islands, false); g.transform.localPosition = p;
                g.AddComponent<Bob>().Init(rng.Range(0f, 6f), rng.Range(0.5f, 1.5f));
            }
        }
    }

    public sealed class Bob : MonoBehaviour
    {
        float t, amp; Vector3 start; bool init;
        public void Init(float phase, float a) { t = phase; amp = a; }
        void Update()
        {
            if (!init) { start = transform.localPosition; init = true; }
            t += Time.deltaTime * 0.4f;
            transform.localPosition = start + new Vector3(0f, Mathf.Sin(t) * amp, 0f);
        }
    }

    public static class FloatingIsland
    {
        static Color Hx(int c) { return SLMath.Hex(c); }

        public static GameObject Build(float radius, bool castle, int seed, bool waterfall)
        {
            MeshBuilder mb = new MeshBuilder();
            DetRng rng = new DetRng(seed * 13 + 5);
            int rings = 7; int seg = 14;
            // grass top disc (slight dome)
            Vector3[] cs = new Vector3[3]; float[] rx = new float[3], rz = new float[3];
            cs[0] = new Vector3(0f, 0.2f, 0f); cs[1] = new Vector3(0f, 0f, 0f); cs[2] = new Vector3(0f, -1.5f, 0f);
            rx[0] = radius * 0.2f; rz[0] = radius * 0.2f; rx[1] = radius; rz[1] = radius * 0.92f; rx[2] = radius * 0.92f; rz[2] = radius * 0.86f;
            mb.Loft(cs, rx, rz, seg, Hx(0x5fa043), true, false);
            // rock underside
            Vector3[] rc = new Vector3[rings]; float[] ru = new float[rings], rv = new float[rings];
            for (int i = 0; i < rings; i++)
            {
                float t = i / (float)(rings - 1);
                rc[i] = new Vector3(rng.Range(-1f, 1f) * t, -1.5f - t * radius * 1.1f, rng.Range(-1f, 1f) * t);
                float rr = radius * 0.92f * (1f - t * t * 0.97f) * rng.Range(0.85f, 1.1f);
                ru[i] = rr; rv[i] = rr * 0.93f;
            }
            Vector3[] rcs = rc;
            mb.Loft(rcs, ru, rv, seg, Hx(0x6b645c), false, false);
            for (int k = 0; k < 6; k++)
            {
                float a = k * 1.05f + rng.Range(0f, 0.5f);
                Vector3 p = new Vector3(Mathf.Cos(a) * radius * 0.6f, -2f - rng.Range(0f, radius * 0.4f), Mathf.Sin(a) * radius * 0.6f);
                mb.Cone(p + new Vector3(0f, -rng.Range(3f, 7f), 0f), rng.Range(3f, 7f), rng.Range(1f, 2.2f), 5, Hx(0x5a544d));
            }
            // trees
            for (int k = 0; k < (int)(radius * 0.7f); k++)
            {
                float a = rng.Range(0f, 6.28f), r = rng.Range(0.2f, 0.85f) * radius;
                Vector3 p = new Vector3(Mathf.Cos(a) * r, 0.2f, Mathf.Sin(a) * r);
                if (castle && r < radius * 0.35f) continue;
                if (rng.Next() < 0.6f) ZoneView.Oak(mb, p, rng.Range(0.7f, 1.2f), k + seed); else ZoneView.Pine(mb, p, rng.Range(0.7f, 1.2f), k + seed);
            }
            MeshBuilder gl = new MeshBuilder();
            if (castle)
            {
                Color st = Hx(0xd9d6cc), roof = Hx(0x3d6fb0);
                mb.Box(new Vector3(0f, 3f, 0f), new Vector3(radius * 0.4f, 6f, radius * 0.3f), st);
                mb.Cone(new Vector3(0f, 6f, 0f), 3f, radius * 0.1f, 8, roof);
                for (int k = 0; k < 6; k++)
                {
                    float a = k / 6f * Mathf.PI * 2f; Vector3 p = new Vector3(Mathf.Cos(a) * radius * 0.24f, 0.2f, Mathf.Sin(a) * radius * 0.2f);
                    float hgt = rng.Range(6f, 13f);
                    mb.Cylinder(p, hgt, radius * 0.035f, radius * 0.03f, 8, st, true);
                    mb.Cone(p + new Vector3(0f, hgt, 0f), hgt * 0.45f, radius * 0.05f, 8, roof);
                    gl.Box(p + new Vector3(0f, hgt * 0.6f, radius * 0.03f), new Vector3(0.25f, 0.6f, 0.1f), Hx(0xffd58a));
                }
                mb.Cone(new Vector3(0f, 9f, 0f), 10f, radius * 0.06f, 8, Hx(0xd9b84a));
            }
            GameObject go = new GameObject("FloatingIsland");
            go.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("SL_Island");
            go.AddComponent<MeshRenderer>().sharedMaterial = SLMat.Vertex();
            if (gl.Count > 0)
            {
                GameObject g = new GameObject("Lights"); g.transform.SetParent(go.transform, false);
                g.AddComponent<MeshFilter>().sharedMesh = gl.ToMesh("SL_IslandGlow");
                g.AddComponent<MeshRenderer>().sharedMaterial = SLMat.Lit(Color.white, 0.3f, 0f, new Color(1f, 0.75f, 0.4f));
            }
            if (waterfall)
            {
                MeshBuilder wf = new MeshBuilder();
                float a = rng.Range(0f, 6.28f);
                Vector3 top = new Vector3(Mathf.Cos(a) * radius * 0.88f, 0.1f, Mathf.Sin(a) * radius * 0.82f);
                Vector3 side = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)) * 1.2f;
                float len = radius * 1.8f;
                int n = 8; Vector3[] l = new Vector3[n], r2 = new Vector3[n];
                for (int i = 0; i < n; i++) { float t = i / (float)(n - 1); Vector3 c = top + new Vector3(0f, -len * t, 0f) + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (0.5f + t * 1.5f); l[i] = c - side * (1f - t * 0.4f); r2[i] = c + side * (1f - t * 0.4f); }
                wf.Strip(l, r2, Hx(0xcfeeff));
                GameObject w = new GameObject("Waterfall"); w.transform.SetParent(go.transform, false);
                w.AddComponent<MeshFilter>().sharedMesh = wf.ToMesh("SL_Waterfall");
                w.AddComponent<MeshRenderer>().sharedMaterial = SLMat.Lit(Color.white, 0.8f, 0f, new Color(0.35f, 0.55f, 0.75f));
            }
            return go;
        }
    }

    /// <summary>The lobby set: glowing platform ring in front of a fortress sky.</summary>
    public static class LobbyStage
    {
        public static GameObject Build()
        {
            GameObject root = new GameObject("SL_LobbyStage");
            MeshBuilder mb = new MeshBuilder(), gl = new MeshBuilder();
            Color st = SLMath.Hex(0xb9b3a6), dark = SLMath.Hex(0x6c6861), gold = SLMath.Hex(0xd9b84a);
            mb.Cylinder(new Vector3(0f, -1.2f, 0f), 1.2f, 7.5f, 7.5f, 28, st, true);
            mb.Cylinder(new Vector3(0f, -0.05f, 0f), 0.1f, 6.8f, 6.8f, 28, SLMath.Hex(0xa9a396), true);
            mb.Cylinder(new Vector3(0f, -9f, 0f), 7.8f, 6f, 1.5f, 14, SLMath.Hex(0x6b645c), true);
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f;
                mb.Box(new Vector3(Mathf.Cos(a) * 7.3f, 0.3f, Mathf.Sin(a) * 7.3f), new Vector3(0.7f, 0.9f, 0.7f), dark);
                gl.Ellipsoid(new Vector3(Mathf.Cos(a) * 7.3f, 0.95f, Mathf.Sin(a) * 7.3f), new Vector3(0.14f, 0.14f, 0.14f), SLMath.Hex(0x7fd0ff), 6, 3);
            }
            gl.Cylinder(new Vector3(0f, 0.0f, 0f), 0.03f, 4.4f, 4.4f, 40, gold, true);
            gl.Cylinder(new Vector3(0f, 0.0f, 0f), 0.04f, 3.9f, 3.9f, 40, SLMath.Hex(0x2a3a6a), true);
            gl.Cylinder(new Vector3(0f, 0.01f, 0f), 0.05f, 3.3f, 3.3f, 40, gold, true);
            gl.Cylinder(new Vector3(0f, 0.02f, 0f), 0.05f, 3.15f, 3.15f, 40, SLMath.Hex(0x2a3a6a), true);
            GameObject s = new GameObject("Platform"); s.transform.SetParent(root.transform, false);
            s.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("SL_Platform");
            s.AddComponent<MeshRenderer>().sharedMaterial = SLMat.Vertex();
            GameObject g = new GameObject("PlatformGlow"); g.transform.SetParent(root.transform, false);
            g.AddComponent<MeshFilter>().sharedMesh = gl.ToMesh("SL_PlatformGlow");
            g.AddComponent<MeshRenderer>().sharedMaterial = SLMat.Lit(Color.white, 0.5f, 0.2f, new Color(0.45f, 0.65f, 1f) * 1.2f);
            GameObject castle = FloatingIsland.Build(70f, true, 7, true);
            castle.transform.SetParent(root.transform, false); castle.transform.localPosition = new Vector3(40f, -18f, 120f); castle.transform.localScale = Vector3.one * 1.5f;
            castle.AddComponent<Bob>().Init(0.5f, 0.6f);
            return root;
        }
    }
}
