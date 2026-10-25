using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SL.Core;
using SL.Actors;
using SL.Gameplay;
using SL.World;

namespace SL.UI
{
    /// <summary>
    /// One-time in-engine opening cinematic: the gods, the demon war, the fall, the mystery of Shadow, and the title.
    /// The Shadow God is never shown as a participant in the war. A real video can replace this later without changing the game.
    /// </summary>
    public sealed class IntroCinematic : MonoBehaviour
    {
        Camera cam; Action done; float t; int phase = -1; bool finished;
        readonly List<GameObject> gods = new List<GameObject>(), demons = new List<GameObject>(), extras = new List<GameObject>();
        GameObject world; SkyEnvironment sky; RectTransform ui; Text sub, title; CanvasGroup fade;
        Vector3 camFrom, camTo, lookAt; readonly float[] phaseEnd = { 10f, 24f, 34f, 42f, 50f };
        static readonly string[] Lines =
        {
            "Before the world had a name, eleven gods watched over the heavens.",
            "Then the demons came. The sky burned, and the gods rose to meet them.",
            "In the end, the ten gods fell, and their power sank into the world below.",
            "But something watched from the dark... and did not fall.",
            ""
        };

        public static IntroCinematic Play(Camera c, Transform uiParent, Action onDone)
        {
            GameObject g = new GameObject("SL_Intro");
            IntroCinematic v = g.AddComponent<IntroCinematic>();
            v.cam = c; v.done = onDone;
            v.ui = UIKit.Fill("Intro", uiParent);
            v.sky = SkyEnvironment.Create(c); v.sky.Lobby = true; v.sky.SetHour(12f);
            v.world = new GameObject("IntroWorld");
            for (int i = 0; i < 4; i++)
            {
                GameObject isl = FloatingIsland.Build(16f + i * 5f, i % 2 == 0, 100 + i, false);
                isl.transform.SetParent(v.world.transform, false);
                isl.transform.position = new Vector3(-60f + i * 40f, -12f - i * 3f, 60f + i * 20f);
            }
            GameObject plat = new GameObject("Dais"); plat.transform.SetParent(v.world.transform, false);
            MeshBuilder mb = new MeshBuilder(); mb.Cylinder(new Vector3(0f, -1.2f, 0f), 1.2f, 22f, 20f, 32, SLMath.Hex(0xcfcabd), true);
            mb.Cylinder(new Vector3(0f, 0f, 0f), 0.15f, 21f, 21f, 32, SLMath.Hex(0xe8e2d0), true);
            plat.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("Dais"); plat.AddComponent<MeshRenderer>().sharedMaterial = SLMat.Vertex();
            v.BuildGods();
            RectTransform sr = UIKit.Rect("sub", v.ui, 0.5f, 0f, 0.5f, 0f, -560f, 40f, 560f, 130f);
            v.sub = UIKit.Label(sr, "", 30, Color.white, TextAnchor.MiddleCenter);
            RectTransform tr = UIKit.Rect("title", v.ui, 0.5f, 0.5f, 0.5f, 0.5f, -600f, -120f, 600f, 160f);
            v.title = UIKit.Label(tr, "<b>SOULBOUND</b>\n<size=52>L E G E N D S</size>", 110, UIKit.Gold, TextAnchor.MiddleCenter);
            v.title.color = new Color(UIKit.Gold.r, UIKit.Gold.g, UIKit.Gold.b, 0f);
            RectTransform fr = UIKit.Fill("fade", v.ui); UIKit.Img(fr, Color.black, false); fr.GetComponent<Image>().raycastTarget = false; v.fade = fr.gameObject.AddComponent<CanvasGroup>(); v.fade.alpha = 1f;
            UIKit.Btn(UIKit.AtRight("skip", v.ui, 14f, 14f, 120f, 48f), "Skip >>", 20, new Color(0f, 0f, 0f, 0.55f), Color.white, v.Finish);
            return v;
        }

        void BuildGods()
        {
            int[] looks = { 11, 23, 37, 41, 52, 67, 71, 83, 97, 101 };
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.Lerp(-70f, 70f, i / 9f) * Mathf.Deg2Rad;
                Look lk = Look.Random(looks[i]);
                GameObject g = CharacterBuilder.Build(lk, i, "God" + i, true, 2.1f + (i == 6 ? 0.5f : 0f));
                g.transform.SetParent(world.transform, false);
                g.transform.position = new Vector3(Mathf.Sin(a) * 15f, 0.1f, 20f + Mathf.Cos(a) * 9f);
                g.transform.rotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0f, -1f));
                CharacterRig rig = g.GetComponent<CharacterRig>();
                WeaponType[] wt = { WeaponType.Staff, WeaponType.Greatsword, WeaponType.Spear, WeaponType.Sword, WeaponType.Sword, WeaponType.Staff, WeaponType.Greatsword, WeaponType.Bow, WeaponType.Spear, WeaponType.Staff };
                rig.SetWeapon(wt[i], 4, ElementInfo.Col(i));
                gods.Add(g);
            }
        }

        void SpawnDemons()
        {
            string[] plans = { "heavy", "flyer", "quad", "flyer", "heavy" };
            for (int i = 0; i < 14; i++)
            {
                CreatureStyle st = new CreatureStyle { Plan = plans[i % 5], Scale = 1.6f + (i % 3) * 0.5f, ColA = 0x3a1018, ColB = 0xc02020, ColC = 0xff5a20, Id = "demon" + i, Boss = i % 5 == 0 };
                GameObject d = CreatureBuilder.Build(st);
                d.transform.SetParent(world.transform, false);
                float x = -45f + i * 7f + SLMath.Hash(i, 1, 5) * 4f;
                d.transform.position = new Vector3(x, st.Plan == "flyer" ? 8f : 0.1f, 95f + SLMath.Hash(i, 2, 5) * 30f);
                d.transform.rotation = Quaternion.LookRotation(new Vector3(-x * 0.01f, 0f, -1f));
                d.GetComponent<CreatureRig>().SetMove(0.9f);
                demons.Add(d);
            }
        }

        void Update()
        {
            if (finished) return;
            t += Time.deltaTime;
            int ph = 0; while (ph < phaseEnd.Length - 1 && t > phaseEnd[ph]) ph++;
            if (t > phaseEnd[phaseEnd.Length - 1]) { Finish(); return; }
            if (ph != phase) EnterPhase(ph);
            float start = ph == 0 ? 0f : phaseEnd[ph - 1], len = phaseEnd[ph] - start, k = Mathf.Clamp01((t - start) / len);
            fade.alpha = t < 1.5f ? 1f - t / 1.5f : (t > phaseEnd[4] - 1.5f ? (t - (phaseEnd[4] - 1.5f)) / 1.5f : 0f);
            if (ph == 4) fade.alpha = Mathf.Max(0f, 0f);
            float sa = Mathf.Clamp01(Mathf.Min((t - start) / 1.2f, (phaseEnd[ph] - t) / 1.0f)); sub.color = new Color(1f, 1f, 1f, sa);
            cam.transform.position = Vector3.Lerp(camFrom, camTo, k * k * (3f - 2f * k));
            Vector3 la = lookAt; cam.transform.LookAt(la);
            if (ph == 1) { cam.transform.position += new Vector3(Mathf.Sin(t * 30f), Mathf.Cos(t * 27f), 0f) * 0.08f; AdvanceWar(k); }
            if (ph == 2) AdvanceFall(k);
            if (ph == 3) { }
            if (ph == 4) { title.color = new Color(UIKit.Gold.r, UIKit.Gold.g, UIKit.Gold.b, Mathf.Clamp01(k * 3f)); fade.alpha = k > 0.85f ? (k - 0.85f) / 0.15f : 0f; }
        }

        void EnterPhase(int ph)
        {
            phase = ph; sub.text = Lines[Mathf.Min(ph, Lines.Length - 1)];
            if (ph == 0) { camFrom = new Vector3(-6f, 3f, -8f); camTo = new Vector3(8f, 4.2f, -2f); lookAt = new Vector3(0f, 3.5f, 22f); sky.SetHour(12f); }
            else if (ph == 1)
            {
                camFrom = new Vector3(0f, 5f, 4f); camTo = new Vector3(10f, 9f, 14f); lookAt = new Vector3(0f, 6f, 40f); sky.SetHour(18.2f); SpawnDemons();
                for (int i = 0; i < gods.Count; i++) gods[i].GetComponent<CharacterRig>().Play(Anim.Cast);
            }
            else if (ph == 2) { camFrom = new Vector3(0f, 8f, -4f); camTo = new Vector3(0f, -40f, -6f); lookAt = new Vector3(0f, -10f, 20f); sky.SetHour(20f); for (int i = 0; i < demons.Count; i++) Destroy(demons[i]); demons.Clear(); }
            else if (ph == 3)
            {
                camFrom = new Vector3(0f, -45f, 0f); camTo = new Vector3(0f, -42f, 6f); lookAt = new Vector3(0f, -44f, 40f); sky.SetHour(23.5f);
                for (int i = 0; i < gods.Count; i++) gods[i].SetActive(false);
                BuildShadow();
            }
            else if (ph == 4) { camFrom = new Vector3(0f, -42f, 6f); camTo = new Vector3(0f, -38f, -2f); lookAt = new Vector3(0f, -38f, 40f); sky.SetHour(6f); }
        }

        float nextBang;
        void AdvanceWar(float k)
        {
            for (int i = 0; i < demons.Count; i++) { Vector3 p = demons[i].transform.position; p.z -= Time.deltaTime * (3.5f + (i % 3)); demons[i].transform.position = p; }
            if (Time.time > nextBang && ClientFxHelper.Ready)
            {
                nextBang = Time.time + 0.22f;
                Vector3 pos = new Vector3(UnityEngine.Random.Range(-25f, 25f), UnityEngine.Random.Range(1f, 10f), UnityEngine.Random.Range(35f, 70f));
                ClientFxHelper.Burst(pos, UnityEngine.Random.value < 0.5f ? new Color(1f, 0.5f, 0.15f) : new Color(0.6f, 0.5f, 1f), 40);
            }
        }

        void AdvanceFall(float k)
        {
            if (k < 0.01f) for (int i = 0; i < gods.Count; i++) gods[i].SetActive(true);
            for (int i = 0; i < gods.Count; i++)
            {
                Transform g = gods[i].transform;
                g.position += new Vector3(Mathf.Sin(i * 1.7f) * 0.6f, -22f - i * 0.9f, 0f) * Time.deltaTime;
                g.Rotate(Time.deltaTime * (20f + i * 8f), 0f, Time.deltaTime * 15f);
            }
        }

        void BuildShadow()
        {
            GameObject s = CharacterBuilder.Build(Look.Default(), 10, "Shadow", true, 3.4f);
            s.transform.SetParent(world.transform, false); s.transform.position = new Vector3(0f, -44f, 38f); s.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            s.GetComponent<CharacterRig>().SetGlow(new Color(0.6f, 0.25f, 1f));
            extras.Add(s);
        }

        void Finish()
        {
            if (finished) return; finished = true;
            if (world != null) Destroy(world);
            if (sky != null) Destroy(sky.gameObject);
            if (ui != null) Destroy(ui.gameObject);
            SLSave.SetInt("intro_seen", 1); SLSave.Flush();
            Action cb = done; Destroy(gameObject);
            if (cb != null) cb();
        }
    }

    /// <summary>Tiny bridge so the cinematic can spawn particle bursts without the network assembly.</summary>
    public static class ClientFxHelper
    {
        public static bool Ready { get { return true; } }
        public static void Burst(Vector3 pos, Color c, int n)
        {
            GameObject g = new GameObject("fxp"); g.transform.position = pos;
            ParticleSystem ps = g.AddComponent<ParticleSystem>();
            var m = ps.main; m.loop = false; m.playOnAwake = false; m.startLifetime = 1.1f; m.startSpeed = 9f; m.startSize = 1.1f; m.startColor = c; m.gravityModifier = 0.1f; m.simulationSpace = ParticleSystemSimulationSpace.World;
            var e = ps.emission; e.rateOverTime = 0f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.5f;
            g.GetComponent<ParticleSystemRenderer>().sharedMaterial = SLMat.Particle(Color.white);
            ps.Emit(n); UnityEngine.Object.Destroy(g, 2f);
        }
    }
}
