using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Yu5h1Lib.Theming;

namespace Yu5h1Lib.EditorExtension
{
    [CustomEditor(typeof(ThemeOptionSet))]
    public class ThemeOptionSetEditor : Editor<ThemeOptionSet>
    {
        private ReorderableListEnhanced itemsList;
        static string filterText;
        private void OnEnable()
        {
            TryPrepareList(serializedObject.FindProperty("_Items"),out itemsList);
            itemsList.allowFilter = false;
            ReorderableListEnhanced.FilterTextProvider.Remove(targetObject.GetInstanceID());
            ReorderableListEnhanced.FilterTextProvider.Add(targetObject.GetInstanceID(), GetfilterText);
        }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUI.changed)
                filterText = itemsList.GetFilterText();
        }

        string GetfilterText() => filterText;
        protected override void OnDisable()
        {
            base.OnDisable();
            ReorderableListEnhanced.FilterTextProvider.Remove(targetObject.GetInstanceID());
        }
    }
}