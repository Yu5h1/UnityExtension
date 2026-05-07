using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
	public class EventOptions : OptionSet<UnityEvent>
	{
        public void InvokeCurrentEvent() => current?.Invoke();
    }
    public abstract class EventOptions<T> : OptionSet<UnityEvent<T>> { }
}
