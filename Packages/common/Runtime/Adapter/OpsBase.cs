using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib.Common
{
    /// <summary>
    /// Base class for component operations (Ops = Operations).
    /// Wraps a Unity component and provides a unified interface for manipulation.
    /// </summary>
    /// <typeparam name="T">The type of component this operation wraps.</typeparam>
    public abstract class OpsBase<T> : IOps where T : Component
    {
        protected readonly T c;
        public Component RawComponent => c;
        protected OpsBase(T component) => c = component;
    }

}
