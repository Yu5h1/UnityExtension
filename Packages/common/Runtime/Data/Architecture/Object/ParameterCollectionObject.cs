using UnityEngine;

namespace Yu5h1Lib
{
	public class ParameterCollectionObject : ParameterCollection<ParameterObject>
	{
        public override void ApplyTo(Object target)
        {
            if (target == null) return;

            if (value == null || value.Length == 0) return;

            if (target is Component component)
            {
                int index = component.transform.GetSiblingIndex();
                if (index >= 0 && index < value.Length && value[index] != null)
                {
                    value[index].ApplyTo(target);
                    return;
                }
            }

            // fallback：如果你想要 default 行為（例如取 random 或第0個）
            var fallback = value[0];
            if (fallback != null) fallback.ApplyTo(target);
        }
    } 
}
