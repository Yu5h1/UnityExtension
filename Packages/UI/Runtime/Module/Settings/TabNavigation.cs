using UnityEngine;
using UnityEngine.EventSystems;

namespace Yu5h1Lib.UI
{
    /// <summary>Routes native move events to the owning tab strip.</summary>
    internal sealed class TabNavigation : MonoBehaviour, IMoveHandler
    {
        private Tabs owner;
        internal void Initialize(Tabs tabs) => owner = tabs;

        public void OnMove(AxisEventData eventData)
        {
            if (owner.Navigate(eventData.moveDir))
                eventData.Use();
        }
    }
}
