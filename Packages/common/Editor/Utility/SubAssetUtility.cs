using UnityEngine;
using UnityEditor;

namespace Yu5h1Lib.EditorExtension
{
    public static class SubAssetUtility
    {
        public static Object GetMainAsset(Object subAsset)
        {
            var path = AssetDatabase.GetAssetPath(subAsset);
            return AssetDatabase.LoadMainAssetAtPath(path);
        }

        public static ParameterObject CreateParameterSubAsset(System.Type valueType, Object mainAsset, string name)
        {
            var poType = ParameterObjectUtility.GetParameterObjectType(valueType);
            if (poType == null) return null;

            var instance = (ParameterObject)ScriptableObject.CreateInstance(poType);
            instance.name = name;
            Undo.RegisterCreatedObjectUndo(instance, "Create ParameterObject SubAsset");
            AssetDatabase.AddObjectToAsset(instance, mainAsset);
            EditorUtility.SetDirty(mainAsset);
            AssetDatabase.SaveAssets();
            return instance;
        }

        public static void RemoveSubAsset(ScriptableObject subAsset)
        {
            if (subAsset == null) return;
            Undo.DestroyObjectImmediate(subAsset);
            AssetDatabase.SaveAssets();
        }
    }
}
