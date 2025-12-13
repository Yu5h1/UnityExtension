using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace Yu5h1Lib.EditorExtension
{
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public class CICD_PreBuildProcess : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public static bool Add_version_before_build
        {
            get => EditorPrefs.GetBool(nameof(Add_version_before_build), false);
            set => EditorPrefs.SetBool(nameof(Add_version_before_build), value);
        }
        public static bool Auto_add_BundleVersionCode
        {
            get => EditorPrefs.GetBool(nameof(Auto_add_BundleVersionCode), false);
            set => EditorPrefs.SetBool(nameof(Auto_add_BundleVersionCode), value);
        }
        [InitializeOnLoadMethod]
        private static void Register()
        {
            EditorPreferences.AddDrawer("CI/CD", () =>
            {
                Add_version_before_build = EditorGUILayout.ToggleLeft(nameof(Add_version_before_build).Replace("_", " "), Add_version_before_build);
            });
            EditorPreferences.AddDrawer("Android", () =>
            {
                Auto_add_BundleVersionCode = EditorGUILayout.ToggleLeft(nameof(Auto_add_BundleVersionCode).Replace("_", " "), Auto_add_BundleVersionCode);
            });
        }

        static string previouse_version;
        static int? previouse_bundleVersionCode;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (Add_version_before_build)
            {
                previouse_version = PlayerSettings.bundleVersion;
                PlayerSettings.bundleVersion = IncrementVersion(PlayerSettings.bundleVersion);
                $"[CICD_PreBuildProcess] Auto increment version to {PlayerSettings.bundleVersion}".print();
            }

            if (report.summary.platform == BuildTarget.Android)
            {
                if (Auto_add_BundleVersionCode)
                {
                    previouse_bundleVersionCode = PlayerSettings.Android.bundleVersionCode;
                    PlayerSettings.Android.bundleVersionCode++;
                    $"Auto add bundleVersionCode:{PlayerSettings.Android.bundleVersionCode}".print();
                }
            }

        }
        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.result != BuildResult.Succeeded)
            {
                if (!previouse_version.IsEmpty())
                {
                    PlayerSettings.bundleVersion = previouse_version;
                    $"Revert bundleVersion:{PlayerSettings.bundleVersion} because build {report.summary.result} ".printWarning();
                }
                if (previouse_bundleVersionCode.HasValue)
                {
                    PlayerSettings.Android.bundleVersionCode = previouse_bundleVersionCode.Value;
                    $"Revert bundleVersionCode:{PlayerSettings.Android.bundleVersionCode} because build {report.summary.result} ".printWarning();

                }
            }
            previouse_version = null;
            previouse_bundleVersionCode = null;
        }
        public static string IncrementVersion(string versionString)
        {
            string versionStr = versionString;

            var parts = versionStr.Split('.');
            int.TryParse(parts.Length > 0 ? parts[0] : "0", out int major);
            int.TryParse(parts.Length > 1 ? parts[1] : "0", out int minor);
            int.TryParse(parts.Length > 2 ? parts[2] : "0", out int patch);

            patch++;
            if (patch >= 1000) { patch = 0; minor++; }
            if (minor >= 100) { minor = 0; major++; }

            return $"{major}.{minor}.{patch}";
        }
    }
}