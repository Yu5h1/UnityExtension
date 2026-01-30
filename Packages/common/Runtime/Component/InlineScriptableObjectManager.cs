using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using Yu5h1Lib;

[MovedFrom("IntegersManager")]
public class InlineScriptableObjectManager : SingletonBehaviour<InlineScriptableObjectManager>
{
    [DropdownContext("vcp_AnimationList")]
    [SerializeField,Inline] private List<IntegerArrayObject> _integersObjectList;

    protected override void OnInitializing()
    {
    }

    protected override void OnInstantiated()
    {
    }

    [ContextMenu(nameof(Test))]
    public void Test()
    {
        var foundSObj = Resources.FindObjectsOfTypeAll<StringArrayObject>();
        $"Count : {foundSObj.Length}\n{foundSObj.Select(dd => dd.name).Join('\n')}".print();
        var iobjs = Resources.FindObjectsOfTypeAll<IntegerArrayObject>();
        $"Count : {iobjs.Length} {iobjs.Select(dd => dd.name).Join('\n')}".print();
    }
}
