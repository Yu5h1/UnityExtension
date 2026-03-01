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

        private void OnEnable()
        {
            TryPrepareList(serializedObject.FindProperty("_Items"), out itemsList);
            itemsList.allowFilter = false;
            itemsList.MarkAsFilterProvider();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            itemsList?.UnmarkAsFilterProvider();
        }
    }
}