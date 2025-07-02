using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    public class VersionChecker : MonoBehaviour
    {

        [SerializeField] private UnityEvent<string> _versionChecked;
        public event UnityAction<string> versionChecked
        {
            add => _versionChecked.AddListener(value);
            remove => _versionChecked.RemoveListener(value);
        }
        void Start()
        {
            CheckVersion();
        }
        public void CheckVersion()
        {
            var platform = $"{Application.platform}";
            if (!Application.absoluteURL.IsEmpty())
                platform += " - WebGL";
            _versionChecked?.Invoke($"{platform} {Application.version}");
        }
 
    } 
}

