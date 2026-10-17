using System;
using System.IO;
using UnityEngine;

namespace Yu5h1Lib.UIToolkit.Acceptance
{
    [Serializable]
    public class UxAcceptanceTargetData
    {
        public string targetId = "optionpicker";
        public string displayName = "UX OptionPicker (3.0.2)";
        public string moduleSpec = "Yu5h1Lib.UX/OptionPicker";
    }

    /// <summary>
    /// Resolves the durable agent-editable acceptance target record.
    /// Follows referenced-module-acceptance § 3: "Maintain one durable, agent-editable record of the current entry ID."
    /// </summary>
    public static class UxAcceptanceTarget
    {
        private const string ResourcePath = "Yu5h1Lib.UIToolkit/UxAcceptanceTarget";

        public static string ResolveCurrentTargetId()
        {
            // 1. Check direct file in Samples / project if available
            try
            {
                string[] candidatePaths = new[]
                {
                    Path.Combine(Application.dataPath, "Samples/Yu5h1Lib.UIToolkit/0.1.0/UxAcceptance/CurrentTarget.json"),
                    Path.Combine(Application.dataPath, "../Packages/UIToolkit/Samples~/UxAcceptance/CurrentTarget.json"),
                    Path.Combine(Application.dataPath, "../Packages/com.yu5h1.uitoolkit/Samples~/UxAcceptance/CurrentTarget.json")
                };

                for (int i = 0; i < candidatePaths.Length; i++)
                {
                    string fullPath = Path.GetFullPath(candidatePaths[i]);
                    if (File.Exists(fullPath))
                    {
                        string json = File.ReadAllText(fullPath);
                        var data = JsonUtility.FromJson<UxAcceptanceTargetData>(json);
                        if (data != null && !string.IsNullOrEmpty(data.targetId))
                        {
                            return data.targetId.Trim();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UxAcceptanceTarget] Failed reading disk file: {ex.Message}");
            }

            // 2. Check Resources
            var textAsset = Resources.Load<TextAsset>(ResourcePath);
            if (textAsset != null && !string.IsNullOrEmpty(textAsset.text))
            {
                try
                {
                    var data = JsonUtility.FromJson<UxAcceptanceTargetData>(textAsset.text);
                    if (data != null && !string.IsNullOrEmpty(data.targetId))
                    {
                        return data.targetId.Trim();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[UxAcceptanceTarget] Failed parsing Resource: {ex.Message}");
                }
            }

            // 3. Fallback default
            return "optionpicker";
        }
    }
}
