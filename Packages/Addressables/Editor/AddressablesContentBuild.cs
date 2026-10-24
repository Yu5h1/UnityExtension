// Builds the project's Addressables content without waiting on anyone, for agents (unityMCP, batchmode) as well as people.
// Addressables asks once per project, before its first content build in an interactive editor, whether to turn on
// 'Debug Build Layout'; that modal dialog stops the editor's main thread, and every unityMCP call with it, until someone
// answers. The build marks the question as answered first, leaving the setting itself as it is.
//
//   Unity.exe -batchmode -quit -projectPath <project> -logFile <log>
//     -executeMethod Yu5h1Lib.EditorExtension.AddressablesContentBuild.Run
//
// From code or unityMCP: AddressablesContentBuild.Build() returns the build error, empty on success.
// Exit code (Run): 0 built, 1 the build reported an error, 2 the project has no Addressables settings.
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    public static class AddressablesContentBuild
    {
        private const string Tag = "[AddressablesContentBuild]";

        /// <summary>
        /// Builds the content with the settings' active build script, as Groups > Build > New Build does, and logs each
        /// bundle with its group. Returns the error the build reported, empty on success.
        /// </summary>
        public static string Build()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return "the project has no Addressables settings (Assets/AddressableAssetsData)";
            MarkBuildReportQuestionAnswered();
            AddressableAssetSettings.BuildPlayerContent(out var result);
            if (!string.IsNullOrEmpty(result.Error))
            {
                Debug.LogError($"{Tag} {result.Error}");
                return result.Error;
            }
            foreach (var bundle in result.AssetBundleBuildResults)
                Debug.Log($"{Tag} {bundle.SourceAssetGroup?.Name}: {bundle.FilePath}");
            Debug.Log($"{Tag} built {result.AssetBundleBuildResults.Count} bundle(s) in {result.Duration:0.0} s, " +
                $"play mode script: {settings.ActivePlayModeDataBuilder?.Name}");
            return string.Empty;
        }

        public static void Run()
        {
            try
            {
                var error = Build();
                EditorApplication.Exit(error.Length == 0 ? 0 : AddressableAssetSettingsDefaultObject.Settings == null ? 2 : 1);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Addressables keeps whether it has asked about the build report in an internal flag
        /// (ProjectConfigData.UserHasBeenInformedAboutBuildReportSettingPreBuild, 2.8.1 to 2.11.2). When a later version
        /// renames it, the build still runs and the dialog may appear once.
        /// </summary>
        private static void MarkBuildReportQuestionAnswered()
        {
            var config = typeof(AddressableAssetSettings).Assembly.GetType("UnityEditor.AddressableAssets.Settings.ProjectConfigData");
            var flag = config?.GetProperty("UserHasBeenInformedAboutBuildReportSettingPreBuild",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (flag == null)
                Debug.LogWarning($"{Tag} cannot pre-answer the 'Debug Build Layout' question in this Addressables version; " +
                    "an interactive editor may show it once");
            else
                flag.SetValue(null, true);
        }
    }
}
