using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
	public class EventOptions : OptionSet<UnityEvent>
	{
        protected override void OnSelected(int index,UnityEvent current)
        {

        }
		public void Invoke() => current?.Invoke();
    }
    public abstract class EventOptions<T> : OptionSet<UnityEvent<T>> { }
}
