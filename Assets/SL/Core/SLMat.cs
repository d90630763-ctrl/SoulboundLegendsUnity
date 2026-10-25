using System.Collections.Generic;
using UnityEngine;

namespace SL.Core
{
    /// <summary>Material factory. Uses only shaders shipped in Resources/SL/Shaders so nothing is stripped from builds.</summary>
    public static class SLMat
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static Shader lit, water, sky, glow;

        static Shader Load(string res, string find)
        {
            Shader s = Resources.Load<Shader>("SL/Shaders/" + res);
            if (s == null) s = Shader.Find(find);
            if (s == null) { SLLog.E("Shader missing: " + find); s = Shader.Find("Sprites/Default"); }
            return s;
        }

        static void Init()
        {
            if (lit != null) return;
            lit = Load("VertexColorLit", "SL/VertexColorLit");
            water = Load("Water", "SL/Water");
            sky = Load("Sky", "SL/Sky");
            glow = Load("ParticleGlow", "SL/ParticleGlow");
        }

        public static Material Lit(Color tint, float smooth, float metal, Color emission)
        {
            Init();
            string key = "L" + tint + smooth + metal + emission;
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(lit);
            m.SetColor("_Color", tint);
            m.SetFloat("_Glossiness", smooth);
            m.SetFloat("_Metallic", metal);
            m.SetColor("_Emission", emission);
            cache[key] = m;
            return m;
        }

        public static Material Lit(Color tint, float smooth, float metal) { return Lit(tint, smooth, metal, Color.black); }
        public static Material Lit(Color tint) { return Lit(tint, 0.2f, 0f, Color.black); }
        public static Material Vertex() { return Lit(Color.white, 0.12f, 0f, Color.black); }
        public static Material Glow(Color tint) { return Lit(Color.white, 0.4f, 0f, tint * 1.6f); }

        public static Material Water()
        {
            Init();
            Material m;
            if (cache.TryGetValue("W", out m) && m != null) return m;
            m = new Material(water);
            cache["W"] = m;
            return m;
        }

        public static Material Sky()
        {
            Init();
            Material m;
            if (cache.TryGetValue("S", out m) && m != null) return m;
            m = new Material(sky);
            cache["S"] = m;
            return m;
        }

        public static Material Particle(Color tint)
        {
            Init();
            string key = "P" + tint;
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(glow);
            m.SetColor("_Tint", tint);
            cache[key] = m;
            return m;
        }
    }
}
