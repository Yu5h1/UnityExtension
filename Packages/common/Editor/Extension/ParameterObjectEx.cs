using System.ComponentModel;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Yu5h1Lib
{
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public static class ParameterObjectEx
	{
        public const string MENU_PATH = "CONTEXT/ParameterObject/";
        public const string Delete_PATH = MENU_PATH + "Delete";

        [MenuItem(Delete_PATH)]
        //[Shortcut(Delete_PATH, KeyCode.Delete)]
        private static void Delete(MenuCommand command)
        {
            ParameterObject obj = command.context as ParameterObject;

            if ($"Delete failed ! {obj.name} is not SubAsset.".printWarningIf(!AssetDatabase.IsSubAsset(obj)))
                return;

            if (!EditorUtility.DisplayDialog(
        "Delete SubAsset",
        $"Delete '{obj.name}'?",
        "Delete",
        "Cancel"))
                return;

            var parentPath = AssetDatabase.GetAssetPath(obj);
            var parent = AssetDatabase.LoadMainAssetAtPath(parentPath);

            Undo.DestroyObjectImmediate(obj);
            EditorUtility.SetDirty(parent);
            AssetDatabase.SaveAssets();

        }
    } 
}
