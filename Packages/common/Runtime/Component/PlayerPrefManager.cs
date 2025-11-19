using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib;

namespace Yu5h1Lib
{
	public class PlayerPrefManager : SingletonBehaviour<PlayerPrefManager>
    {
		[SerializeField,ShowDetail] private List<PlayerPrefBoolObject> bools;

    
        public UnityEvent<int, bool, string, float> qqq;
        [SerializeField] private UnityEvent<string> _sss;

        public int _____qq;

        public Bounds bbb;

        protected override void OnInstantiated()
        {
        }
        protected override void OnInitializing()
        {
        }
        private void Start()
        {
            if (!enabled)
                return;
            foreach (var item in bools)
            {
                item.data.Init();
            }
        }



        public void v3(Vector3Object v3) { }

        public void qq(RectObject val) { }
        public void qq(Vector2Object val) { }

        public void qq(IntegersObject val) { }
        public void qq(ColorObject val) { }
        public void qq(AnimationCurveObject val) { }
        public void qq(GradientObject val) { }
        public void qq(BoundsObject val) { }
    } 
}
