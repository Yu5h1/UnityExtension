using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
	public class UnityEventOption : OptionSet<UnityEvent>
	{
		public void Invoke() => current?.Invoke();
    } 
}
