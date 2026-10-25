using System;
using System.Collections;
using UnityEngine;
using SL.Core;
using SL.Actors;
using SL.Gameplay;
using SL.Net;
using SL.UI;
using SL.World;

namespace SL.Game
{
    /// <summary>
    /// The whole game boots from here; nothing has to be placed in the scene.
    /// Loading -> one-time intro -> account -> character creation -> lobby -> mode select -> connect -> world.
    /// Launch with "-sl-server" (or in batch mode) to run a headless dedicated server instead of the client.
    /// </summary>
    public sealed class GameFlow : MonoBehaviour
    {
        public static GameFlow I;
        Camera cam; LoadingView loading; AccountView account; CreatorView creator; LobbyView lobby; ModeView modes; ConnectView connect;
        bool inWorld, stageUp, connecting; float connectDeadline;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            Application.runInBackground = true;
            bool server = Application.isBatchMode;
            string[] args = Application.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++) if (args[i] == "-sl-server") server = true;
            if (server)
            {
                Application.targetFrameRate = 30;
                SLLog.I("Starting dedicated server on port " + NetBootstrap.Port);
                NetBootstrap.Create().StartServer();
                return;
            }
            GameObject g = new GameObject("SL_Game");
            DontDestroyOnLoad(g);
            I = g.AddComponent<GameFlow>();
        }

        void Start()
        {
            Application.targetFrameRate = SLSave.GetInt("fps", 60);
            QualitySettings.SetQualityLevel(Mathf.Clamp(SLSave.GetInt("quality", 1) * 2, 0, Mathf.Max(0, QualitySettings.names.Length - 1)));
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            SetupCamera();
            UIRoot.Create();
            Panels.OnReplayIntro = () => { Panels.Close(); ClearViews(); HideStage(); PlayIntro(ShowLobby); };
            Panels.OnEditCharacter = ShowCreator;
            Panels.OnLeaveWorld = () => LeaveWorld("");
            Panels.OnResetAll = () => { ClientIdentity.Name = "Adventurer"; ClientIdentity.Look = 0; ClearViews(); HideStage(); StartCoroutine(Boot2()); };
            NetBootstrap.Connected += OnConnected;
            NetBootstrap.Disconnected += OnDisconnected;
            StartCoroutine(Boot2());
        }

        void OnDestroy() { NetBootstrap.Connected -= OnConnected; NetBootstrap.Disconnected -= OnDisconnected; }

        void SetupCamera()
        {
            cam = Camera.main;
            if (cam == null) { GameObject c = new GameObject("MainCamera"); c.tag = "MainCamera"; cam = c.AddComponent<Camera>(); c.AddComponent<AudioListener>(); }
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black; cam.fieldOfView = 55f; cam.nearClipPlane = 0.2f; cam.farClipPlane = 700f;
        }

        void LoadIdentity()
        {
            ClientIdentity.Name = SLSave.GetStr("char_name", "Adventurer");
            uint look; if (uint.TryParse(SLSave.GetStr("char_look", "0"), out look)) ClientIdentity.Look = look;
        }

        // ------------------------------------------------------------------ boot sequence
        IEnumerator Boot2()
        {
            LoadIdentity(); UIRoot.ResetCache();
            loading = LoadingView.Build(UIRoot.I.ScreenLayer);
            string[] steps = { "Preparing the heavens...", "Raising Dawnrest Village...", "Raising Aurelion Kingdom...", "Waking the monsters...", "Almost ready..." };
            for (int i = 0; i < steps.Length; i++)
            {
                loading.Set((i + 1f) / steps.Length, steps[i]);
                if (i == 1) ZoneLayout.Get(0); else if (i == 2) ZoneLayout.Get(1);
                float t = 0f; while (t < 0.45f) { t += Time.deltaTime; loading.Tick(Time.deltaTime); yield return null; }
            }
            loading.Destroy(); loading = null;
            if (SLSave.GetInt("intro_seen", 0) == 0) PlayIntro(AfterIntro); else AfterIntro();
        }

        void PlayIntro(Action then) { IntroCinematic.Play(cam, UIRoot.I.ScreenLayer, then); }

        void AfterIntro()
        {
            if (SLSave.GetInt("signed_in", 0) == 0) ShowAccount();
            else StartCoroutine(LoadAccount());
        }

        void ShowAccount()
        {
            ShowStage(); ClearViews();
            account = AccountView.Build(UIRoot.I.ScreenLayer, () => { SLSave.SetInt("signed_in", 1); SLSave.Flush(); if (account != null) account.Destroy(); account = null; StartCoroutine(LoadAccount()); });
        }

        IEnumerator LoadAccount()
        {
            ClearViews(); HideStage();
            loading = LoadingView.Build(UIRoot.I.ScreenLayer);
            string[] s = { "Contacting the guild hall...", "Loading your account...", "Preparing your character..." };
            for (int i = 0; i < s.Length; i++) { loading.Set((i + 1f) / s.Length, s[i]); float t = 0f; while (t < 0.5f) { t += Time.deltaTime; yield return null; } }
            loading.Destroy(); loading = null;
            if (SLSave.GetInt("char_made", 0) == 0) ShowCreator(); else ShowLobby();
        }

        // ------------------------------------------------------------------ screens
        void ShowStage() { if (!stageUp) { Stage.Show(cam); stageUp = true; } }
        void HideStage() { Stage.Hide(); stageUp = false; }

        void ClearViews()
        {
            if (account != null) account.Destroy(); account = null;
            if (creator != null) creator.Destroy(); creator = null;
            if (lobby != null) lobby.Destroy(); lobby = null;
            if (modes != null) modes.Destroy(); modes = null;
            if (connect != null) connect.Destroy(); connect = null;
            if (loading != null) loading.Destroy(); loading = null;
        }

        void ShowCreator()
        {
            Panels.Close(); ClearViews(); ShowStage();
            creator = CreatorView.Build(UIRoot.I.ScreenLayer, () => { UIRoot.ResetCache(); ShowLobby(); });
        }

        void ShowLobby()
        {
            Panels.Close(); ClearViews(); ShowStage();
            Profile p = UIRoot.Prof;
            Stage.SetCharacter(Look.Unpack(ClientIdentity.Look), p.element, p.weapon);
            lobby = LobbyView.Build(UIRoot.I.ScreenLayer, ShowModes);
        }

        void ShowModes()
        {
            if (lobby != null) lobby.Destroy(); lobby = null;
            modes = ModeView.Build(UIRoot.I.ScreenLayer, ShowLobby, ShowConnect);
        }

        void ShowConnect()
        {
            if (connect != null) connect.Destroy();
            connect = ConnectView.Build(UIRoot.I.ScreenLayer, HostVillage, JoinServer, () => { if (connect != null) connect.Destroy(); connect = null; });
        }

        // ------------------------------------------------------------------ networking
        void HostVillage()
        {
            NetBootstrap nb = NetBootstrap.Create();
            connecting = true; connectDeadline = Time.time + 12f;
            if (!nb.StartHost()) Fail("Could not start the village on this phone.");
        }

        void JoinServer(string address)
        {
            NetBootstrap nb = NetBootstrap.Create();
            connecting = true; connectDeadline = Time.time + 12f;
            if (!nb.StartClient(address)) Fail("Could not start the connection.");
        }

        void Fail(string msg)
        {
            connecting = false;
            if (NetBootstrap.I != null) NetBootstrap.I.Stop();
            if (connect != null) connect.SetStatus(msg); else UIRoot.I.Toast(msg);
        }

        void OnConnected()
        {
            if (inWorld) return;
            connecting = false; inWorld = true;
            Panels.Close(); ClearViews(); HideStage();
            SetupCamera();
            ClientWorld.Create(cam);
            HudView.Create(UIRoot.I.ScreenLayer);
        }

        void OnDisconnected() { if (inWorld) LeaveWorld("Disconnected from the server."); else if (connecting) Fail("Could not connect. Check the address and that the host is running."); }

        void LeaveWorld(string msg)
        {
            Panels.Close();
            if (HudView.I != null) HudView.I.Destroy();
            if (ClientWorld.I != null) ClientWorld.I.Shutdown();
            if (NetBootstrap.I != null) NetBootstrap.I.Stop();
            inWorld = false; connecting = false;
            SetupCamera();
            ShowLobby();
            if (!string.IsNullOrEmpty(msg)) UIRoot.I.Toast(msg);
        }

        void Update()
        {
            if (loading != null) loading.Tick(Time.deltaTime);
            if (connecting && !inWorld && Time.time > connectDeadline) Fail("Connection timed out. Check the address and that the host is running.");
            if (lobby != null) { lobbyT += Time.deltaTime; if (lobbyT > 1f) { lobbyT = 0f; lobby.Refresh(); } }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Panels.IsOpen) Panels.Close();
                else if (connect != null) { connect.Destroy(); connect = null; }
                else if (modes != null) ShowLobby();
            }
        }
        float lobbyT;
    }
}
