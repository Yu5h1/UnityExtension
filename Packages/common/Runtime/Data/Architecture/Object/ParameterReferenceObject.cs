using UnityEngine;

namespace Yu5h1Lib
{
	public class ParameterReferenceObject : ParameterObject<ParameterObject>
    {
        public override void ApplyTo(Object target)
        {
            if (value == null)
                return;
            value.ApplyTo(target);
        }
	} 
}
