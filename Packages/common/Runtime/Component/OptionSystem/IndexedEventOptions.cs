using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
	public class IndexedEventOptions : EventOptions<int>
	{
        protected override void OnSelected(int index,UnityEvent<int> current)
        {
            if (selector == null)
                return;
            current?.Invoke(selector.current);
        }
	} 
}
