using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using SL.Core;
using SL.Actors;
using SL.Gameplay;
using SL.World;

namespace SL.Net
{
    /// <summary>Input state written by the UI and read by the client world.</summary>
    public static class Controls
    {
        public static Vector2 Move; public static bool Sprint, Block;
        public static float CamYaw = 0f, CamPitch = 20f;
        public static string Prompt = ""; public static bool HudVisible;
    }

    public sealed class FxLife : MonoBehaviour
    {
        public float Life = 0.5f, Age; public Vector3 From = Vector3.one, To = Vector3.one, Vel; public bool Shrink;
        void Update()
        {
            Age += Time.deltaTime; float t = Mathf.Clamp01(Age / Life);
            Vector3 s = Vector3.Lerp(From, To, t); if (Shrink) s *= (1f - t * t);
            transform.localScale = s; transform.position += Vel * Time.deltaTime;
            if (Age >= Life) Destroy(gameObject);
        }
    }

    /// <summary>Everything the player sees in the world: zone, sky, NPCs, effects, camera, name tags, and local input handling.</summary>
    public sealed class ClientWorld : MonoBehaviour
    {
        public static ClientWorld I;
        public ZoneView View; public int Zone = -1, InstId = -1;
        Camera cam; SkyEnvironment sky;
        readonly HashSet<int> destroyed = new HashSet<int>(), openedChests = new HashSet<int>();
        readonly List<GameObject> npcObjs = new List<GameObject>();
        struct FloatT { public Vector3 Pos; public string Text; public int Kind; public float Born; }
        readonly List<FloatT> floats = new List<FloatT>();
        float faceYaw, promptT; GUIStyle labelStyle, smallStyle;
        static Mesh discMesh, crescentMesh, orbMesh;

        public static ClientWorld Create(Camera camera)
        {
            if (I != null) return I;
            GameObject g = new GameObject("SL_ClientWorld");
            DontDestroyOnLoad(g);
            I = g.AddComponent<ClientWorld>();
            I.cam = camera;
            I.sky = SkyEnvironment.Create(camera);
            I.sky.Lobby = false;
            PlayerNet.ZoneEvent += I.OnZone;
            PlayerNet.ProfileEvent += I.OnProfile;
            return I;
        }

        public void Shutdown()
        {
            PlayerNet.ZoneEvent -= OnZone; PlayerNet.ProfileEvent -= OnProfile;
            ClearWorld();
            if (sky != null) Destroy(sky.gameObject);
            I = null; Destroy(gameObject);
        }

        void ClearWorld()
        {
            if (View != null) View.Destroy(); View = null;
            for (int i = 0; i < npcObjs.Count; i++) if (npcObjs[i] != null) Destroy(npcObjs[i]);
            npcObjs.Clear(); destroyed.Clear(); openedChests.Clear(); Zone = -1; InstId = -1;
        }

        // ------------------------------------------------------------ zone handling
        void OnZone(int zone, int inst, Vector3 pos, string sync)
        {
            if (zone != Zone || inst != InstId)
            {
                ClearWorld();
                Zone = zone; InstId = inst;
                View = ZoneView.Build(zone);
                BuildNpcs();
                ApplySync(sync);
                OnProfile();
            }
            if (PlayerNet.Local != null) { PlayerNet.Local.transform.position = pos; }
            if (zone == 1) Controls.CamYaw = 0f; else Controls.CamYaw = 180f;
        }

        void ApplySync(string sync)
        {
            if (string.IsNullOrEmpty(sync) || View == null) return;
            string[] parts = sync.Split('|');
            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i]; if (p.Length < 3) continue;
                string body = p.Substring(2); if (body.Length == 0) continue;
                char k = p[0];
                if (k == 'c')
                {
                    string[] cs = body.Split(';');
                    for (int c = 0; c < cs.Length; c++) { string[] f = cs[c].Split(','); if (f.Length == 4) View.AddCrater(Num(f[0]), Num(f[1]), Num(f[2]), Num(f[3])); }
                    continue;
                }
                string[] ids = body.Split(',');
                for (int n = 0; n < ids.Length; n++)
                {
                    int id; if (!int.TryParse(ids[n], out id)) continue;
                    if (k == 'b') { destroyed.Add(id); View.SetBlockDestroyed(id, false); }
                    else if (k == 'n') View.SetNodeActive(id, false);
                    else if (k == 'k') { openedChests.Add(id); View.SetChestOpened(id, true); }
                }
            }
        }

        static float Num(string s) { float f; float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f); return f; }

        void OnProfile()
        {
            if (View == null) return;
            PlayerNet me = PlayerNet.Local; if (me == null || me.Prof == null) { View.SetPortalActive(false); return; }
            View.SetPortalActive(Zone == 1 || (me.Prof.promoted && me.Prof.Count("kingdom_token") > 0));
        }

        public void WorldEvent(int kind, int id, float a, float b, float c)
        {
            if (View == null) return;
            if (kind == 2) { destroyed.Add(id); View.SetBlockDestroyed(id, true); }
            else if (kind == 3) { destroyed.Remove(id); View.RestoreBlock(id); }
            else if (kind == 4) View.SetNodeActive(id, a > 0.5f);
            else if (kind == 5) { View.AddCrater(a, b, c, 0.9f); }
            else if (kind == 6) { if (a > 0.5f) openedChests.Remove(id); else openedChests.Add(id); View.SetChestOpened(id, a < 0.5f); }
        }

        void BuildNpcs()
        {
            for (int i = 0; i < View.Layout.Npcs.Count; i++)
            {
                NpcDef n = View.Layout.Npcs[i];
                Look lk = Look.Random(1000 + n.Look * 7 + i * 13 + n.Id.Length);
                GameObject c = CharacterBuilder.Build(lk, (n.Look + i) % 10, n.Name);
                c.transform.position = n.Pos; c.transform.rotation = Quaternion.Euler(0f, n.Yaw, 0f);
                CharacterRig rig = c.GetComponent<CharacterRig>();
                if (n.Role == "guard") rig.SetWeapon(WeaponType.Spear, 1, ElementInfo.Col(1));
                else if (n.Role == "smith") rig.SetWeapon(WeaponType.Sword, 2, ElementInfo.Col(1));
                npcObjs.Add(c);
            }
        }

        // ------------------------------------------------------------ per-frame
        void Update()
        {
            PlayerNet me = PlayerNet.Local;
            if (me == null || View == null || cam == null) return;
            float dt = Time.deltaTime;
            NetworkManager nm = NetworkManager.Singleton;
            bool dead = me.NDead.Value;
            Vector2 mv = dead ? Vector2.zero : Controls.Move;
            Vector3 wm = Quaternion.Euler(0f, Controls.CamYaw, 0f) * new Vector3(mv.x, 0f, mv.y);
            Vector2 dir = new Vector2(wm.x, wm.z);
            if (dir.sqrMagnitude > 0.04f) faceYaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
            me.LocalYaw = faceYaw;
            me.SendInput(dir, faceYaw, Controls.Sprint, Controls.Block);
            if (nm != null && !nm.IsServer && !dead && dir.sqrMagnitude > 0.01f)
            {
                float speed = Controls.Block ? Mover.BlockSpeed : (Controls.Sprint ? Mover.SprintSpeed : Mover.WalkSpeed);
                me.transform.position = Mover.Step(View.Layout, View.Hf, destroyed, me.transform.position, dir, speed, dt);
            }
            UpdateCamera(me, dt);
            promptT -= dt;
            if (promptT <= 0f) { promptT = 0.15f; UpdatePrompt(me); }
            for (int i = floats.Count - 1; i >= 0; i--) if (Time.time - floats[i].Born > 1.4f) floats.RemoveAt(i);
        }

        void UpdateCamera(PlayerNet me, float dt)
        {
            Vector3 target = me.transform.position + new Vector3(0f, 1.6f, 0f);
            Quaternion rot = Quaternion.Euler(Controls.CamPitch, Controls.CamYaw, 0f);
            Vector3 pos = target + rot * new Vector3(0f, 0f, -6.8f);
            float g = View.Hf.At(pos.x, pos.z) + 0.7f; if (pos.y < g) pos.y = g;
            cam.transform.position = Vector3.Lerp(cam.transform.position, pos, 1f - Mathf.Exp(-16f * dt));
            cam.transform.LookAt(target);
        }

        /// <summary>Yaw that aims at the nearest monster in front/near the player, else the movement facing.</summary>
        public float AimYaw()
        {
            PlayerNet me = PlayerNet.Local; if (me == null) return faceYaw;
            float best = 9f; float yaw = faceYaw; Vector3 p = me.transform.position;
            for (int i = 0; i < MonsterNet.All.Count; i++)
            {
                MonsterNet m = MonsterNet.All[i];
                if (m.NDead.Value || m.NZone.Value != me.NZone.Value) continue;
                Vector3 to = m.transform.position - p; to.y = 0f; float d = to.magnitude;
                if (d < best) { best = d; yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg; }
            }
            faceYaw = yaw; return yaw;
        }

        void UpdatePrompt(PlayerNet me)
        {
            Vector3 p = me.transform.position; ZoneLayout L = View.Layout;
            float best = 3.6f; string txt = "";
            for (int i = 0; i < L.Npcs.Count; i++) { float d = Vector3.Distance(p, L.Npcs[i].Pos); if (d < best) { best = d; txt = "Talk to " + L.Npcs[i].Name; } }
            float dp = Vector3.Distance(p, L.PortalPos); if (dp < 4.5f && dp < best + 1f) { best = dp; txt = Zone == 1 ? "Return through the portal" : "Enter the Kingdom portal"; }
            for (int i = 0; i < L.Nodes.Count; i++)
            {
                NodeDef n = L.Nodes[i]; GameObject go; if (!View.NodeObjs.TryGetValue(n.Id, out go) || go == null || !go.activeSelf) continue;
                float d = Vector3.Distance(p, n.Pos) - 0.4f; if (d < best) { best = d; txt = (n.Kind == NodeKind.Herb ? "Pick herb" : (n.Kind == NodeKind.Tree ? "Chop tree" : (n.Kind == NodeKind.Ore ? "Mine ore" : "Mine crystal"))); }
            }
            for (int i = 0; i < L.Chests.Count; i++)
            {
                ChestDef c = L.Chests[i]; if (openedChests.Contains(c.Id)) continue;
                float d = Vector3.Distance(p, c.Pos) - 0.4f; if (d < best) { best = d; txt = "Open " + Items.ChestGrades[c.Grade] + " Chest"; }
            }
            Controls.Prompt = txt;
        }

        // ------------------------------------------------------------ effects
        static Mesh Orb() { if (orbMesh == null) { MeshBuilder m = new MeshBuilder(); m.Ellipsoid(Vector3.zero, new Vector3(0.2f, 0.2f, 0.2f), Color.white, 8, 5); orbMesh = m.ToMesh("FxOrb"); } return orbMesh; }
        static Mesh Disc() { if (discMesh == null) { MeshBuilder m = new MeshBuilder(); m.Cylinder(Vector3.zero, 0.05f, 1f, 1f, 24, Color.white, true); discMesh = m.ToMesh("FxDisc"); } return discMesh; }
        static Mesh Crescent()
        {
            if (crescentMesh == null)
            {
                MeshBuilder m = new MeshBuilder(); Vector3[] a = new Vector3[9], b = new Vector3[9];
                for (int i = 0; i < 9; i++) { float t = (i / 8f - 0.5f) * 2.2f; a[i] = new Vector3(Mathf.Sin(t) * 1.1f, 0f, Mathf.Cos(t) * 1.1f); b[i] = new Vector3(Mathf.Sin(t) * 1.7f, 0f, Mathf.Cos(t) * 1.7f); }
                m.Strip(a, b, Color.white); crescentMesh = m.ToMesh("FxCrescent");
            }
            return crescentMesh;
        }

        GameObject Shape(Mesh mesh, Vector3 pos, Quaternion rot, Color c, float life, Vector3 from, Vector3 to, Vector3 vel, bool shrink)
        {
            GameObject g = new GameObject("fx");
            g.transform.SetPositionAndRotation(pos, rot);
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            g.AddComponent<MeshRenderer>().sharedMaterial = SLMat.Lit(c, 0.3f, 0f, c * 2f);
            FxLife f = g.AddComponent<FxLife>(); f.Life = life; f.From = from; f.To = to; f.Vel = vel; f.Shrink = shrink;
            return g;
        }

        void Burst(Vector3 pos, Color c, int n, float speed, float life, float size, float gravity)
        {
            GameObject g = new GameObject("fxp"); g.transform.position = pos;
            ParticleSystem ps = g.AddComponent<ParticleSystem>();
            var m = ps.main; m.loop = false; m.playOnAwake = false; m.startLifetime = life; m.startSpeed = speed; m.startSize = size; m.startColor = c; m.gravityModifier = gravity; m.simulationSpace = ParticleSystemSimulationSpace.World;
            var e = ps.emission; e.rateOverTime = 0f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.25f;
            g.GetComponent<ParticleSystemRenderer>().sharedMaterial = SLMat.Particle(Color.white);
            ps.Emit(n); Destroy(g, life + 0.6f);
        }

        public void Fx(int fx, Vector3 pos, Vector3 dir, int element)
        {
            Color c = element >= 0 ? ElementInfo.Col(element) : Color.white;
            if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;
            Quaternion face = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z).sqrMagnitude < 0.001f ? Vector3.forward : new Vector3(dir.x, 0f, dir.z));
            switch (fx)
            {
                case 0: Shape(Crescent(), pos + dir.normalized * 0.4f, face, c, 0.22f, Vector3.one * 0.7f, Vector3.one * 1.25f, Vector3.zero, true); break;
                case 1: Burst(pos, Color.Lerp(c, Color.white, 0.5f), 14, 4f, 0.4f, 0.16f, 0.5f); break;
                case 2: { GameObject o = Shape(Orb(), pos, face, c, 0.55f, Vector3.one, Vector3.one * 0.7f, dir.normalized * 26f, false); Burst(pos, c, 6, 1.5f, 0.3f, 0.12f, 0f); } break;
                case 10: Shape(Orb(), pos, face, new Color(0.5f, 1f, 0.3f), 0.6f, Vector3.one * 1.4f, Vector3.one, dir.normalized * 14f, false); break;
                case 3: Shape(Disc(), pos + Vector3.up * 0.15f, Quaternion.identity, c, 0.5f, new Vector3(0.5f, 1f, 0.5f), new Vector3(7f, 1f, 7f), Vector3.zero, true); Burst(pos + Vector3.up, c, 40, 7f, 0.6f, 0.22f, 0.3f); break;
                case 4: Burst(pos, c, 30, 3f, 0.5f, 0.2f, 0f); Burst(pos + dir * 0.5f, Color.white, 14, 2f, 0.3f, 0.14f, 0f); break;
                case 5: Burst(pos + Vector3.up * 0.2f, new Color(0.5f, 1f, 0.6f), 26, 1.6f, 0.9f, 0.18f, -0.3f); Shape(Disc(), pos + Vector3.up * 0.1f, Quaternion.identity, new Color(0.4f, 1f, 0.6f), 0.7f, new Vector3(0.6f, 1f, 0.6f), new Vector3(3f, 1f, 3f), Vector3.zero, true); break;
                case 6: Shape(Orb(), pos + Vector3.up * 1f, Quaternion.identity, c, 0.8f, Vector3.one * 5f, Vector3.one * 6.5f, Vector3.zero, true); break;
                case 7: Burst(pos + Vector3.up * 0.3f, new Color(0.55f, 0.45f, 0.35f), 40, 5f, 0.8f, 0.5f, 0.6f); break;
                case 8: Shape(Disc(), pos + Vector3.up * 0.1f, Quaternion.identity, c, 1f, new Vector3(0.5f, 20f, 0.5f), new Vector3(2.4f, 20f, 2.4f), Vector3.zero, true); Burst(pos + Vector3.up, c, 60, 5f, 1.1f, 0.25f, -0.2f); break;
                case 9: Burst(pos + Vector3.up * 0.5f, new Color(1f, 0.85f, 0.4f), 36, 3.5f, 0.9f, 0.2f, 0.3f); break;
                case 11: Shape(Disc(), pos + Vector3.up * 0.1f, Quaternion.identity, new Color(1f, 0.2f, 0.2f), 1.0f, new Vector3(1f, 1f, 1f), new Vector3(7f, 1f, 7f), Vector3.zero, false); break;
                case 12: Burst(pos + Vector3.up, new Color(0.8f, 0.8f, 1f), 30, 3f, 0.8f, 0.3f, -0.1f); break;
            }
        }

        public void FloatText(Vector3 pos, string text, int kind)
        {
            if (floats.Count > 40) floats.RemoveAt(0);
            floats.Add(new FloatT { Pos = pos, Text = text, Kind = kind, Born = Time.time });
        }

        // ------------------------------------------------------------ labels (IMGUI so no canvas is needed)
        void Label(Vector3 world, string text, Color col, GUIStyle st, float k, float yOff)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z < 0.1f) return;
            Rect r = new Rect(sp.x / k - 100f, (Screen.height - sp.y) / k - yOff, 200f, 24f);
            st.normal.textColor = Color.black; GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), text, st);
            st.normal.textColor = col; GUI.Label(r, text, st);
        }

        void OnGUI()
        {
            PlayerNet me = PlayerNet.Local;
            if (me == null || cam == null || View == null || !Controls.HudVisible) return;
            float k = Screen.height / 720f;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(k, k, 1f));
            if (labelStyle == null) { labelStyle = new GUIStyle(); labelStyle.alignment = TextAnchor.MiddleCenter; labelStyle.fontSize = 14; labelStyle.fontStyle = FontStyle.Bold; smallStyle = new GUIStyle(); smallStyle.alignment = TextAnchor.MiddleCenter; smallStyle.fontSize = 12; }
            Vector3 mp = me.transform.position;
            for (int i = 0; i < PlayerNet.All.Count; i++)
            {
                PlayerNet p = PlayerNet.All[i];
                if (p == me || !p.InMyInstance || Vector3.Distance(p.transform.position, mp) > 32f) continue;
                Label(p.transform.position + Vector3.up * 2.3f, p.DisplayName + "  Lv." + p.NLevel.Value, new Color(0.7f, 0.9f, 1f), labelStyle, k, 12f);
            }
            for (int i = 0; i < MonsterNet.All.Count; i++)
            {
                MonsterNet m = MonsterNet.All[i];
                if (m.NDead.Value || m.NZone.Value != me.NZone.Value) continue;
                MonsterDef d = m.ClientDef; if (d == null) continue;
                float dist = Vector3.Distance(m.transform.position, mp); if (dist > (d.Boss ? 60f : 26f)) continue;
                Vector3 top = m.transform.position + Vector3.up * (2.1f * d.Scale);
                Label(top, d.Name + "  Lv." + d.Level, d.Boss ? new Color(1f, 0.6f, 0.3f) : new Color(1f, 0.85f, 0.85f), smallStyle, k, 22f);
                Vector3 sp = cam.WorldToScreenPoint(top);
                if (sp.z > 0.1f)
                {
                    float w = d.Boss ? 120f : 60f, f = Mathf.Clamp01(m.NHp.Value / Mathf.Max(1f, (float)m.NMaxHp.Value));
                    Rect bg = new Rect(sp.x / k - w * 0.5f, (Screen.height - sp.y) / k - 6f, w, 6f);
                    GUI.color = new Color(0f, 0f, 0f, 0.7f); GUI.DrawTexture(bg, Texture2D.whiteTexture);
                    GUI.color = d.Boss ? new Color(1f, 0.4f, 0.2f) : new Color(0.9f, 0.2f, 0.2f); GUI.DrawTexture(new Rect(bg.x + 1f, bg.y + 1f, (w - 2f) * f, 4f), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
            }
            Profile pr = me.Prof;
            for (int i = 0; i < View.Layout.Npcs.Count; i++)
            {
                NpcDef n = View.Layout.Npcs[i];
                if (Vector3.Distance(n.Pos, mp) > 30f) continue;
                Label(n.Pos + Vector3.up * 2.3f, n.Name, new Color(0.9f, 1f, 0.8f), smallStyle, k, 12f);
                string mark = QuestMark(pr, n.Id);
                if (mark.Length > 0) Label(n.Pos + Vector3.up * 2.9f, mark, mark == "?" ? new Color(1f, 0.9f, 0.3f) : (mark == "!" ? new Color(1f, 0.85f, 0.2f) : Color.gray), labelStyle, k, 16f);
            }
            Color[] fc = { Color.white, new Color(1f, 0.3f, 0.3f), new Color(1f, 0.9f, 0.2f), new Color(0.8f, 0.5f, 1f), new Color(1f, 0.8f, 0.3f), new Color(0.5f, 1f, 0.6f) };
            for (int i = 0; i < floats.Count; i++)
            {
                FloatT f = floats[i]; float age = Time.time - f.Born;
                Label(f.Pos + Vector3.up * (age * 1.2f), f.Text, fc[Mathf.Clamp(f.Kind, 0, 5)], labelStyle, k, 12f);
            }
            GUI.matrix = Matrix4x4.identity;
        }

        static string QuestMark(Profile pr, string npc)
        {
            if (pr == null) return "";
            List<QuestDef> qs = Quests.ForGiver(npc); string res = "";
            for (int i = 0; i < qs.Count; i++)
            {
                QuestDef q = qs[i]; QuestProg qp = pr.Q(q.Id);
                if (qp != null && qp.accepted) { if (Rules.Complete(pr, q)) return "?"; res = res == "" ? "..." : res; }
                else if (Rules.Available(pr, q)) res = "!";
            }
            return res;
        }
    }
}
