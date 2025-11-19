using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using Yu5h1Lib;

[MovedFrom("IntegersManager")]
public class InlineScriptableObjectManager : SingletonBehaviour<InlineScriptableObjectManager>
{
    [DropdownContext("vcp_AnimationList")]
    [SerializeField,ShowDetail()] private List<IntegersObject> _integersObjectList;

    protected override void OnInitializing()
    {
    }

    protected override void OnInstantiated()
    {
    }
}
