using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    public class MonoEventHandler : MonoBehaviour
    {
        [SerializeField] private UnityEvent _Enabled;
        [SerializeField] private UnityEvent _Disabled;


        private void OnEnable()
        {
            if (!isActiveAndEnabled)
                return;
            _Enabled?.Invoke();
        }
        private void OnDisable()
        {
            if (!isActiveAndEnabled || ApplicationInfo.WantsToQuit)
                return;
            _Disabled?.Invoke();
        }
        public void Log( ){ }
    }

}