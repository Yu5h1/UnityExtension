using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib.Common
{
    public abstract class OpsBase<T> : IOps where T : Component
    {
        protected readonly T c;
        public Component RawComponent => c;
        protected OpsBase(T component) => c = component;
    }

}
