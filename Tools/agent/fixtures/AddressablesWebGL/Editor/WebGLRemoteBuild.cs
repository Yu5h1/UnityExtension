// Builds only the probe scene. The caller backs up ProjectSettings before starting Unity and restores them on exit.
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using Yu5h1Lib.EditorExtension;

public static class WebGLRemoteBuild
{
    public static void Run()
    {
        var exit = 1;
        try
        {
            var output = Environment.GetEnvironmentVariable("ADDRESSABLES_WEBGL_OUT");
            if (string.IsNullOrEmpty(output))
                throw new ArgumentException("ADDRESSABLES_WEBGL_OUT must name the isolated build directory");
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.template = "APPLICATION:Minimal";
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
            const string scenePath = "Assets/AddressablesTest/WebGLRemoteTest.unity";
            if (!File.Exists(scenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, scenePath);
            }
            var error = RemoteTestSetup.Apply();
            if (!string.IsNullOrEmpty(error))
                throw new Exception(error);
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
            error = AddressablesContentBuild.Build();
            if (!string.IsNullOrEmpty(error))
                throw new Exception(error);
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { scenePath }, locationPathName = output,
                target = BuildTarget.WebGL, options = BuildOptions.Development,
            });
            if (result.summary.result != BuildResult.Succeeded)
                throw new Exception("WebGL build: " + result.summary.result);
            Debug.Log("[WebGLRemoteBuild] PASS " + output);
            exit = 0;
        }
        catch (Exception error)
        {
            Debug.LogException(error);
        }
        EditorApplication.Exit(exit);
    }
}
