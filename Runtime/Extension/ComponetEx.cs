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

        public static bool TryGetComponentInChildren<T>(this Component component, out T result, bool includeInactive, bool includeSelf = false) where T : Component
        {
            result = null;
            if (component == null)
                return false;

            if (includeSelf)
            {
                result = component.GetComponentInChildren<T>(includeInactive);
                return result != null;
            }

            foreach (Transform child in component.transform)
            {
                result = child.GetComponentInChildren<T>(includeInactive);
                if (result != null)
                    return true;
            }
            return false;
        }
        public static bool TryGetComponentInChildren<T>(this Component c, string name, out T result,bool searchAllChildren = false, bool includeInactive = true) where T : Component
		{
			result = null;
			if (c == null)
				return false;
			if (searchAllChildren)
			{
				foreach (var item in c.GetComponentsInChildren<T>(includeInactive))
				{
                    if (item.name.Equals(name))
					{
						result = item;
						return true;
					}
                }
				return false;
			}

            return c.transform.TryFind(name, out Transform child) && child.TryGetComponent(out result);
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
