using System.Collections.Generic;
using UnityEngine;
using Yu5h1Lib;

public class IntegersManager : SingletonBehaviour<IntegersManager>
{
    //[Dropdown("vcp_AnimationList")]
    [SerializeField,ShowDetail] private List<IntegersObject> integers;
 

    protected override void OnInitializing()
    {
    }

    protected override void OnInstantiated()
    {
    }
}
