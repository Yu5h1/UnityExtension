using UnityEngine;
using UnityEditor;
using System.IO;

namespace Yu5h1Lib.EditorExtension
{
    public static class ModelImporterEx
    {
        [MenuItem("CONTEXT/ModelImporter/Import by Yu5h1Tools default setting")]
        static void ImportByYu5h1ToolsDefaultSetting(MenuCommand command)
        {
            var target = (ModelImporter)command.context;
            Yu5h1LibPreference.ImportProcess.ImportByYu5h1ToolsDefaultSetting(target);
            
        }

        [MenuItem("CONTEXT/ModelImporter/Extract Take001 by FileName")]
        static void ExtractTake001ByFileName(MenuCommand command)
        {

            var target = (ModelImporter)command.context;
            string modelPath = AssetDatabase.GetAssetPath(target);

            if (AssetDatabase.LoadAllAssetsAtPath(modelPath).TryGet(obj => (obj is AnimationClip) && obj.name.Equals("Take 001")
            , out Object defaultTake))
            {
                AssetDatabase.CreateAsset(Object.Instantiate(defaultTake), PathInfo.ChangeExtension(modelPath, ".anim"));
            }
            else
                "Take 001 was not found !".printWarning();
        }
    } 
}