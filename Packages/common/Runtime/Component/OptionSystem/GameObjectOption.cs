using UnityEngine;
using Yu5h1Lib;

public class GameObjectOption : OptionSet<GameObject>
{
    protected override void OnSelected(int index)
    {
        GameObject current = Items[index];
        foreach (var item in Items)
            item.gameObject.SetActive(item == current);
    }
}
