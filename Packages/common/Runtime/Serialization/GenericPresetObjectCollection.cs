using UnityEngine;

namespace Yu5h1Lib
{
	public class GenericPresetObjectCollection : CollectionObject<GenericObjectPreset> 
	{
        public override void ApplyTo(Object target)
        {
            foreach (var preset in value)
			{
                if (target?.GetType() == preset.targetType.type)
                {
                    preset.ApplyTo(target);
                    break;
                }
            }
        }        
    }
}
