using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yu5h1Lib;

public class GameObjectOption : OptionSet<GameObject>
{
    public void ActivateSelected(GameObject target)
    {
        foreach (var item in Items)
            item.gameObject.SetActive(item == target);
    }
}
