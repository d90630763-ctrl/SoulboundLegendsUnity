using System;
using UnityEngine;

namespace SL.Core
{
    public static class SLLog
    {
        public static void I(string m) { Debug.Log("[SL] " + m); }
        public static void W(string m) { Debug.LogWarning("[SL] " + m); }
        public static void E(string m) { Debug.LogError("[SL] " + m); }
    }

    /// <summary>Deterministic math helpers. Server and clients must produce identical results.</summary>
    public static class SLMath
    {
        public static float Hash(int x, int z, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + z * 668265263 + seed * 362437);
                h = (h ^ (h >> 13)) * 1274126177u;
                h = h ^ (h >> 16);
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }

        public static float Smooth(float a, float b, float x)
        {
            if (b == a) return x < a ? 0f : 1f;
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        public static float Noise(float x, float z, int seed)
        {
            int xi = Mathf.FloorToInt(x), zi = Mathf.FloorToInt(z);
            float fx = x - xi, fz = z - zi;
            float u = fx * fx * (3f - 2f * fx), v = fz * fz * (3f - 2f * fz);
            float a = Hash(xi, zi, seed), b = Hash(xi + 1, zi, seed);
            float c = Hash(xi, zi + 1, seed), d = Hash(xi + 1, zi + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        /// <summary>Fractal noise in roughly -0.5..0.5.</summary>
        public static float Fbm(float x, float z, int seed, int oct)
        {
            float sum = 0f, amp = 0.5f, f = 1f, norm = 0f;
            for (int i = 0; i < oct; i++)
            {
                sum += (Noise(x * f, z * f, seed + i * 31) - 0.5f) * amp;
                norm += amp; amp *= 0.5f; f *= 2f;
            }
            return sum / norm;
        }

        public static Color Jitter(Color c, float amt, float n)
        {
            float k = 1f + (n - 0.5f) * 2f * amt;
            return new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a);
        }

        public static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
        }
    }

    public sealed class DetRng
    {
        uint s;
        public DetRng(int seed) { s = (uint)(seed * 2654435761u + 1013904223u); if (s == 0) s = 1; }
        public uint NextU() { s ^= s << 13; s ^= s >> 17; s ^= s << 5; return s; }
        public float Next() { return (NextU() & 0xFFFFFF) / (float)0x1000000; }
        public float Range(float a, float b) { return a + (b - a) * Next(); }
        public int Int(int a, int bExclusive) { return a + (int)(Next() * (bExclusive - a)) % Math.Max(1, bExclusive - a); }
    }

    public static class SLSave
    {
        public static int GetInt(string k, int d) { return PlayerPrefs.GetInt("SL_" + k, d); }
        public static void SetInt(string k, int v) { PlayerPrefs.SetInt("SL_" + k, v); }
        public static string GetStr(string k, string d) { return PlayerPrefs.GetString("SL_" + k, d); }
        public static void SetStr(string k, string v) { PlayerPrefs.SetString("SL_" + k, v); }
        public static void Flush() { PlayerPrefs.Save(); }
    }
}
