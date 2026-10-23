// Build-time half of the agent translator: during a player build it runs ObjectExport over every scene Unity processes and
// compares the scenes' wiring — each object reference field's value, each prefab instance's source, each UnityEvent
// listener with its status, each missing script — with the previous build for the same platform, so a connection that
// broke since then shows up as a build warning. It never fails the build. Switched off in
// Preferences > Yu5h1Lib Settings > CI/CD.
// An incremental build skips scenes that did not change; their lines are carried over from the previous build, and lines
// of scenes no longer in the build are dropped. Results stay in <project>/Library/Yu5h1Lib/BuildWiring/<platform>/:
// export.json (ObjectExport of the scenes processed by the latest build that processed any), wiring.txt (one sorted line
// per connection of every scene in the build) and wiring.previous.txt.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using Field = Yu5h1Lib.EditorExtension.InlineObjectExport.Field;
using EventEntry = Yu5h1Lib.EditorExtension.InlineObjectExport.EventEntry;

namespace Yu5h1Lib.EditorExtension
{
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public class WiringBuildCheck : BuildPlayerProcessor, IPreprocessBuildWithReport, IProcessSceneWithReport, IPostprocessBuildWithReport
    {
        private const string Tag = "[WiringBuildCheck]";
        private const string Separator = " | ";
        private const int ListedLines = 50;

        private static ObjectExport.Document document;
        private static readonly HashSet<string> processedScenes = new();
        private static string[] buildScenes;

        public override int callbackOrder => int.MaxValue;

        public static bool Wiring_report_on_build
        {
            get => EditorPrefs.GetBool(nameof(Wiring_report_on_build), true);
            set => EditorPrefs.SetBool(nameof(Wiring_report_on_build), value);
        }

        [InitializeOnLoadMethod]
        private static void Register() => EditorPreferences.AddDrawer("CI/CD", () =>
            Wiring_report_on_build = EditorGUILayout.ToggleLeft(nameof(Wiring_report_on_build).Replace("_", " "), Wiring_report_on_build));

        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext) =>
            buildScenes = buildPlayerContext.BuildPlayerOptions.scenes;

        public void OnPreprocessBuild(BuildReport report)
        {
            document = Wiring_report_on_build ? new ObjectExport.Document { unityVersion = Application.unityVersion } : null;
            processedScenes.Clear();
        }

        /// <summary>
        /// Unity also calls this when entering Play Mode, with no report; only player builds are scanned.
        /// </summary>
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (report == null || document == null)
                return;
            try
            {
                ObjectExport.AppendScene(document, scene);
                processedScenes.Add(scene.path);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{Tag} {scene.path} was not scanned: {e}");
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            var doc = document;
            var scenes = buildScenes;
            document = null;
            buildScenes = null;
            if (doc == null)
                return;
            try
            {
                Compare(doc, report.summary.platform.ToString(), processedScenes, scenes);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{Tag} the wiring comparison failed: {e}");
            }
        }

        /// <summary>
        /// Writes this build's wiring, carrying over the previous lines of build scenes Unity did not process, then logs what
        /// changed since the previous wiring file of the platform; a warning when a connection broke since then.
        /// <paramref name="scenesInBuild"/> null keeps every previous scene that was not processed.
        /// </summary>
        internal static void Compare(ObjectExport.Document doc, string platform, ICollection<string> processed, IList<string> scenesInBuild)
        {
            var folder = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Library", "Yu5h1Lib", "BuildWiring", platform);
            var currentPath = Path.Combine(folder, "wiring.txt");
            var previousPath = Path.Combine(folder, "wiring.previous.txt");
            var previous = File.Exists(currentPath) ? File.ReadAllLines(currentPath, Encoding.UTF8) : null;
            if (previous == null && processed.Count == 0)
                return;

            var carried = (previous ?? Array.Empty<string>()).Where(line =>
            {
                var scene = SceneOf(line);
                return !processed.Contains(scene) && (scenesInBuild == null || scenesInBuild.Contains(scene));
            });
            var wiring = WiringOf(doc).Concat(carried).OrderBy(line => line, StringComparer.Ordinal).ToList();

            Directory.CreateDirectory(folder);
            if (processed.Count > 0)
                InlineObjectExport.Write(doc, Path.Combine(folder, "export.json"));
            if (previous != null)
                File.Copy(currentPath, previousPath, true);
            File.WriteAllLines(currentPath, wiring, new UTF8Encoding(false));

            var summary = $"{Tag} {platform}: {processed.Count} scenes scanned, {wiring.Count} connections, {wiring.Count(IsBroken)} broken";
            if (previous == null)
            {
                Debug.Log($"{summary}; no previous build to compare with. Wiring: {currentPath}");
                return;
            }
            var before = new HashSet<string>(previous, StringComparer.Ordinal);
            var after = new HashSet<string>(wiring, StringComparer.Ordinal);
            var added = wiring.Where(line => !before.Contains(line)).ToList();
            var removed = before.Count(line => !after.Contains(line));
            var newlyBroken = added.Where(IsBroken).ToList();
            summary += $"; since the previous build {added.Count} added, {removed} removed.";
            var files = $"Previous: {previousPath}\nCurrent: {currentPath}";
            if (newlyBroken.Count == 0)
            {
                Debug.Log($"{summary}\n{files}");
                return;
            }
            var listed = string.Join("\n", newlyBroken.Take(ListedLines));
            var more = newlyBroken.Count > ListedLines ? $"\n… {newlyBroken.Count - ListedLines} more in the current file" : string.Empty;
            Debug.LogWarning($"{summary} {newlyBroken.Count} newly broken:\n{listed}{more}\n{files}");
        }

        /// <summary>
        /// One line per connection, each starting with its scene or asset path: "&lt;asset&gt; | &lt;owner&gt; | &lt;field&gt; = &lt;value&gt;"
        /// for object references and prefab sources, "… | &lt;event&gt;[i] -&gt; &lt;target&gt; .&lt;method&gt; (&lt;mode&gt; &lt;argument&gt;) &lt;status&gt;"
        /// for listeners, "… | missing script" for components whose script is gone.
        /// </summary>
        internal static List<string> WiringOf(ObjectExport.Document doc)
        {
            var wiring = new List<string>();
            foreach (var go in doc.gameObjects)
            {
                if (!go.prefab.IsEmpty())
                    wiring.Add($"{go.asset}{Separator}{go.path}{Separator}prefab = {go.prefab}");
                foreach (var component in go.components)
                {
                    var owner = $"{go.asset}{Separator}{go.path} ({component.id})";
                    if (component.type == ObjectExport.MissingScript)
                        wiring.Add($"{owner}{Separator}{MissingScriptLine}");
                    AddConnections(wiring, owner, component.fields, component.events);
                }
            }
            foreach (var asset in doc.assets)
                AddConnections(wiring, asset.asset, asset.fields, asset.events);
            foreach (var inline in doc.inlineObjects)
                AddConnections(wiring, $"{inline.asset}{Separator}inline: {inline.holder} > {inline.field}", inline.fields, inline.events);
            return wiring.Select(line => line.Replace("\r", "\\r").Replace("\n", "\\n")).ToList();
        }

        private const string MissingScriptLine = "missing script";

        private static void AddConnections(List<string> wiring, string owner, List<Field> fields, List<EventEntry> events)
        {
            foreach (var field in fields.Where(ObjectExport.IsReference))
                wiring.Add($"{owner}{Separator}{field.path} = {field.value}");
            foreach (var channel in events)
                foreach (var call in channel.calls)
                    wiring.Add($"{owner}{Separator}{channel.path}[{call.index}] -> {call.target} .{call.method} ({call.mode} {call.argument}) {call.status}");
        }

        private static string SceneOf(string line)
        {
            var end = line.IndexOf(Separator, StringComparison.Ordinal);
            return end < 0 ? line : line.Substring(0, end);
        }

        private static bool IsBroken(string line)
        {
            if (line.EndsWith($" = {InlineObjectExport.Missing}", StringComparison.Ordinal) || line.EndsWith(MissingScriptLine, StringComparison.Ordinal))
                return true;
            var status = line.Substring(line.LastIndexOf(' ') + 1);
            return line.Contains(" -> ") && Enum.TryParse(status, out PersistentCallStatus parsed) && UnityEventRecord.IsBrokenStatus(parsed);
        }
    }
}
