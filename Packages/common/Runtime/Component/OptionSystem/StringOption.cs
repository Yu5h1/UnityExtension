using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib.Serialization;

namespace  Yu5h1Lib
{
    public class StringOption : OptionSetValue<string>
    {
        public OptionSet overrideSet;

        [SerializeField, ReadOnly] List<string> _overrideItems;

        public override List<string> Items
        {
            get
            {
                if (overrideSet == null)
                    return base.Items;

                if (_overrideItems == null || _overrideItems.Count != overrideSet.Count)
                {
                    _overrideItems = new List<string>();
                    for (int i = 0; i < overrideSet.Count; i++)
                        _overrideItems.Add(overrideSet.GetItemText(i));
                }
                return _overrideItems;
            }
        }
        public override bool TryParse(string value, out string result)
        {
            result = current;
            if (overrideSet != null)
            {
                result = overrideSet.GetItemText(selector.current);
                return true;
            }
            return true;
        }
        public override string GetValue() => overrideSet == null ? current : overrideSet.GetItemText(selector.current);
    } 
}
