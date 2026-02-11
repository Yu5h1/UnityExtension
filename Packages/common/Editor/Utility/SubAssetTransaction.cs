using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib
{
    public static class SubAssetTransaction
    {
        public static void Rename(Object subAsset, string newName)
            => Act("Rename SubAsset", subAsset, sub => sub.name = newName);
        public static void Remove(Object subAsset)
            => Act("Remove SubAsset", subAsset, Undo.DestroyObjectImmediate);

        public static void Act(string undoName, Object subAsset,System.Action<Object> action)
        {
            var path = AssetDatabase.GetAssetPath(subAsset);
            var main = AssetDatabase.LoadMainAssetAtPath(path);

            Undo.RegisterCompleteObjectUndo(main, undoName);

            AssetDatabase.StartAssetEditing();
            try
            {
                action.Invoke(subAsset);
                
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            if (subAsset != null)
                EditorUtility.SetDirty(subAsset);
            EditorUtility.SetDirty(main);

            //AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            UndoAdvanced.Delay(() =>
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

                var obj = Selection.activeObject;
                Selection.activeObject = null;
                Selection.activeObject = obj;
            });
        }
        public static Object GetMainAsset(Object subAsset)
        { 
            if (subAsset == null) return null;
            var path = AssetDatabase.GetAssetPath(subAsset);
            var main = AssetDatabase.LoadMainAssetAtPath(path);
            return main;
        }
    } 
}
