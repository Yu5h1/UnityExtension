using UnityEngine;

namespace Yu5h1Lib
{
	public class GenericObjectCollectionBinding : Theme.BindingObject<Object, GenericPresetObjectCollection>
	{
        protected override void SetValue(Object c, GenericPresetObjectCollection value)
        {
            value.ApplyTo(c);
        }
        public override void Apply(Theme profile)
        {
            if (!profile.TryGet<GenericPresetObjectCollection>(key, out var value))
            {
                $"key:{key} not found".printWarning();
                return;
            }
            foreach (var target in targets)
            {
                if (target != null)
                    SetValue(target, value);
            }
        }
	}

}
