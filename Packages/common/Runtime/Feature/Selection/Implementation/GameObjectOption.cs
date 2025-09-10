using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yu5h1Lib;

public class GameObjectOption : OptionSet<GameObject>
{
    protected override void OnSelected(GameObject current)
    {
        foreach (var item in Items)
            item.gameObject.SetActive(item == current);
    }
}
