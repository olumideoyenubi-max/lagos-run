using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-off project setup and command-line builds:
//   Unity -batchmode -quit -projectPath <path> -executeMethod LRBuild.Setup
//   Unity -batchmode -quit -projectPath <path> -executeMethod LRBuild.BuildMac   (or BuildWeb / BuildAndroid / BuildIOS)
public static class LRBuild {
    const string ScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("Runtown/Set Up Project")]
    public static void Setup() {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Runtown").AddComponent<LRGame>();
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        // Archivo's Latin subset has no naira sign; fall back to Bungee, which does
        foreach (string f in new[] { "Assets/Resources/Archivo-ExtraBold.ttf", "Assets/Resources/Archivo-SemiBold.ttf" }) {
            var imp = AssetImporter.GetAtPath(f) as TrueTypeFontImporter;
            if (imp == null) continue;
            imp.fontNames = new[] { imp.fontTTFName, "Bungee" };
            imp.SaveAndReimport();
        }

        PlayerSettings.companyName = "Olumide Oyenubi";
        PlayerSettings.productName = "Runtown";
        PlayerSettings.colorSpace = ColorSpace.Gamma;
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "com.olumideoyenubi.lagosrun");
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.olumideoyenubi.lagosrun");
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, "com.olumideoyenubi.lagosrun");
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = false;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        AssetDatabase.SaveAssets();
        Debug.Log("LRBuild.Setup done");
    }

    static void Build(BuildTarget target, string path, BuildOptions options = BuildOptions.None) {
        // the game was first called Lagos Run; the app identifier keeps that name so saved progress carries over
        PlayerSettings.productName = "Runtown";
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = path, target = target, options = options });
        Debug.Log("LRBuild " + target + ": " + report.summary.result + ", " + report.summary.totalErrors + " errors, " + (report.summary.totalSize / 1048576) + " MB -> " + path);
        if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }
    [MenuItem("Runtown/Build Mac App")] public static void BuildMac() {
        // open full screen at the display's own resolution
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow; PlayerSettings.defaultIsNativeResolution = true;
        Build(BuildTarget.StandaloneOSX, "Builds/Mac/Runtown.app");
    }
    [MenuItem("Runtown/Build Web")] public static void BuildWeb() { PlayerSettings.WebGL.template = "PROJECT:LagosRun"; Build(BuildTarget.WebGL, "Builds/Web"); }
    [MenuItem("Runtown/Build Android APK")] public static void BuildAndroid() { Build(BuildTarget.Android, "Builds/Android/Runtown.apk"); }
    [MenuItem("Runtown/Build iOS Xcode Project")] public static void BuildIOS() { Build(BuildTarget.iOS, "Builds/iOS"); }
}
