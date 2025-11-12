using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Yu5h1Lib;

public class IntegersManager : SingletonBehaviour<IntegersManager>
{
    [DropdownContext("vcp_AnimationList")]
    [SerializeField,ShowDetail] private List<IntegersObject> _integersObjectList;

    protected override void OnInitializing()
    {
    }

    protected override void OnInstantiated()
    {
    }
}
