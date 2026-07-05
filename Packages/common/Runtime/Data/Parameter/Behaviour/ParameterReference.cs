using UnityEngine;

namespace Yu5h1Lib.Parameter
{
	public class ParameterReference : ParameterBehaviour<ParameterBehaviour>
    {
        public override void ApplyTo(Object target) => value.ApplyTo(target);
	}
}