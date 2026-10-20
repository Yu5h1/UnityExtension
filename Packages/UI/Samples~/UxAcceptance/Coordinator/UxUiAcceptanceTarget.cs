using System;
using System.IO;
using UnityEngine;

namespace Yu5h1Lib.UI.Acceptance
{
    [Serializable]
    public class UxUiAcceptanceTargetData
    {
        public string targetId = "button";
        public string displayName = "UX Button (2.0.0)";
        public string moduleSpec = "Yu5h1Lib.UX/Button";
    }

    /// <summary>
    /// Resolves the durable agent-editable acceptance target record for Yu5h1Lib.UI.
    /// Kept within acceptance demo sample scope to keep production runtime DLL clean.
    /// </summary>
    public static class UxUiAcceptanceTarget
    {
        public static string ResolveCurrentTargetId()
        {
            try
            {
                string[] candidatePaths = new[]
                {
                    Path.Combine(Application.dataPath, "Samples/Yu5h1Lib.UI/1.0.0/UxAcceptance/CurrentTarget.json"),
                    Path.Combine(Application.dataPath, "../Packages/UI/Samples~/UxAcceptance/CurrentTarget.json"),
                    Path.Combine(Application.dataPath, "../Packages/com.yu5h1.ui/Samples~/UxAcceptance/CurrentTarget.json")
                };

                for (int i = 0; i < candidatePaths.Length; i++)
                {
                    string fullPath = Path.GetFullPath(candidatePaths[i]);
                    if (File.Exists(fullPath))
                    {
                        string json = File.ReadAllText(fullPath);
                        var data = JsonUtility.FromJson<UxUiAcceptanceTargetData>(json);
                        if (data != null && !string.IsNullOrEmpty(data.targetId))
                        {
                            return data.targetId.Trim();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UxUiAcceptanceTarget] Failed reading disk file: {ex.Message}");
            }

            return "button";
        }
    }
}
