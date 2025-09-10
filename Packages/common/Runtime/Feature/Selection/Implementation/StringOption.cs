using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yu5h1Lib;

public class StringOption : OptionSet<string>, IBindable
{
    public override string GetValue() => current;
    public override void SetValue(string value) => selector.current = Items.IndexOf(value);
}
