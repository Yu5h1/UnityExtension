// Builds only the native numerical validation scene and restores the host's settings afterward.
using System;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yu5h1Lib.Tests;

namespace Yu5h1Lib.EditorExtension
{
    public static class MotionValidationBuild
    {
        [Test] public static void EditorFixtures() => new MotionNativeTests().SharedReferenceFixtures();
        [Test] public static void EditorLifecycle() => new MotionNativeTests().LifecycleAndInstanceIsolation();
        public static void Windows() => Build(false);
        public static void Android() => Build(true);
        private static void Build(bool android)
        {
            for (int i=0;i<SceneManager.sceneCount;i++)
            {
                var existing=SceneManager.GetSceneAt(i);
                if (existing.isDirty) throw new InvalidOperationException("Save or close modified scenes before Motion validation builds.");
            }
            string output=null;
            var args=Environment.GetCommandLineArgs();
            for (int i=0;i<args.Length-1;i++) if (args[i]=="-motionOutput") output=Path.GetFullPath(args[i+1]);
            if (output==null) throw new ArgumentException("Pass -motionOutput with an explicit validation artifact path.");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            string scenePath="Assets/MotionNativeValidation-"+Guid.NewGuid().ToString("N")+".unity";
            var scenes=EditorSceneManager.GetSceneManagerSetup();
            var named=android ? NamedBuildTarget.Android : NamedBuildTarget.Standalone;
            var backend=PlayerSettings.GetScriptingBackend(named);
            var architecture=PlayerSettings.Android.targetArchitectures;
            var minimum=PlayerSettings.Android.minSdkVersion;
            var targetSdk=PlayerSettings.Android.targetSdkVersion;
            bool bundle=EditorUserBuildSettings.buildAppBundle;
            bool exportProject=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            var plugin=(PluginImporter)AssetImporter.GetAtPath("Packages/com.yu5h1.motion/Plugins/Android/arm64-v8a/libMotionNative.so");
            bool androidEnabled=plugin.GetCompatibleWithPlatform(BuildTarget.Android);
            try
            {
                if (android)
                {
                    plugin.SetCompatibleWithPlatform(BuildTarget.Android,true); plugin.SaveAndReimport();
                    PlayerSettings.SetScriptingBackend(named,ScriptingImplementation.IL2CPP);
                    PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
                    PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
                    PlayerSettings.Android.targetSdkVersion=(AndroidSdkVersions)36;
                    EditorUserBuildSettings.buildAppBundle=false;
                    EditorUserBuildSettings.exportAsGoogleAndroidProject=false;
                }
                else PlayerSettings.SetScriptingBackend(named,ScriptingImplementation.Mono2x);
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var runner=new GameObject("Motion numerical validation").AddComponent<MotionValidationPlayer>();
                var manifest=AssetDatabase.LoadAssetAtPath<TextAsset>("Packages/com.yu5h1.motion/delivery.json");
                using (var sha=SHA256.Create()) runner.deliverySha256=BitConverter.ToString(sha.ComputeHash(manifest.bytes)).Replace("-","").ToLowerInvariant();
                EditorSceneManager.SaveScene(scene,scenePath);
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes=new[]{scenePath},locationPathName=output,target=android ? BuildTarget.Android : BuildTarget.StandaloneWindows64,
                    options=BuildOptions.Development|BuildOptions.IncludeTestAssemblies
                });
                if (report.summary.result!=BuildResult.Succeeded) throw new InvalidOperationException("Motion validation build failed: "+report.summary.result);
                Debug.Log("Motion validation build succeeded: "+output);
            }
            finally
            {
                PlayerSettings.SetScriptingBackend(named,backend);
                PlayerSettings.Android.targetArchitectures=architecture;
                PlayerSettings.Android.minSdkVersion=minimum; PlayerSettings.Android.targetSdkVersion=targetSdk;
                EditorUserBuildSettings.buildAppBundle=bundle;
                EditorUserBuildSettings.exportAsGoogleAndroidProject=exportProject;
                plugin.SetCompatibleWithPlatform(BuildTarget.Android,androidEnabled); plugin.SaveAndReimport();
                EditorSceneManager.RestoreSceneManagerSetup(scenes);
                AssetDatabase.DeleteAsset(scenePath);
            }
        }
    }
}
