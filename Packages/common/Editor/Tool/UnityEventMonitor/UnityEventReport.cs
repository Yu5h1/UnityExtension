// Batchmode entry for the UnityEvent monitor: scans scenes and assets without showing the editor
// and writes the records as JSON.
//
//   Unity.exe -batchmode -nographics -projectPath <project> -logFile <log>
//     -executeMethod Yu5h1Lib.EditorExtension.UnityEventReport.Run
//     -ueScenes "Assets/Scene/A.unity;Assets/Scene/B.unity"
//     -ueAssets "Assets/Prefabs;Assets/Data/Some.asset"
//     -ueOut "<path>.json"
//     [-ueBrokenOnly]
//
// Exit code: 0 no broken call, 1 broken calls found, 2 the scan itself failed.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    public static class UnityEventReport
    {
        [Serializable]
        private class Row
        {
            public string status;
            public string asset;
            public string owner;
            public string eventPath;
            public int index;
            public string target;
            public string targetType;
            public string method;
            public string mode;
            public string argument;
            public string callState;
            public string detail;
        }

        [Serializable]
        private class Document
        {
            public string generatedAt;
            public string unityVersion;
            public int total;
            public int broken;
            public List<Row> calls = new();
        }

        public static void Run()
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                var scenes = SplitArgument(args, "-ueScenes");
                var assets = SplitArgument(args, "-ueAssets");
                var output = GetArgument(args, "-ueOut") ?? "UnityEventReport.json";
                var records = new List<UnityEventRecord>();
                foreach (var scenePath in scenes)
                    UnityEventScanner.ScanScene(EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single), records);
                if (assets.Length > 0)
                    records.AddRange(UnityEventScanner.ScanAssets(assets));
                var broken = Write(records, output, args.Contains("-ueBrokenOnly"));
                Debug.Log($"[UnityEventReport] {records.Count} calls, {broken} broken -> {Path.GetFullPath(output)}");
                EditorApplication.Exit(broken > 0 ? 1 : 0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(2);
            }
        }

        /// <summary>
        /// Writes records as JSON and returns how many are broken. Shared by the batchmode entry and the viewer's export.
        /// </summary>
        public static int Write(IReadOnlyCollection<UnityEventRecord> records, string path, bool brokenOnly = false)
        {
            var doc = new Document
            {
                generatedAt = DateTime.Now.ToString("o"),
                unityVersion = Application.unityVersion,
                total = records.Count,
                broken = records.Count(r => r.IsBroken),
            };
            foreach (var r in records.OrderBy(r => r.status))
            {
                if (brokenOnly && !r.IsBroken)
                    continue;
                doc.calls.Add(new Row
                {
                    status = r.status.ToString(),
                    asset = r.assetPath,
                    owner = r.ownerPath,
                    eventPath = r.eventPath,
                    index = r.index,
                    target = r.targetPath,
                    targetType = r.targetType,
                    method = r.methodName,
                    mode = r.mode.ToString(),
                    argument = r.argument,
                    callState = r.callState.ToString(),
                    detail = r.detail,
                });
            }
            var directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!directory.IsEmpty())
                Directory.CreateDirectory(directory);
            File.WriteAllText(path, JsonUtility.ToJson(doc, true), new System.Text.UTF8Encoding(false));
            return doc.broken;
        }

        private static string GetArgument(string[] args, string name)
        {
            var i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        private static string[] SplitArgument(string[] args, string name) =>
            (GetArgument(args, name) ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
    }
}
