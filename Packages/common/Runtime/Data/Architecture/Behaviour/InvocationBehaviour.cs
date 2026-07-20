using UnityEngine;
using Object = UnityEngine.Object;

namespace Yu5h1Lib
{
    /// <summary>
    /// Provides a scene-side UnityEvent entry point for an InvocationObject.
    /// </summary>
    public class InvocationBehaviour : MonoBehaviour
    {
        [SerializeField, Inline(true)]
        private InvocationObject _invocation;

        public InvocationObject invocation
        {
            get => _invocation;
            set => _invocation = value;
        }

        public void Invoke()
        {
            if (_invocation != null)
                _invocation.Invoke(gameObject);
        }

        public void Invoke(Object target)
        {
            if (_invocation != null)
                _invocation.Invoke(target);
        }
    }
}
