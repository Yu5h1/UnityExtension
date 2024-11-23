using System;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEditor;

namespace Yu5h1Lib.EditorExtension
{
    public static class ProjectBrowserEx
    {
        public static Type InternalType { get { return typeof(Editor).Assembly.GetType("UnityEditor.ProjectBrowser"); ; } }
        public static object ProjectBrowserWindowCache;
        public static object ProjectBrowserWindow {
            get {
                if (ProjectBrowserWindowCache == null) {
                    ProjectBrowserWindowCache = EditorWindowUtil.GetExistsWindow(InternalType);
                }
                return ProjectBrowserWindowCache;
            }
        }
        public static string SelectedFolderPath
        {
            get {
                return InternalType.GetMethod("GetActiveFolderPath",
                    BindingFlags.NonPublic | BindingFlags.Instance).Invoke(ProjectBrowserWindow, null) as string;
            }
        }
        public static string SelectedFolderFullPath { get { return Path.GetFullPath(SelectedFolderPath); } }
        public static Object SelectedFolder
        { get { return AssetDatabase.LoadAssetAtPath(SelectedFolderPath, typeof(Object)); } }
        public static void SelectEditorFolder()
        {
            var folder = ProjectBrowserEx.SelectedFolderPath;            
            if (!folder.Contains("Editor"))
            {
                folder = folder + @"\Editor";
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                    AssetDatabase.ImportAsset(folder);
                }
                Selection.activeObject = AssetDatabase.LoadAssetAtPath(folder, typeof(Object));
            }
        }
    }
}
