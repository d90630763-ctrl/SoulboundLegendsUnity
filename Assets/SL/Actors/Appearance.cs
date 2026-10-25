using UnityEngine;
using SL.Core;

namespace SL.Actors
{
    public static class LookPalette
    {
        public static readonly int[] Skin = { 0xf6d6bf, 0xe0b08a, 0xc68b5f, 0x9a6038, 0x6b3f22, 0xfbe4d8 };
        public static readonly int[] Hair = { 0x15151c, 0xc9ccd6, 0x9a2430, 0xd9b36a, 0x4a2c1c, 0x4a3a8a };
        public static readonly int[] Outfit = { 0x1c2233, 0x6a1f2a, 0x1f4a35, 0xd8d2c0, 0x2a3f8a, 0x4a2a6a };
        public static readonly int[] Eye = { 0x3a8aff, 0x4fb070, 0xe0a820, 0xd03a3a, 0x9a4aff };
        public static readonly string[] BodyNames = { "Slim", "Average", "Broad" };
        public static readonly string[] HairNames = { "Spiky", "Long", "Ponytail", "Crest" };
        public static readonly string[] FaceNames = { "Calm", "Sharp", "Soft" };
        public static readonly string[] AccNames = { "None", "Scarf", "Earring", "Mask" };
        public static readonly string[] SkinNames = { "Porcelain", "Fair", "Tan", "Bronze", "Deep", "Pale" };
        public static readonly string[] HairColorNames = { "Black", "Silver", "Crimson", "Gold", "Brown", "Indigo" };
        public static readonly string[] OutfitNames = { "Midnight", "Crimson", "Forest", "Ivory", "Royal", "Violet" };
        public static readonly string[] EyeNames = { "Blue", "Green", "Amber", "Red", "Violet" };
    }

    public struct Look
    {
        public int body, skin, hairStyle, hairColor, outfit, face, eye, acc, voice;

        public uint Pack()
        {
            uint v = 0;
            v |= (uint)(body & 3);
            v |= (uint)(skin & 7) << 2;
            v |= (uint)(hairStyle & 3) << 5;
            v |= (uint)(hairColor & 7) << 7;
            v |= (uint)(outfit & 7) << 10;
            v |= (uint)(face & 3) << 13;
            v |= (uint)(eye & 7) << 15;
            v |= (uint)(acc & 3) << 18;
            v |= (uint)(voice & 3) << 20;
            return v;
        }

        public static Look Unpack(uint v)
        {
            Look l = new Look();
            l.body = (int)(v & 3); l.skin = (int)((v >> 2) & 7); l.hairStyle = (int)((v >> 5) & 3); l.hairColor = (int)((v >> 7) & 7);
            l.outfit = (int)((v >> 10) & 7); l.face = (int)((v >> 13) & 3); l.eye = (int)((v >> 15) & 7); l.acc = (int)((v >> 18) & 3); l.voice = (int)((v >> 20) & 3);
            l.skin = Mathf.Min(l.skin, 5); l.hairColor = Mathf.Min(l.hairColor, 5); l.outfit = Mathf.Min(l.outfit, 5); l.eye = Mathf.Min(l.eye, 4); l.body = Mathf.Min(l.body, 2); l.face = Mathf.Min(l.face, 2);
            return l;
        }

        public static Look Default() { Look l = new Look(); l.body = 1; l.skin = 1; l.hairStyle = 0; l.hairColor = 0; l.outfit = 0; l.face = 0; l.eye = 0; l.acc = 1; return l; }

        public static Look Random(int seed)
        {
            DetRng r = new DetRng(seed);
            Look l = new Look();
            l.body = r.Int(0, 3); l.skin = r.Int(0, 6); l.hairStyle = r.Int(0, 4); l.hairColor = r.Int(0, 6); l.outfit = r.Int(0, 6); l.face = r.Int(0, 3); l.eye = r.Int(0, 5); l.acc = r.Int(0, 4);
            return l;
        }
    }
}
