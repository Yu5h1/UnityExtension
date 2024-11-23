using System.ComponentModel;
using UnityEditor;
using UnityEngine;


namespace Yu5h1Lib.EditorExtension
{
    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public static class AnimationClipEx
    {
        private static AnimationClipSettings settingCache;

        [MenuItem("CONTEXT/AnimationClip/Copy Clip Setting")]
        public static void CopyClipSetting(MenuCommand command)
        { 
            AnimationClip clip = (AnimationClip)command.context;
            settingCache = AnimationUtility.GetAnimationClipSettings(clip);
        }
        [MenuItem("CONTEXT/AnimationClip/Paste Clip Setting")]
        public static void PasteClipSetting(MenuCommand command)
        {
            AnimationClip clip = (AnimationClip)command.context;
            AnimationUtility.SetAnimationClipSettings(clip, settingCache);
        }
    }
}
