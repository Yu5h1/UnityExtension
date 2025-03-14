using UnityEngine;
using UnityEditor;
using System.IO;

namespace Yu5h1Lib.EditorExtension
{
    public static class ScriptableObjectUtil
    {        
        public static T CreateScriptableObject<T>(string folderPath, string fileName) where T : ScriptableObject
        {
            return (T)CreateScriptableObject(ScriptableObject.CreateInstance<T>(), folderPath, fileName);
        }
        public static void CreateScriptableObject(System.Type type, string path = "")
        {
            var obj = ScriptableObject.CreateInstance(type);
            if (obj == null)
            {
                Debug.LogError("Fail Create ScriptableObject " + type.FullName);
            }
            else
            {
                CreateScriptableObject(obj, path);
            }
        }
        public static Object CreateScriptableObject(Object newScriptableObject, string path = "", string newName = "")
        {

            if (path == "") path = ProjectBrowserEx.SelectedFolderPath;
            if (newName == "") newName = "New " + newScriptableObject.GetType().Name;

            if (path.Contains(Application.dataPath)) path = path.Remove(0, Path.GetDirectoryName(Application.dataPath).Length + 1);

            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + newName + ".asset");

            AssetDatabase.CreateAsset(newScriptableObject, assetPathAndName);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ProjectWindowUtil.ShowCreatedAsset(newScriptableObject);
            return newScriptableObject;
        }
    }
}