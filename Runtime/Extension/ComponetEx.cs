using UnityEngine;

namespace Yu5h1Lib
{
	public static class ComponentEx
	{
		public static bool TryGetComponentInParent<T>(this Component component, out T result, bool IncludeSelf = false) where T : Component
		{
			if (IncludeSelf && component.TryGetComponent(out result)) 
				return true;
            return result = component.GetComponentInParent<T>();
        }

		public static bool TryGetComponentInChildren<T>(this Component component, out T result, bool IncludeSelf = false) where T : Component
		{
            if (IncludeSelf && component.TryGetComponent(out result))
                return true;
            return result = component.GetComponentInChildren<T>();
        }
		public static bool TryGetComponentInChildren<T>(this Component c, string name, out T component) where T : Object
		{
			component = null;
			return c.transform.TryFind(name, out Transform child) && child.TryGetComponent(out component);
		}
		public static T GetOrAdd<T>(this Component c, out T componentParam) where T : Component
		{
			if (!c.TryGetComponent(out componentParam))
				componentParam = c.gameObject.AddComponent<T>();
			return componentParam;
		}
		public static T GetOrAddIfNull<T>(this Component c, ref T target) where T : Component
			=> target ?? c.GetOrAdd(out target);

		public static bool EqualAnyTag(this Component c, params string[] items)
		{
			foreach (var item in items)
				if (c.CompareTag(item))
					return true;
			return false;
		}
	} 
}
