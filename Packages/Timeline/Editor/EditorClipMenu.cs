using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// Context-menu entry on a Timeline clip's Inspector (EditorClip) that renames the
    /// embedded AnimationClip sub-asset to match the clip's display name. Makes the
    /// sub-assets inside a .playable identifiable — you can tell which AnimationClip
    /// belongs to which clip.
    /// </summary>
    public static class EditorClipMenu
    {
        [MenuItem("CONTEXT/EditorClip/Rename Sub-Asset To Clip Name")]
        private static void RenameSubAssetToClipName(MenuCommand command)
        {
            // EditorClip (UnityEditor.Timeline) is internal — reach its TimelineClip via reflection.
            var context = command.context;
            var clipProp = context.GetType().GetProperty(
                "clip", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (clipProp?.GetValue(context) is not TimelineClip timelineClip)
            {
                Debug.LogWarning($"{nameof(EditorClipMenu)}: could not resolve TimelineClip from '{context?.GetType().Name}'.");
                return;
            }

            if (timelineClip.asset is not AnimationPlayableAsset playableAsset || playableAsset.clip == null)
            {
                Debug.LogWarning($"{nameof(EditorClipMenu)}: clip '{timelineClip.displayName}' has no AnimationClip sub-asset to rename.");
                return;
            }

            var animationClip = playableAsset.clip;
            animationClip.name = timelineClip.displayName;
            EditorUtility.SetDirty(animationClip);
            AssetDatabase.SaveAssets();
            Debug.Log($"{nameof(EditorClipMenu)}: AnimationClip sub-asset renamed → '{animationClip.name}'.", animationClip);
        }
    }
}
