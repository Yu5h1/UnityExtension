// Dispatches method calls to explicitly registered Unity objects without owning receiver behavior.
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Yu5h1Lib
{
    /// <summary>
    /// Invokes public instance methods on explicitly registered objects.
    /// Registrations are runtime-only and are cleared when the hub is disabled or destroyed.
    /// </summary>
    [AddComponentMenu("Yu5h1Lib/Signal Dispatcher")]
    public class Broadcaster : SingletonBehaviour<Broadcaster>
    {
        private readonly List<Object> _registeredObjects = new();

        /// <summary>
        /// Finds an existing hub without creating one, including during teardown.
        /// </summary>
        public static bool TryGetExisting(out Broadcaster broadcaster)
        {
            broadcaster = _instance;
            if (!broadcaster)
                ObjectUtility.TryFind(out broadcaster);
            return broadcaster;
        }

        protected override void OnInitializing() {}
        protected override void OnInstantiated() {}

        /// <summary>
        /// Registers an object once. Subscription lifecycle is supplied by the caller.
        /// </summary>
        public void Register(Object target)
        {
            if (!isActiveAndEnabled || !target)
                return;
            _registeredObjects.RemoveAll(item => !item);
            if (!_registeredObjects.Contains(target))
                _registeredObjects.Add(target);
        }

        public void Unregister(Object target)
        {
            _registeredObjects.Remove(target);
            _registeredObjects.RemoveAll(item => !item);
        }

        public bool Dispatch(string methodName) => DispatchCore(methodName, null, null);

        /// <summary>
        /// Dispatches one argument using its runtime type, or its declared type when null.
        /// </summary>
        public bool Dispatch<T>(string methodName, T argument)
            => DispatchCore(methodName, argument?.GetType() ?? typeof(T), argument);

        private bool DispatchCore(string methodName, Type argumentType, object argument)
        {
            if (!isActiveAndEnabled || methodName.IsEmpty())
                return false;

            _registeredObjects.RemoveAll(target => !target);
            var snapshot = _registeredObjects.ToArray();
            var invoked = false;
            foreach (var target in snapshot)
            {
                if (!target || !_registeredObjects.Contains(target))
                    continue;
                if (target is Behaviour behaviour && !behaviour.isActiveAndEnabled)
                    continue;
                if (target is Component component && !component.gameObject.activeInHierarchy)
                    continue;
                if (target is GameObject gameObject && !gameObject.activeInHierarchy)
                    continue;

                invoked |= argumentType == null
                    ? MethodInvoker.TryInvoke(target, methodName, null)
                    : MethodInvoker.TryInvoke(target, methodName, argumentType, argument);
            }
            return invoked;
        }

        private void OnDisable() => _registeredObjects.Clear();

        private void OnDestroy() => _registeredObjects.Clear();
    }
}
