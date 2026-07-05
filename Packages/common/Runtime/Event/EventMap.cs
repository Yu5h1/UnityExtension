using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib
{
	public class EventMap<T, U> : KeyValue<T, UnityEvent<U>>
    {
		public void Invoke(T key)
		{

        }
    } 
}
