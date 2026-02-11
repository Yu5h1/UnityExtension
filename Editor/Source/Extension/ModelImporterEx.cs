using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

namespace Yu5h1Lib.EditorExtension
{
    public static class ModelImporterEx
    {
        [MenuItem("CONTEXT/ModelImporter/Extract Clip/Take001 or First (As File Name)")]
        static void ExtractTake001OrFirstAsFileName(MenuCommand command)
        {

            var target = (ModelImporter)command.context;
            string modelPath = AssetDatabase.GetAssetPath(target);
            var clips = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>();
            if (clips.IsEmpty())
            {
                "No animation clips found in the model.".printWarning();
                return;
            }
            if (!clips.TryGet(clip => clip.name.Equals("Take 001") , out AnimationClip sourceClip))
            {
                "Take 001 was not found ! Extract first Clip instead.".print();
                sourceClip = clips.First();
            }
            AssetDatabase.CreateAsset(AnimationClip.Instantiate(sourceClip), PathInfo.ChangeExtension(modelPath, ".anim"));
        }
    } 
}