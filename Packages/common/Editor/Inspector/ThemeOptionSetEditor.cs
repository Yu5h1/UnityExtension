using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Yu5h1Lib.Theming;
using System.Linq;

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
        #region MenuItem

        private const string MENU_PATH = "CONTEXT/ThemeOptionSet/";
        private const string FindRelativeKeyItemName = MENU_PATH + nameof(FindRelativeKey);

        [MenuItem(FindRelativeKeyItemName)]
        private static void FindRelativeKey(MenuCommand command)
        {
            var target = command.context as ThemeOptionSet;
            var schema = target.Items.MostFrequentBy(t => t?.schema);

            var warningInfos = new List<string>();

            foreach (var item in schema.items)
            {
                if (target.bindings.TryFindIndex(b => b.keyRef == item,out int index))
                {
                    var bind = target.bindings[index];
                    if (bind.name != item.name)
                        warningInfos.Add($"bindings[{index}] name not matched with keyRef");
                }else
                    warningInfos.Add($"{item.name}");
            }

            foreach (var bind in target.bindings)
            {
                if (bind.keyRef == null)
                {
                    if (schema.items.TryGet(style => style.name == bind.name, out ParameterObject style))
                        bind.keyRef = style;
                    else if (bind.name.IsEmpty())
                        warningInfos.Add($"([{target.bindings.IndexOf(bind)}]Empty)");
                    else
                        warningInfos.Add(bind.name);
                }
            }
            if (warningInfos.Count > 0)
                $" key not exists in schema({schema.name}): \n{warningInfos.Join('\n')}\n------".printWarning();
        }
        #endregion
    }

}