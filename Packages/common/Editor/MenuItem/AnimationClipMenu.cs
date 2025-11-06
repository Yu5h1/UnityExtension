using UnityEditor;
using UnityEngine;
using System.Linq;

public static class AnimationClipMenu
{
    [MenuItem("CONTEXT/AnimationClip/Remove Position and Scale Curves")]
    private static void RemoveRotationAndScale()
    {
        var clips = Selection.objects.OfType<AnimationClip>().ToArray();
        if (clips.Length == 0)
        {
            Debug.LogWarning("?? No AnimationClip selected.");
            return;
        }

        foreach (var clip in clips)
        {
            RemoveCurves(clip);
            EditorUtility.SetDirty(clip);
            Debug.Log($"? Removed rotation & scale curves from: {clip.name}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void RemoveCurves(AnimationClip clip)
    {
        var bindings = AnimationUtility.GetCurveBindings(clip);
        foreach (var binding in bindings)
        {
            if (IsPosition(binding) || IsScale(binding))
                AnimationUtility.SetEditorCurve(clip, binding, null);
        }
    }

    private static bool IsPosition(EditorCurveBinding b)
    {
        // °w¹ï Quaternion rotation
        return b.propertyName.StartsWith("m_LocalPosition");
    }

    private static bool IsScale(EditorCurveBinding b)
    {
        return b.propertyName.StartsWith("m_LocalScale");
    }
}
