

using UnityEngine;
using Yu5h1Lib;

public abstract class AssetSequence : ScriptableObject {}
public abstract class AssetSequence<TComponent,TValue> : AssetSequence where TValue : Object
{
    public TValue[] items;

    public void MoveNext(TComponent component)
    {
        if (items.IsEmpty())
            return;
        var index = items.Repeat(items.IndexOf(GetValue(component)) + 1);
        SetValue(component, items[index]);
    }

    public abstract TValue GetValue(TComponent renderer);
    public abstract void SetValue(TComponent component, TValue val);

}