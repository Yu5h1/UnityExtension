using UnityEngine;

namespace Yu5h1Lib
{
	public class ActionBehaviour : MonoBehaviour
	{
        public Object target;
        [TypeRestriction(typeof(IParameter))]
		public Object argument;

		public void Execute()
		{
			if (!(argument is IParameter parameter))
				return;
            parameter.ApplyTo(target);
		}
    } 
}
