using UnityEngine;
using Yu5h1Lib.Data;

namespace Yu5h1Lib
{
	public class TransformAddon : ComponentController<Transform>
	{
        public void SetPosition(Vector3Object position)
		{
            if (position == null)
                return;
            transform.position = position.value;
		}
		public void Apply(TransformInfoObject info) 
		{ 
			if (info == null)
				return;
			info.ApplyTo(transform);
        }
	} 
}
