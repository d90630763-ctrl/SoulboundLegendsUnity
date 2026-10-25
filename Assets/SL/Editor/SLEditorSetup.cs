using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Unity.Netcode;
using SL.Net;

namespace SL.EditorTools
{
    /// <summary>
    /// Makes the project build-ready by itself: creates the two network prefabs, registers the scene and applies Android settings.
    /// It runs when the project opens, before every build (Unity Cloud Build included), and from the Soulbound menu.
    /// </summary>
    [InitializeOnLoad]
    public static class SLEditorSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string NetFolder = "Assets/Resources/SL/Net";

        static SLEditorSetup() { EditorApplication.delayCall += RunQuiet; }
        static void RunQuiet() { Ensure(false); }

        public static string Ensure(bool log)
        {
            string report = "";
            try
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
                if (!AssetDatabase.IsValidFolder("Assets/Resources/SL")) AssetDatabase.CreateFolder("Assets/Resources", "SL");
                if (!AssetDatabase.IsValidFolder(NetFolder)) AssetDatabase.CreateFolder("Assets/Resources/SL", "Net");
                report += MakePrefab(NetFolder + "/PlayerNet.prefab", "PlayerNet", typeof(PlayerNet));
                report += MakePrefab(NetFolder + "/MonsterNet.prefab", "MonsterNet", typeof(MonsterNet));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (System.Exception e) { report += "Prefab setup problem: " + e.Message + "\n"; }
            try { report += RegisterScene(); } catch (System.Exception e) { report += "Scene registration problem: " + e.Message + "\n"; }
            try { ApplyPlayerSettings(); report += "Android settings applied.\n"; } catch (System.Exception e) { report += "Player settings problem: " + e.Message + "\n"; }
            if (log) Debug.Log("[SL] Setup:\n" + report);
            return report;
        }

        static string MakePrefab(string path, string name, System.Type component)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return name + " prefab: ok\n";
            GameObject go = new GameObject(name);
            go.AddComponent<NetworkObject>();
            go.AddComponent(component);
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return name + " prefab: created\n";
        }

        static string RegisterScene()
        {
            EditorBuildSettingsScene[] cur = EditorBuildSettings.scenes;
            for (int i = 0; i < cur.Length; i++) if (cur[i].path == ScenePath) { if (cur[i].enabled) return "Scene: ok\n"; cur[i].enabled = true; EditorBuildSettings.scenes = cur; return "Scene: enabled\n"; }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            return "Scene: registered\n";
        }

        static void ApplyPlayerSettings()
        {
            PlayerSettings.productName = "Soulbound Legends";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        }

        // ---------------------------------------------------------------- Unity Cloud Build hook (optional)
        /// <summary>Set this as the "Pre-Export Method" in Unity Cloud Build: SL.EditorTools.SLEditorSetup.CloudPreExport</summary>
        public static void CloudPreExport() { Debug.Log(Ensure(true)); }

        [MenuItem("Soulbound/Build Game (prepare + validate)")]
        public static void BuildGameMenu()
        {
            string r = Ensure(true);
            string[] need =
            {
                "Assets/Resources/SL/Shaders/VertexColorLit.shader", "Assets/Resources/SL/Shaders/Water.shader", "Assets/Resources/SL/Shaders/Sky.shader", "Assets/Resources/SL/Shaders/ParticleGlow.shader",
                "Assets/Resources/SL/Net/PlayerNet.prefab", "Assets/Resources/SL/Net/MonsterNet.prefab", ScenePath
            };
            int missing = 0;
            for (int i = 0; i < need.Length; i++) if (!File.Exists(need[i])) { missing++; r += "MISSING: " + need[i] + "\n"; }
            r += missing == 0 ? "All required files present. Press Play, or use Soulbound > Build Android APK.\n" : missing + " required file(s) missing.\n";
            Debug.Log("[SL] Build Game report:\n" + r);
            EditorUtility.DisplayDialog("Soulbound Legends - Build Game", r, "OK");
        }

        [MenuItem("Soulbound/Export Admin Compendium")]
        public static void ExportCompendium()
        {
            Directory.CreateDirectory("Docs");
            File.WriteAllText("Docs/ADMIN_COMPENDIUM.md", SL.Gameplay.AdminCompendium.Build());
            Debug.Log("[SL] Wrote Docs/ADMIN_COMPENDIUM.md");
        }

        [MenuItem("Soulbound/Build Android APK")]
        public static void BuildApk()
        {
            Ensure(true);
            Directory.CreateDirectory("Builds");
            BuildPlayerOptions o = new BuildPlayerOptions();
            o.scenes = new[] { ScenePath };
            o.locationPathName = "Builds/SoulboundLegends.apk";
            o.target = BuildTarget.Android;
            o.options = BuildOptions.None;
            BuildReport rep = BuildPipeline.BuildPlayer(o);
            Debug.Log("[SL] APK build result: " + rep.summary.result);
        }
    }

    public sealed class SLBuildPreprocess : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return -1000; } }
        public void OnPreprocessBuild(BuildReport report) { SLEditorSetup.Ensure(true); }
    }
}
