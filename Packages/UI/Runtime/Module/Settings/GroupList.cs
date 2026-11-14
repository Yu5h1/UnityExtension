using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    /// <summary>A scrollable native single-selection list for settings groups.</summary>
    public sealed class GroupList : MonoBehaviour
    {
        private Tabs choices;
        public string SelectedId => choices.SelectedId;
        public Selectable ActiveButton => choices.ActiveButton;
        public bool Interactable { get => choices.Interactable; set => choices.Interactable = value; }

        public event Action<string> Changed
        {
            add => choices.Changed += value;
            remove => choices.Changed -= value;
        }

        public static GroupList Create(Transform parent, UguiControlPalette palette = null)
        {
            var rect = SettingsLayout.Rect(parent, "Groups");
            var list = rect.gameObject.AddComponent<GroupList>();
            list.choices = Tabs.Create(rect, palette, true);
            SettingsLayout.Stretch((RectTransform)list.choices.transform);
            return list;
        }

        public void SetGroups(IReadOnlyList<(string Id, string Label)> groups) => choices.SetItems(MakeItems(groups));

        internal static Tabs.Item[] MakeItems(IReadOnlyList<(string Id, string Label)> groups)
        {
            if (groups == null)
                throw new ArgumentNullException(nameof(groups));
            var items = new Tabs.Item[groups.Count];
            for (var index = 0; index < groups.Count; index++)
                items[index] = new Tabs.Item(groups[index].Id, groups[index].Label);
            return items;
        }

        internal void ApplyPalette(UguiControlPalette palette) => choices.ApplyPalette(palette);

        public bool Select(string id, bool focus = false) => choices.Select(id, focus);
    }
}
