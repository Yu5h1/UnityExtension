// Stores a method name and bridges Inspector-authored calls to the registered-object hub.
using UnityEngine;
using Object = UnityEngine.Object;

namespace Yu5h1Lib
{
    /// <summary>
    /// Provides zero-argument and single-argument UnityEvent entry points for a configured method.
    /// Objects subscribe explicitly; unregistering never creates a hub.
    /// </summary>
    public class BroadcasterProxy : BaseMonoBehaviour
    {
        [SerializeField] private string _methodName = "Call";

        public string methodName { get => _methodName; set => _methodName = value; }

        protected override void OnInitializing() {}

        public void Register(Object target)
        {
            if (!target)
                return;
            var hub = Broadcaster.instance;
            if (hub)
                hub.Register(target);
        }

        public void Unregister(Object target)
        {
            if (Broadcaster.TryGetExisting(out var hub))
                hub.Unregister(target);
        }

        public void Dispatch()
        {
            var hub = Broadcaster.instance;
            if (hub)
                hub.Dispatch(_methodName);
        }

        public void Dispatch(string argument) => DispatchArgument(argument);
        public void Dispatch(int argument) => DispatchArgument(argument);
        public void Dispatch(float argument) => DispatchArgument(argument);
        public void Dispatch(bool argument) => DispatchArgument(argument);
        public void Dispatch(Object argument) => DispatchArgument(argument);

        private void DispatchArgument<T>(T argument)
        {
            var hub = Broadcaster.instance;
            if (hub)
                hub.Dispatch(_methodName, argument);
        }
    }
}
