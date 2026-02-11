using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
	public class UnityEventTest : MonoBehaviour
	{
		[SerializeField] private UnityEvent _contextMenuEvent;

		[ContextMenu(nameof(Trigger))]
		public void Trigger() => _contextMenuEvent?.Invoke();

	} 
}
