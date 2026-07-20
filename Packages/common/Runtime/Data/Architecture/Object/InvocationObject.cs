using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Yu5h1Lib
{
    /// <summary>
    /// Resolves a target relative to an input object and invokes a sequence of methods on it.
    /// </summary>
    public class InvocationObject : ScriptableObject
    {
        [SerializeField, Tooltip(
            "Relative child path from the target GameObject. Leave empty to use the supplied target directly.")]
        private string _path;

        [SerializeField]
        private SerializedType _targetType;

        [SerializeField]
        private List<MethodObject.Descriptor> _methods = new();

        public string path => _path;
        public System.Type targetType => _targetType?.type;
        public IReadOnlyList<MethodObject.Descriptor> methods => _methods;

        public void Invoke(Object target)
            => TryInvoke(target);

        public bool TryInvoke(Object target)
        {
            if (target == null)
            {
#if UNITY_EDITOR
                "Invocation target is unassigned".printWarning();
#endif
                return false;
            }

            var type = _targetType?.type;
            if (type == null)
            {
#if UNITY_EDITOR
                $"{name} target type is unassigned".printWarning();
#endif
                return false;
            }

            var resolvedTarget = ResolveTarget(target, type);
            if (resolvedTarget == null)
            {
#if UNITY_EDITOR
                $"Could not resolve '{_path}' as {type} from {target.name}".printWarning();
#endif
                return false;
            }

            if (!type.IsInstanceOfType(resolvedTarget))
            {
#if UNITY_EDITOR
                $"{type} does not match {resolvedTarget.GetType()}".printWarning();
#endif
                return false;
            }

            if (_methods == null)
                return true;

            for (int i = 0; i < _methods.Count; i++)
            {
                var method = _methods[i];
                if (method == null)
                {
#if UNITY_EDITOR
                    $"Method descriptor at index {i} is unassigned".printWarning();
#endif
                    return false;
                }

                if (!method.TryInvoke(resolvedTarget))
                    return false;
            }

            return true;
        }

        private Object ResolveTarget(Object target, System.Type type)
        {
            if (string.IsNullOrEmpty(_path))
                return target;

            GameObject root = null;
            if (target is GameObject gameObject)
                root = gameObject;
            else if (target is Component component)
                root = component.gameObject;

            if (root == null)
                return target;

            var child = root.transform.Find(_path);
            if (child == null)
                return null;

            if (type.IsInstanceOfType(child.gameObject))
                return child.gameObject;
            if (type.IsInstanceOfType(child))
                return child;
            if (typeof(Component).IsAssignableFrom(type))
                return child.GetComponent(type);

            return null;
        }
    }
}
