using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Yu5h1Lib.EditorExtension
{
    public static class AssetDatabaseEx
    {
        public static string AssemblyLocation => Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location); 

        public static string ProjectSettingLocation => Path.Combine(Path.GetDirectoryName(Application.dataPath), "ProjectSettings"); 

        public static bool IsFocuseProjectWindow => EditorWindow.focusedWindow.GetType() == ProjectBrowserEx.InternalType; 

        #region Public Methods
        public static string RemoveInvalidFileNameChars(string fileName)
        {            
            foreach (var item in Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(new string(new char[] { item }), "");
            return fileName;
        }
        public static string CorrectAssetsPath(string path) { return path.Replace("/", "\\"); }
        public static string ConvertToAssetDataPath(string fullPath)
        { return fullPath.Replace(CorrectAssetsPath(Path.GetDirectoryName(Application.dataPath)) + "\\", ""); }

        public static void PingObject(string filePath)
        {
            var target = AssetDatabase.LoadAssetAtPath(filePath, typeof(Object));
            ProjectWindowUtil.ShowCreatedAsset(target);
            EditorGUIUtility.PingObject(target);
        }
        #endregion


        /// <summary>
        /// 0 replace , 1 ignore , 2 replace all , 3 ignore all, 4 Cancel
        /// </summary>
        static int DetectFilesExistsPrompt(string message)
        {
            int result = EditorUtility.DisplayDialogComplex("The file already exists", "The file : \n\n" + message + "\n\n already exists.\n\nDo you want to replace it?", "Replace", "Ignore", "More options");
            if (result == 2)
            {
                result += EditorUtility.DisplayDialogComplex("More options", "Options of process all file", "Replace all", "Ignore all", "Cancel");
            }
            return result;
        }
        static bool DetectFileExistsPrompt(string destFilePath)
        {
            bool result = true;
            if (File.Exists(destFilePath) || Directory.Exists(destFilePath))
            {
                result = EditorUtility.DisplayDialog("The file already exists", "The file already exists. Do you want to replace it?", "Replace", "Cancel");
            }

            return result;
        }
        public static List<Object> LoadAllAssetsAtPath(string assetPath,List<System.Type> specificTypes)
        {
            List<Object> results = new List<Object>();
            var searchResults = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var obj in searchResults)
            {
                bool enableAdd = specificTypes == null || specificTypes.Count == 0;
                if (!enableAdd == false) enableAdd = specificTypes!.Exists(d => d == obj.GetType());                
                if (enableAdd) results.Add(obj);
            }
            return results;
        }
        public static List<Object> LoadAllAssetsAtPath(string assetPath,params System.Type[] specificTypes)
        { return LoadAllAssetsAtPath(assetPath, specificTypes.ToList<System.Type>()); }

    }
}