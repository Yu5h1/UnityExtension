using UnityEditor;
using Yu5h1Lib.EditorExtension;

namespace Yu5h1Lib.UI
{
    /// <summary>Exposes ScrollView options together with its inherited ScrollRect settings.</summary>
    [CustomEditor(typeof(ScrollView)), CanEditMultipleObjects]
    public sealed class ScrollViewEditor : Editor<ScrollView>
    {
        public override void OnInspectorGUI() => DrawDefaultInspector();
    }
}
