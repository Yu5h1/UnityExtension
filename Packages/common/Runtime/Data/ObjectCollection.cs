using UnityEngine;

namespace Yu5h1Lib
{
	public class ObjectCollection : ParameterCollection<Object>
    {
         
        public void Apply(ParameterObject p)
        {
            foreach (var target in value)
                p.ApplyTo(target);
        }
	}

}