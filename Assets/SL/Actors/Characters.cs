using UnityEngine;
using SL.Core;

namespace SL.Actors
{
    public enum Anim { Idle, Attack, Heavy, Cast, Dodge, Hit, Jump, Emote }

    /// <summary>Procedural skeleton animation for humanoids. Meshes are rigidly skinned to these bones.</summary>
    public sealed class CharacterRig : MonoBehaviour
    {
        public Transform[] B;
        public Transform HandR, HandL;
        public Renderer[] GlowRenderers;
        GameObject weaponR, weaponL;
        float phase, move, moveTarget, oneShot, oneShotLen, t; Anim cur = Anim.Idle; bool block, dead, sprint; float deadT; int emoteId;
        Vector3 hipsRest;
        public Color Glow = new Color(0.4f, 0.7f, 1f);

        public const int ROOT = 0, HIPS = 1, SPINE = 2, CHEST = 3, NECK = 4, HEAD = 5, SHL = 6, ELL = 7, HDL = 8, SHR = 9, ELR = 10, HDR = 11, HPL = 12, KNL = 13, FTL = 14, HPR = 15, KNR = 16, FTR = 17;

        void Start() { if (B != null && B.Length > HIPS) hipsRest = B[HIPS].localPosition; }

        public void SetMove(float speed01, bool isSprint) { moveTarget = Mathf.Clamp01(speed01); sprint = isSprint; }
        public void SetBlock(bool b) { block = b; }
        public void SetDead(bool d) { if (d && !dead) deadT = 0f; dead = d; }
        public void Play(Anim a, int id = 0)
        {
            cur = a; t = 0f; emoteId = id;
            switch (a)
            {
                case Anim.Attack: oneShotLen = 0.45f; break;
                case Anim.Heavy: oneShotLen = 0.85f; break;
                case Anim.Cast: oneShotLen = 0.65f; break;
                case Anim.Dodge: oneShotLen = 0.5f; break;
                case Anim.Hit: oneShotLen = 0.3f; break;
                case Anim.Jump: oneShotLen = 0.7f; break;
                case Anim.Emote: oneShotLen = 2.2f; break;
                default: oneShotLen = 0f; break;
            }
            oneShot = oneShotLen;
        }

        public void SetWeapon(WeaponType wt, int rarity, Color glow)
        {
            if (weaponR != null) Destroy(weaponR);
            if (weaponL != null) Destroy(weaponL);
            if (HandR == null || wt == WeaponType.None) return;
            weaponR = WeaponKit.Build(wt, rarity, glow);
            weaponR.transform.SetParent(HandR, false);
            if (wt == WeaponType.Bow)
            {
                weaponR.transform.SetParent(HandL, false);
                weaponR.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                weaponR.transform.localPosition = new Vector3(0f, -0.02f, 0.03f);
            }
            else if (wt == WeaponType.Staff || wt == WeaponType.Spear)
            { weaponR.transform.localRotation = Quaternion.Euler(8f, 0f, 0f); weaponR.transform.localPosition = new Vector3(0f, 0.02f, 0.04f); }
            else
            { weaponR.transform.localRotation = Quaternion.Euler(75f, 0f, 0f); weaponR.transform.localPosition = new Vector3(0f, -0.03f, 0.02f); }
        }

        public void SetGlow(Color c)
        {
            Glow = c;
            if (GlowRenderers == null) return;
            for (int i = 0; i < GlowRenderers.Length; i++)
                if (GlowRenderers[i] != null) GlowRenderers[i].sharedMaterial = SLMat.Lit(Color.white, 0.4f, 0f, c * 1.5f);
        }

        static Quaternion Rx(float a) { return Quaternion.Euler(a, 0f, 0f); }

        void LateUpdate()
        {
            if (B == null || B.Length < 18) return;
            float dt = Time.deltaTime; t += dt;
            move = Mathf.Lerp(move, moveTarget, 1f - Mathf.Exp(-10f * dt));
            phase += dt * move * (sprint ? 12f : 8f);
            if (oneShot > 0f) { oneShot -= dt; if (oneShot <= 0f) { oneShot = 0f; cur = Anim.Idle; } }
            float u = oneShotLen > 0f ? 1f - oneShot / oneShotLen : 0f;
            float w = move, sw = Mathf.Sin(phase) * 38f * w, br = Mathf.Sin(Time.time * 1.7f);

            for (int i = 1; i < 18; i++) B[i].localRotation = Quaternion.identity;
            B[HIPS].localPosition = hipsRest + new Vector3(0f, Mathf.Abs(Mathf.Cos(phase)) * 0.04f * w - 0.02f * w, 0f);
            B[SPINE].localRotation = Rx(br * 1.2f + w * (sprint ? 12f : 5f));
            B[CHEST].localRotation = Quaternion.Euler(br * 0.8f, Mathf.Sin(phase) * -6f * w, 0f);
            B[HEAD].localRotation = Quaternion.Euler(-br * 1.5f - w * 4f, Mathf.Sin(Time.time * 0.6f) * 5f * (1f - w), 0f);
            B[HPL].localRotation = Rx(-sw); B[HPR].localRotation = Rx(sw);
            B[KNL].localRotation = Rx(Mathf.Max(0f, Mathf.Sin(phase + 1.2f)) * 55f * w + 4f);
            B[KNR].localRotation = Rx(Mathf.Max(0f, Mathf.Sin(phase + 1.2f + Mathf.PI)) * 55f * w + 4f);
            B[SHL].localRotation = Quaternion.Euler(sw * 0.9f, 0f, 4f); B[SHR].localRotation = Quaternion.Euler(-sw * 0.9f, 0f, -4f);
            B[ELL].localRotation = Rx(-(8f + 30f * w)); B[ELR].localRotation = Rx(-(8f + 30f * w));
            B[SHR].localRotation = B[SHR].localRotation * Rx(-18f);

            if (block && !dead)
            {
                B[SHL].localRotation = Quaternion.Euler(-70f, 25f, 10f); B[SHR].localRotation = Quaternion.Euler(-70f, -25f, -10f);
                B[ELL].localRotation = Rx(-100f); B[ELR].localRotation = Rx(-100f); B[SPINE].localRotation = Rx(8f);
            }

            switch (cur)
            {
                case Anim.Attack:
                    {
                        float a = u < 0.35f ? Mathf.Lerp(0f, -150f, Ease(u / 0.35f)) : Mathf.Lerp(-150f, -15f, Ease((u - 0.35f) / 0.35f));
                        B[SHR].localRotation = Rx(a); B[ELR].localRotation = Rx(u < 0.35f ? -50f : -10f);
                        B[CHEST].localRotation = Quaternion.Euler(0f, Mathf.Lerp(-25f, 30f, Ease(u)), 0f);
                        B[HPL].localRotation = Rx(-22f); B[HPR].localRotation = Rx(18f); B[KNL].localRotation = Rx(20f);
                        break;
                    }
                case Anim.Heavy:
                    {
                        float a = u < 0.6f ? Mathf.Lerp(0f, -175f, Ease(u / 0.6f)) : Mathf.Lerp(-175f, 10f, Ease((u - 0.6f) / 0.25f));
                        B[SHR].localRotation = Rx(a); B[SHL].localRotation = Rx(a * 0.8f); B[ELR].localRotation = Rx(-30f); B[ELL].localRotation = Rx(-30f);
                        B[SPINE].localRotation = Rx(u < 0.6f ? Mathf.Lerp(0f, -12f, u / 0.6f) : 28f);
                        B[HPL].localRotation = Rx(-30f); B[KNR].localRotation = Rx(30f);
                        break;
                    }
                case Anim.Cast:
                    {
                        float k = Mathf.Sin(u * Mathf.PI);
                        B[SHL].localRotation = Rx(-85f * k - 10f); B[SHR].localRotation = Rx(-95f * k - 10f);
                        B[ELL].localRotation = Rx(-20f); B[ELR].localRotation = Rx(-20f); B[SPINE].localRotation = Rx(-8f * k);
                        break;
                    }
                case Anim.Dodge:
                    {
                        B[HIPS].localRotation = Rx(360f * Ease(u));
                        B[HIPS].localPosition = hipsRest + new Vector3(0f, -0.38f * Mathf.Sin(u * Mathf.PI), 0f);
                        B[HPL].localRotation = Rx(-80f); B[HPR].localRotation = Rx(-80f); B[KNL].localRotation = Rx(90f); B[KNR].localRotation = Rx(90f);
                        B[SHL].localRotation = Rx(-60f); B[SHR].localRotation = Rx(-60f);
                        break;
                    }
                case Anim.Hit:
                    B[SPINE].localRotation = Rx(-22f * (1f - u)); B[HEAD].localRotation = Rx(-18f * (1f - u)); break;
                case Anim.Jump:
                    B[HPL].localRotation = Rx(-50f); B[HPR].localRotation = Rx(-30f); B[KNL].localRotation = Rx(70f); B[KNR].localRotation = Rx(50f);
                    B[SHL].localRotation = Quaternion.Euler(-30f, 0f, 20f); B[SHR].localRotation = Quaternion.Euler(-30f, 0f, -20f); break;
                case Anim.Emote:
                    {
                        if (emoteId == 0)
                        { B[SHR].localRotation = Quaternion.Euler(-10f, 0f, -150f); B[ELR].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 12f) * 30f); }
                        else if (emoteId == 1)
                        { B[SHR].localRotation = Quaternion.Euler(0f, 0f, -150f); B[SHL].localRotation = Quaternion.Euler(0f, 0f, 150f); B[HIPS].localPosition = hipsRest + new Vector3(0f, Mathf.Abs(Mathf.Sin(t * 8f)) * 0.15f, 0f); }
                        else
                        { B[SPINE].localRotation = Rx(25f); B[HEAD].localRotation = Rx(15f); B[SHL].localRotation = Rx(-20f); B[SHR].localRotation = Rx(-20f); }
                        break;
                    }
            }

            if (dead)
            {
                deadT = Mathf.Min(1f, deadT + dt * 2f);
                B[HIPS].localRotation = Rx(-90f * Ease(deadT));
                B[HIPS].localPosition = hipsRest + new Vector3(0f, -0.82f * Ease(deadT), 0f);
                B[SHL].localRotation = Quaternion.Euler(0f, 0f, 40f); B[SHR].localRotation = Quaternion.Euler(0f, 0f, -40f);
            }
        }

        static float Ease(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
    }

    public static class CharacterBuilder
    {
        static Color H(int c) { return SLMath.Hex(c); }

        static readonly int[] Parent = { -1, 0, 1, 2, 3, 4, 3, 6, 7, 3, 9, 10, 1, 12, 13, 1, 15, 16 };
        static readonly string[] Names = { "root", "hips", "spine", "chest", "neck", "head", "shL", "elL", "hdL", "shR", "elR", "hdR", "hpL", "knL", "ftL", "hpR", "knR", "ftR" };

        static Vector3[] LocalPos(float w)
        {
            return new[]
            {
                Vector3.zero, new Vector3(0f, 0.98f, 0f), new Vector3(0f, 0.12f, 0f), new Vector3(0f, 0.22f, 0f), new Vector3(0f, 0.26f, 0f), new Vector3(0f, 0.10f, 0f),
                new Vector3(-0.2f * w, 0.17f, 0f), new Vector3(0f, -0.29f, 0f), new Vector3(0f, -0.27f, 0f),
                new Vector3(0.2f * w, 0.17f, 0f), new Vector3(0f, -0.29f, 0f), new Vector3(0f, -0.27f, 0f),
                new Vector3(-0.1f * w, -0.04f, 0f), new Vector3(0f, -0.45f, 0f), new Vector3(0f, -0.45f, 0f),
                new Vector3(0.1f * w, -0.04f, 0f), new Vector3(0f, -0.45f, 0f), new Vector3(0f, -0.45f, 0f)
            };
        }

        static void Skin(GameObject root, Transform[] bones, MeshBuilder mb, Material mat, string name)
        {
            mb.Skinned = true;
            GameObject go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            SkinnedMeshRenderer r = go.AddComponent<SkinnedMeshRenderer>();
            Mesh m = mb.ToMesh(name);
            Matrix4x4[] bind = new Matrix4x4[bones.Length];
            for (int i = 0; i < bones.Length; i++) bind[i] = bones[i].worldToLocalMatrix * root.transform.localToWorldMatrix;
            m.bindposes = bind;
            r.bones = bones; r.rootBone = bones[1]; r.sharedMesh = m; r.sharedMaterial = mat;
            r.updateWhenOffscreen = true;
        }

        public static GameObject Build(Look look, int element, string name)
        {
            return Build(look, element, name, false, 1f);
        }

        /// <summary>Build a detailed skinned humanoid. 'god' makes a larger radiant silhouette used by the opening cinematic.</summary>
        public static GameObject Build(Look look, int element, string name, bool god, float scale)
        {
            GameObject root = new GameObject("Char_" + name);
            float w = look.body == 0 ? 0.92f : (look.body == 2 ? 1.14f : 1f);
            Vector3[] lp = LocalPos(w);
            Transform[] b = new Transform[18];
            Vector3[] rest = new Vector3[18];
            for (int i = 0; i < 18; i++)
            {
                GameObject g = new GameObject(Names[i]);
                b[i] = g.transform;
                b[i].SetParent(i == 0 ? root.transform : b[Parent[i]], false);
                b[i].localPosition = i == 0 ? Vector3.zero : lp[i];
                rest[i] = i == 0 ? Vector3.zero : rest[Parent[i]] + lp[i];
            }
            Color skin = H(LookPalette.Skin[Mathf.Clamp(look.skin, 0, 5)]);
            Color hair = H(LookPalette.Hair[Mathf.Clamp(look.hairColor, 0, 5)]);
            Color coat = H(LookPalette.Outfit[Mathf.Clamp(look.outfit, 0, 5)]);
            Color eye = H(LookPalette.Eye[Mathf.Clamp(look.eye, 0, 4)]);
            Color glow = element >= 0 ? ElementColor(element) : new Color(0.3f, 0.6f, 1f);
            Color metal = H(0x5b616e), dark = H(0x1a1c24);
            if (god) { coat = glow * 0.35f; coat.a = 1f; skin = Color.Lerp(skin, glow, 0.4f); metal = Color.Lerp(H(0xd8c070), glow, 0.35f); }
            if (element == 10) { coat = H(0x0a0a10); skin = H(0x07070b); hair = H(0x0a0a10); glow = H(0x9a4aff); metal = H(0x14141c); }

            MeshBuilder mSkin = new MeshBuilder(), mHair = new MeshBuilder(), mCoat = new MeshBuilder(), mArm = new MeshBuilder(), mEye = new MeshBuilder(), mGlow = new MeshBuilder(), mDark = new MeshBuilder();
            Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }
            Vector3 R(int bone, float x, float y, float z) { return rest[bone] + new Vector3(x, y, z); }

            // ---------- head / face ----------
            mSkin.Bone = 5;
            float hx = look.face == 1 ? 0.098f : (look.face == 2 ? 0.112f : 0.105f);
            Vector3 hc = V(0f, 1.77f, 0.005f);
            mSkin.Ellipsoid(hc, V(hx, 0.128f, 0.115f), skin, 12, 8);
            if (look.face == 1) mSkin.Cone(V(0f, 1.69f, 0.045f) + V(0f, -0.04f, 0f), 0.06f, 0.05f, 6, skin);
            mSkin.Ellipsoid(V(0f, 1.69f, 0.06f), V(hx * 0.8f, 0.07f, 0.075f), skin, 10, 5);
            mSkin.Push(V(0f, 1.745f, 0.115f), Quaternion.Euler(80f, 0f, 0f), Vector3.one); mSkin.Cone(V(0f, -0.01f, 0f), 0.05f, 0.015f, 5, skin); mSkin.Pop();
            mSkin.Ellipsoid(V(-hx, 1.76f, 0f), V(0.018f, 0.04f, 0.03f), skin, 6, 4); mSkin.Ellipsoid(V(hx, 1.76f, 0f), V(0.018f, 0.04f, 0.03f), skin, 6, 4);
            mSkin.Bone = 4;
            mSkin.Loft(new[] { R(4, 0, -0.05f, 0), R(4, 0, 0.05f, 0.005f) }, new[] { 0.052f, 0.048f }, new[] { 0.055f, 0.05f }, 8, skin, false, false);
            mEye.Bone = 5;
            for (int s = -1; s <= 1; s += 2)
            {
                mEye.Ellipsoid(V(s * 0.043f, 1.775f, 0.108f), V(0.023f, 0.028f, 0.012f), eye, 8, 4);
                mDark.Bone = 5; mDark.Box(V(s * 0.043f, 1.815f + (look.face == 1 ? 0.004f : 0f), 0.11f), V(0.05f, 0.008f, 0.01f), hair);
            }
            mDark.Bone = 5; mDark.Box(V(0f, 1.69f, 0.112f), V(0.04f, 0.006f, 0.008f), H(0x7a3030));

            // ---------- hair ----------
            mHair.Bone = 5;
            mHair.Ellipsoid(V(0f, 1.805f, -0.018f), V(hx + 0.012f, 0.112f, 0.122f), hair, 12, 7);
            for (int s = -1; s <= 1; s += 2) mHair.Ellipsoid(V(s * (hx + 0.002f), 1.76f, -0.03f), V(0.016f, 0.05f, 0.05f), hair, 6, 3);
            if (look.hairStyle == 0)
            {
                for (int i = 0; i < 9; i++)
                {
                    float a = i / 9f * Mathf.PI * 2f; float r = i < 6 ? 0.07f : 0.03f;
                    mHair.Push(V(Mathf.Cos(a) * r, 1.88f, Mathf.Sin(a) * r * 1.1f - 0.02f), Quaternion.Euler(Mathf.Sin(a) * -35f - 10f, 0f, -Mathf.Cos(a) * 35f), Vector3.one);
                    mHair.Cone(Vector3.zero, 0.11f + (i % 3) * 0.025f, 0.034f, 5, hair); mHair.Pop();
                }
            }
            else if (look.hairStyle == 1)
            {
                mHair.Loft(new[] { V(0f, 1.80f, -0.07f), V(0f, 1.6f, -0.1f), V(0f, 1.42f, -0.1f) }, new[] { 0.115f, 0.1f, 0.075f }, new[] { 0.055f, 0.05f, 0.04f }, 10, hair, false, true);
                mHair.Bone = 3; mHair.Loft(new[] { V(0f, 1.42f, -0.1f), V(0f, 1.22f, -0.11f) }, new[] { 0.075f, 0.04f }, new[] { 0.04f, 0.025f }, 8, hair, false, true);
            }
            else if (look.hairStyle == 2)
            {
                mHair.Ellipsoid(V(0f, 1.82f, -0.13f), V(0.04f, 0.04f, 0.04f), H(0xd9b84a), 6, 4);
                mHair.Loft(new[] { V(0f, 1.82f, -0.14f), V(0f, 1.68f, -0.22f), V(0f, 1.5f, -0.24f), V(0f, 1.36f, -0.22f) }, new[] { 0.035f, 0.05f, 0.045f, 0.012f }, new[] { 0.035f, 0.05f, 0.045f, 0.012f }, 7, hair, false, true);
            }
            else
            {
                for (int i = 0; i < 6; i++)
                {
                    mHair.Push(V(0f, 1.9f - i * 0.002f, 0.06f - i * 0.04f), Quaternion.Euler(-20f - i * 4f, 0f, 0f), Vector3.one);
                    mHair.Cone(Vector3.zero, 0.15f - i * 0.012f, 0.04f, 5, hair); mHair.Pop();
                }
            }

            // ---------- torso & coat ----------
            mCoat.Bone = 1;
            mCoat.Loft(new[] { R(1, 0, -0.06f, 0), R(1, 0, 0.1f, 0) }, new[] { 0.155f * w, 0.150f * w }, new[] { 0.10f, 0.095f }, 12, dark, false, false);
            mCoat.Bone = 2;
            mCoat.Loft(new[] { R(1, 0, 0.1f, 0), R(3, 0, -0.05f, 0), R(3, 0, 0.12f, 0.005f) }, new[] { 0.15f * w, 0.165f * w, 0.19f * w }, new[] { 0.095f, 0.105f, 0.11f }, 12, coat, false, false);
            mCoat.Bone = 3;
            mCoat.Loft(new[] { R(3, 0, 0.12f, 0.005f), R(3, 0, 0.22f, 0f) }, new[] { 0.19f * w, 0.17f * w }, new[] { 0.11f, 0.085f }, 12, coat, false, false);
            // coat tails
            mCoat.Bone = 1;
            for (int s = -1; s <= 1; s += 2)
            {
                mCoat.Strip(new[] { R(1, s * 0.05f * w, 0.0f, -0.09f), R(1, s * 0.1f * w, -0.2f, -0.13f), R(1, s * 0.12f * w, -0.42f, -0.16f) },
                            new[] { R(1, s * 0.19f * w, 0.0f, -0.04f), R(1, s * 0.24f * w, -0.2f, -0.08f), R(1, s * 0.27f * w, -0.44f, -0.1f) }, coat);
            }
            mGlow.Bone = 3;
            mGlow.Ellipsoid(R(3, 0f, 0.02f, 0.115f), V(0.026f, 0.034f, 0.014f), glow, 8, 4);
            mGlow.Bone = 1;
            mGlow.Strip(new[] { R(1, -0.215f * w, 0f, -0.03f), R(1, -0.26f * w, -0.2f, -0.08f), R(1, -0.29f * w, -0.44f, -0.1f) }, new[] { R(1, -0.2f * w, 0f, -0.03f), R(1, -0.245f * w, -0.2f, -0.08f), R(1, -0.275f * w, -0.44f, -0.1f) }, glow);
            mGlow.Strip(new[] { R(1, 0.2f * w, 0f, -0.03f), R(1, 0.245f * w, -0.2f, -0.08f), R(1, 0.275f * w, -0.44f, -0.1f) }, new[] { R(1, 0.215f * w, 0f, -0.03f), R(1, 0.26f * w, -0.2f, -0.08f), R(1, 0.29f * w, -0.44f, -0.1f) }, glow);

            // ---------- arms ----------
            for (int s = -1; s <= 1; s += 2)
            {
                int sh = s < 0 ? 6 : 9, el = s < 0 ? 7 : 10, hd = s < 0 ? 8 : 11;
                mCoat.Bone = sh;
                mCoat.Loft(new[] { R(sh, 0, 0.07f, 0), R(sh, 0, -0.1f, 0), R(el, 0, 0f, 0) }, new[] { 0.062f, 0.058f, 0.05f }, new[] { 0.062f, 0.058f, 0.05f }, 8, coat, false, false);
                mCoat.Ellipsoid(R(sh, 0, 0.02f, 0), V(0.07f, 0.07f, 0.07f), coat, 8, 4);
                mCoat.Bone = el;
                mCoat.Loft(new[] { R(el, 0, 0f, 0), R(el, 0, -0.14f, 0), R(hd, 0, 0.02f, 0) }, new[] { 0.05f, 0.047f, 0.04f }, new[] { 0.05f, 0.047f, 0.04f }, 8, coat, false, false);
                mArm.Bone = el;
                mArm.Loft(new[] { R(el, 0, -0.08f, 0), R(hd, 0, 0.02f, 0) }, new[] { 0.056f, 0.05f }, new[] { 0.056f, 0.05f }, 8, metal, true, true);
                mSkin.Bone = hd;
                mSkin.Ellipsoid(R(hd, 0, -0.05f, 0), V(0.035f, 0.055f, 0.03f), skin, 8, 4);
                mDark.Bone = hd;
                mDark.Ellipsoid(R(hd, 0, -0.045f, 0), V(0.04f, 0.045f, 0.034f), dark, 8, 4);
            }
            // ---------- armour ----------
            mArm.Bone = 6;
            mArm.Ellipsoid(R(6, -0.04f * w, 0.05f, 0), V(0.125f, 0.08f, 0.115f), metal, 10, 5);
            for (int i = 0; i < 3; i++) { mArm.Push(R(6, -0.1f * w - i * 0.01f, 0.09f + i * 0.02f, -0.02f + i * 0.04f), Quaternion.Euler(0f, 0f, 55f + i * 8f), Vector3.one); mArm.Cone(Vector3.zero, 0.12f, 0.03f, 5, metal); mArm.Pop(); }
            mArm.Bone = 9; mArm.Ellipsoid(R(9, 0.03f * w, 0.05f, 0), V(0.095f, 0.065f, 0.095f), metal, 10, 5);
            mArm.Bone = 3; mArm.Ellipsoid(R(3, 0f, 0.06f, 0.07f), V(0.12f * w, 0.1f, 0.06f), metal, 10, 5);
            mArm.Bone = 1; mArm.Cylinder(R(1, 0, -0.03f, 0), 0.07f, 0.162f * w, 0.162f * w, 12, H(0x3a2f26), false);
            mGlow.Bone = 1; mGlow.Box(R(1, 0, 0.0f, 0.108f), V(0.05f, 0.05f, 0.02f), glow);

            // ---------- legs ----------
            for (int s = -1; s <= 1; s += 2)
            {
                int hp = s < 0 ? 12 : 15, kn = s < 0 ? 13 : 16, ft = s < 0 ? 14 : 17;
                mDark.Bone = hp;
                mDark.Loft(new[] { R(hp, 0, 0.03f, 0), R(hp, 0, -0.22f, 0), R(kn, 0, 0f, 0) }, new[] { 0.095f * w, 0.082f, 0.068f }, new[] { 0.095f * w, 0.085f, 0.07f }, 9, dark, false, false);
                mDark.Bone = kn;
                mDark.Loft(new[] { R(kn, 0, 0f, 0), R(kn, 0, -0.2f, 0), R(ft, 0, 0.1f, 0) }, new[] { 0.068f, 0.06f, 0.048f }, new[] { 0.07f, 0.062f, 0.05f }, 9, dark, false, false);
                mArm.Bone = kn;
                mArm.Loft(new[] { R(kn, 0, -0.04f, 0), R(kn, 0, -0.22f, 0), R(ft, 0, 0.08f, 0) }, new[] { 0.075f, 0.066f, 0.055f }, new[] { 0.078f, 0.068f, 0.057f }, 9, H(0x20232c), false, true);
                mArm.Ellipsoid(R(kn, 0, 0.02f, 0.05f), V(0.07f, 0.08f, 0.045f), metal, 8, 4);
                mArm.Bone = ft;
                mArm.Box(R(ft, 0, 0.02f, 0.05f), V(0.1f, 0.07f, 0.24f), H(0x20232c));
                mArm.Box(R(ft, 0, 0.0f, 0.15f), V(0.09f, 0.04f, 0.05f), metal);
            }

            // ---------- accessories ----------
            if (look.acc == 1)
            {
                mGlow.Bone = 4;
                mGlow.Loft(new[] { R(4, 0, -0.03f, 0), R(4, 0, 0.03f, 0.0f) }, new[] { 0.07f, 0.065f }, new[] { 0.075f, 0.07f }, 10, glow, false, false);
                mGlow.Bone = 3; mGlow.Strip(new[] { R(3, 0.05f, 0.15f, 0.1f), R(3, 0.07f, -0.1f, 0.12f), R(3, 0.07f, -0.4f, 0.1f) }, new[] { R(3, 0.1f, 0.15f, 0.1f), R(3, 0.12f, -0.1f, 0.12f), R(3, 0.12f, -0.42f, 0.1f) }, glow);
            }
            else if (look.acc == 2) { mGlow.Bone = 5; mGlow.Ellipsoid(V(hx + 0.01f, 1.73f, 0f), V(0.012f, 0.02f, 0.012f), glow, 6, 3); }
            else if (look.acc == 3) { mDark.Bone = 5; mDark.Box(V(0f, 1.71f, 0.1f), V(0.17f, 0.075f, 0.06f), H(0x15151c)); mGlow.Bone = 5; mGlow.Box(V(0f, 1.72f, 0.132f), V(0.1f, 0.012f, 0.01f), glow); }

            // ---------- materials & assembly ----------
            Skin(root, b, mSkin, SLMat.Lit(Color.white, 0.25f, 0f), "skin");
            Skin(root, b, mHair, SLMat.Lit(Color.white, 0.4f, 0f), "hair");
            Skin(root, b, mCoat, SLMat.Lit(Color.white, 0.3f, 0f), "coat");
            Skin(root, b, mDark, SLMat.Lit(Color.white, 0.2f, 0f), "dark");
            Skin(root, b, mArm, SLMat.Lit(Color.white, 0.6f, 0.75f), "armor");
            Skin(root, b, mEye, SLMat.Lit(Color.white, 0.8f, 0f, eye * 0.6f), "eyes");
            Skin(root, b, mGlow, SLMat.Lit(Color.white, 0.4f, 0f, glow * 1.5f), "glow");

            CharacterRig rig = root.AddComponent<CharacterRig>();
            rig.B = b; rig.HandR = b[CharacterRig.HDR]; rig.HandL = b[CharacterRig.HDL];
            SkinnedMeshRenderer[] smr = root.GetComponentsInChildren<SkinnedMeshRenderer>();
            for (int i = 0; i < smr.Length; i++) if (smr[i].gameObject.name == "glow") rig.GlowRenderers = new Renderer[] { smr[i] };
            rig.Glow = glow;
            root.transform.localScale = Vector3.one * scale;
            return root;
        }

        public static Color ElementColor(int e)
        {
            if (e == 10) return H(0x9a4aff);
            return ElementInfoColor(e);
        }

        static Color ElementInfoColor(int e)
        {
            int[] hex = { 0x3aa0ff, 0xff5a1f, 0x9a7b4f, 0x8a3cff, 0x6aa8ff, 0x4fd04f, 0xfff0a0, 0xa0f0ff, 0xbfe8ff, 0xffffff };
            return H(hex[Mathf.Clamp(e, 0, 9)]);
        }
    }
}
