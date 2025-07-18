using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yu5h1Lib;

namespace Yu5h1Lib.Common
{
    public class ComponentAdapter<TOps> : BaseMonoBehaviour, IOps where TOps : class, IOps
    {
        [SerializeField] private Component _component;
        public Component RawComponent => _component;
        private TOps _Ops;
        public TOps Ops => _Ops ??= OpsFactory.Create<TOps>(RawComponent);

        private void Reset() => Init();

        protected override void OnInitializing()
        {
            if (_component == null)
            {
                foreach (var type in OpsFactory.Ops[typeof(TOps)])
                    if (TryGetComponent(type, out _component))
                        break;
            }
        }
    } 
}
