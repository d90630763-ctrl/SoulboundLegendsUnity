using System.Collections.Generic;
using UnityEngine;
using SL.World;

namespace SL.Gameplay
{
    /// <summary>Movement shared by the server (authoritative) and the owning client (prediction).</summary>
    public static class Mover
    {
        public const float WalkSpeed = 5.0f, SprintSpeed = 7.6f, BlockSpeed = 2.6f;

        public static Vector3 Step(ZoneLayout L, HeightField hf, HashSet<int> destroyed, Vector3 pos, Vector2 move, float speed, float dt)
        {
            if (hf.IsWater(pos.x, pos.z) && !L.OnBridge(pos.x, pos.z)) speed *= 0.7f;
            Vector2 old = new Vector2(pos.x, pos.z);
            Vector2 np = old + move * (speed * dt);
            np = L.Resolve(np, 0.45f, destroyed);
            float r = np.magnitude, max = L.Def.Radius * 0.95f;
            if (r > max) np *= max / r;
            if (hf.IsDeep(np.x, np.y) && !L.OnBridge(np.x, np.y))
            {
                if (!hf.IsDeep(np.x, old.y) || L.OnBridge(np.x, old.y)) np = new Vector2(np.x, old.y);
                else if (!hf.IsDeep(old.x, np.y) || L.OnBridge(old.x, np.y)) np = new Vector2(old.x, np.y);
                else np = old;
            }
            return new Vector3(np.x, L.Ground(hf, np.x, np.y), np.y);
        }

        public static Vector3 Forward(float yawDeg) { float a = yawDeg * Mathf.Deg2Rad; return new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)); }
    }
}
