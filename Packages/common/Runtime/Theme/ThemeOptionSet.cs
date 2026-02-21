using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib.Theming
{
    /// <summary>
    /// 主題選項集 - 接入 OptionSelector 系統
    /// </summary>
    public class ThemeOptionSet : OptionSet<Theme>
    {
        [SerializeField,Inline(true)] private List<Theme.BindingObject> _bindings;
        public List<Theme.BindingObject> bindings => _bindings;

        protected override void OnSelected(Theme profile)
        {
            foreach (var group in _bindings)
            {
                if (group != null)
                    group.Apply(profile);
            }
        }

        public override string ToString(Theme item)
        {
            return item != null ? item.name : string.Empty;
        }
    }
}
