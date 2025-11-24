using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib;

namespace Yu5h1Lib
{
	public class PlayerPrefManager : SingletonBehaviour<PlayerPrefManager>
    {
		[SerializeField,ShowDetail] private List<PlayerPrefBoolObject> bools;

        protected override void OnInstantiated() {}
        protected override void OnInitializing() {}
        private void Start()
        {
            if (!enabled)
                return;
            foreach (var item in bools)
            {
                item.data.Init();
            }
        }
    } 
}
