using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// Registers <see cref="AssetCreation.StartNameEditing"/> with the Project window rename callback of the running Unity version.
    /// Kept as source rather than in the Core DLL because the callback type changed across versions, and only Unity's own
    /// compilation applies the UNITY_x defines that pick the right one.
    /// </summary>
#if UNITY_6000_6_OR_NEWER
    internal class NamedAssetEndAction : AssetCreationEndAction
#else
    internal class NamedAssetEndAction : EndNameEditAction
#endif
    {
        private System.Func<string, string, string> _replaceContent;

#if UNITY_6000_6_OR_NEWER
        public override void Action(EntityId entityId, string path, string contents)
#else
        public override void Action(int instanceId, string path, string contents)
#endif
            => AssetCreation.Complete(path, contents, _replaceContent);

        [InitializeOnLoadMethod]
        private static void Register() => AssetCreation.StartNameEditing = StartNameEditing;

        private static void StartNameEditing(string fileName, string contents, Texture2D icon, System.Func<string, string, string> replaceContent)
        {
            var action = CreateInstance<NamedAssetEndAction>();
            action._replaceContent = replaceContent;
#if UNITY_6000_6_OR_NEWER
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(EntityId.None, action, fileName, icon, contents);
#else
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, action, fileName, icon, contents);
#endif
        }
    }
}
