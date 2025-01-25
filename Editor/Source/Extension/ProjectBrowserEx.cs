using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    public static class ProjectBrowserEx
    {
        public static System.Type InternalType { get { return typeof(Editor).Assembly.GetType("UnityEditor.ProjectBrowser"); ; } }
        public static object ProjectBrowserWindowCache;
        public static object ProjectBrowserWindow {
            get {
                if (ProjectBrowserWindowCache == null) {
                    ProjectBrowserWindowCache = EditorWindowUtil.GetExistsWindow(InternalType);
                }
                return ProjectBrowserWindowCache;
            }
        }
        public static string? SelectedFolderPath
        => ProjectBrowserWindow == null ? null : 
            InternalType.GetMethod("GetActiveFolderPath",
                    BindingFlags.NonPublic | BindingFlags.Instance).Invoke(ProjectBrowserWindow, null) as string;

        public static string SelectedFolderFullPath { get { return Path.GetFullPath(SelectedFolderPath); } }
        public static Object SelectedFolder
            => AssetDatabase.LoadAssetAtPath(SelectedFolderPath, typeof(Object));
        public static void SelectEditorFolder()
        {
            string folder = ProjectBrowserEx.SelectedFolderPath ?? "";

            if (!folder.EndsWith("Editor"))
            {
                var folderEditor = PathInfo.Combine(folder, "Editor");
                var parentEditor = PathInfo.Combine(PathInfo.GetDirectory(folder), "Editor");
                if (PathInfo.Exists(folderEditor))
                    folder = folderEditor;
                else if (PathInfo.Exists(parentEditor))
                    folder = parentEditor;
                else if (folder.StartsWith("Packages"))
                    folder = PathInfo.Combine(folder.TrimAfter("/", true, occurrence: 2), "Editor");
                else
                    folder = AssetPathInfo.CreatePath("Editor");
            }
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                AssetDatabase.ImportAsset(folder);
            }
            Selection.activeObject = AssetDatabase.LoadAssetAtPath(folder, typeof(Object));
        }

        
    }
}
