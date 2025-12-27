using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yu5h1Lib;

public class StringOption : OptionSet<string>, IBindable
{    
    public OptionSet overrideSet;

    [SerializeField,ReadOnly] List<string> _overrideItems;
    
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
                    _overrideItems.Add(overrideSet.GetItemName(i));
            }
            return _overrideItems;
        }
    } 

    public override string GetValue() => overrideSet == null ? current : overrideSet.GetItemName(selector.current);
    public override void SetValue(string value) => selector.current = Items.IndexOf(value);
}
