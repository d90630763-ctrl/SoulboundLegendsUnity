using UnityEngine;
using SL.Core;

namespace SL.Actors
{
    public enum WeaponType { None, Sword, Greatsword, Staff, Bow, Spear }

    public static class WeaponKit
    {
        static Color H(int c) { return SLMath.Hex(c); }

        /// <summary>Procedural weapon. Built along +Y with the grip at the origin.</summary>
        public static GameObject Build(WeaponType wt, int rarity, Color glow)
        {
            Color[] steel = { H(0x9aa0aa), H(0xb8c0cc), H(0xd0d8e8), H(0xc8b8ff), H(0xffd870), H(0xff9090) };
            Color blade = steel[Mathf.Clamp(rarity, 0, 5)];
            Color wood = H(0x6b4a2e), gold = H(0xd9b84a);
            MeshBuilder mb = new MeshBuilder(), mg = new MeshBuilder();
            switch (wt)
            {
                case WeaponType.Sword:
                case WeaponType.Greatsword:
                    {
                        float L = wt == WeaponType.Greatsword ? 1.45f : 0.95f, W = wt == WeaponType.Greatsword ? 0.13f : 0.065f;
                        mb.Cylinder(new Vector3(0, -0.12f, 0), 0.24f, 0.022f, 0.022f, 6, wood, true);
                        mb.Ellipsoid(new Vector3(0, -0.14f, 0), new Vector3(0.035f, 0.035f, 0.035f), gold, 6, 4);
                        mb.Box(new Vector3(0, 0.125f, 0), new Vector3(W * 2.6f, 0.04f, 0.05f), gold);
                        mb.Loft(new[] { new Vector3(0, 0.14f, 0), new Vector3(0, 0.14f + L * 0.85f, 0), new Vector3(0, 0.14f + L, 0) }, new[] { W * 0.5f, W * 0.45f, 0.004f }, new[] { 0.012f, 0.01f, 0.004f }, 4, blade, true, false);
                        mg.Box(new Vector3(0, 0.14f + L * 0.45f, 0.012f), new Vector3(0.012f, L * 0.7f, 0.006f), glow);
                        break;
                    }
                case WeaponType.Spear:
                    {
                        mb.Cylinder(new Vector3(0, -0.8f, 0), 2.1f, 0.022f, 0.02f, 6, wood, true);
                        mb.Loft(new[] { new Vector3(0, 1.3f, 0), new Vector3(0, 1.5f, 0), new Vector3(0, 1.72f, 0) }, new[] { 0.01f, 0.05f, 0.004f }, new[] { 0.01f, 0.012f, 0.004f }, 4, blade, true, false);
                        mg.Ellipsoid(new Vector3(0, 1.3f, 0), new Vector3(0.03f, 0.03f, 0.03f), glow, 6, 4);
                        break;
                    }
                case WeaponType.Staff:
                    {
                        mb.Cylinder(new Vector3(0, -0.7f, 0), 1.9f, 0.026f, 0.02f, 6, wood, true);
                        for (int i = 0; i < 4; i++) { mb.Push(new Vector3(0, 1.15f, 0), Quaternion.Euler(0, i * 90f, 0), Vector3.one); mb.Push(new Vector3(0.05f, 0, 0), Quaternion.Euler(0, 0, -18f), Vector3.one); mb.Cone(Vector3.zero, 0.28f, 0.018f, 5, gold); mb.Pop(); mb.Pop(); }
                        mg.Ellipsoid(new Vector3(0, 1.3f, 0), new Vector3(0.075f, 0.09f, 0.075f), glow, 8, 5);
                        break;
                    }
                case WeaponType.Bow:
                    {
                        Vector3[] c = new Vector3[9]; float[] r = new float[9];
                        for (int i = 0; i < 9; i++) { float a = (i / 8f - 0.5f) * 2.4f; c[i] = new Vector3(0, Mathf.Sin(a) * 0.55f, -Mathf.Cos(a) * 0.14f + 0.08f); r[i] = 0.018f; }
                        mb.Loft(c, r, r, 5, wood, true, true);
                        mb.Box(new Vector3(0, 0, 0.0f), new Vector3(0.045f, 0.14f, 0.045f), gold);
                        mg.Box(new Vector3(0, 0, -0.06f), new Vector3(0.004f, 1.05f, 0.004f), glow);
                        break;
                    }
            }
            GameObject root = new GameObject("Weapon_" + wt);
            GameObject am = new GameObject("metal"); am.transform.SetParent(root.transform, false);
            am.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("W"); am.AddComponent<MeshRenderer>().sharedMaterial = SLMat.Lit(Color.white, 0.7f, 0.8f);
            GameObject g = new GameObject("glow"); g.transform.SetParent(root.transform, false);
            g.AddComponent<MeshFilter>().sharedMesh = mg.ToMesh("WG"); g.AddComponent<MeshRenderer>().sharedMaterial = SLMat.Lit(Color.white, 0.4f, 0f, glow * 1.6f);
            return root;
        }
    }
}
