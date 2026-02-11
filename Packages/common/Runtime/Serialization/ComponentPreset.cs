using UnityEngine;

namespace Yu5h1Lib
{
	public abstract class ComponentPreset<T> where T : Component
	{
		public abstract void ApplyTo(T component);

    } 
}
