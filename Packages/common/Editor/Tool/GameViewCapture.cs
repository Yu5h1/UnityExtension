// Tools > ScreenCapture > Capture (Ctrl+Shift+K) saves what the Game view shows as a PNG in <project>/Recordings/Screenshots.
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// Captures the Game view at once, without setting up a Recorder. Uses <see cref="ScreenCapture.CaptureScreenshot(string, int)"/>: the image
    /// is exactly what the Game view shows, screen-space UI included, at the Game view's resolution times the chosen scale. Set the Game view to a
    /// fixed resolution (for example 1920x1080) so the picture is not the window's shape. Works while playing and while paused; the Game view is
    /// repainted so the capture happens even when nothing else redraws it. The file goes to <c>Recordings/Screenshots</c> beside <c>Assets</c>
    /// and its path is logged; <c>Show in Explorer</c> reveals the last capture.
    /// </summary>
    internal static class GameViewCapture
    {
        private const string Root = "Tools/ScreenCapture/";
        private const string ScaleKey = "Yu5h1Lib.GameViewCapture.Scale";
        private const double WriteTimeout = 5.0;

        private static string _lastPath;

        private static int Scale
        {
            get => Mathf.Clamp(EditorPrefs.GetInt(ScaleKey, 1), 1, 4);
            set => EditorPrefs.SetInt(ScaleKey, value);
        }

        private static string Folder
        {
            get
            {
                string folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Recordings", "Screenshots");
                Directory.CreateDirectory(folder);
                return folder;
            }
        }

        [MenuItem(Root + "Capture %#k", priority = 0)]
        private static void Capture()
        {
            string path = Path.Combine(Folder, $"GameView-{DateTime.Now:yyyyMMdd-HHmmss-fff}.png");
            UnityEngine.ScreenCapture.CaptureScreenshot(path, Scale);
            _lastPath = path;
            RepaintGameViews();
            // The file is written at the end of the next rendered frame; keep repainting until it exists.
            double until = EditorApplication.timeSinceStartup + WriteTimeout;
            void WaitForFile()
            {
                if (File.Exists(path))
                {
                    EditorApplication.update -= WaitForFile;
                    Debug.Log($"Game view capture ({Scale}x) → {path}");
                }
                else if (EditorApplication.timeSinceStartup > until)
                {
                    EditorApplication.update -= WaitForFile;
                    Debug.LogWarning($"Game view capture: no file after {WriteTimeout} s. Open the Game view (it must be visible) and capture again.");
                }
                else
                    RepaintGameViews();
            }
            EditorApplication.update += WaitForFile;
        }

        /// <summary>Reveals the last capture of this session, or the screenshot folder when there is none.</summary>
        [MenuItem(Root + "Show in Explorer", priority = 1)]
        private static void ShowInExplorer()
        {
            if (!string.IsNullOrEmpty(_lastPath) && File.Exists(_lastPath))
                EditorUtility.RevealInFinder(_lastPath);
            else
                EditorUtility.RevealInFinder(Folder + Path.DirectorySeparatorChar);
        }

        private static void RepaintGameViews()
        {
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (type == null)
                return;
            foreach (var window in Resources.FindObjectsOfTypeAll(type))
                ((EditorWindow)window).Repaint();
        }

        [MenuItem(Root + "Scale 1x", priority = 20)] private static void Scale1() => Scale = 1;
        [MenuItem(Root + "Scale 2x", priority = 21)] private static void Scale2() => Scale = 2;
        [MenuItem(Root + "Scale 4x", priority = 22)] private static void Scale4() => Scale = 4;

        [MenuItem(Root + "Scale 1x", true)] private static bool Scale1Check() { UnityEditor.Menu.SetChecked(Root + "Scale 1x", Scale == 1); return true; }
        [MenuItem(Root + "Scale 2x", true)] private static bool Scale2Check() { UnityEditor.Menu.SetChecked(Root + "Scale 2x", Scale == 2); return true; }
        [MenuItem(Root + "Scale 4x", true)] private static bool Scale4Check() { UnityEditor.Menu.SetChecked(Root + "Scale 4x", Scale == 4); return true; }
    }
}
