using System;
using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib.UX;

namespace Yu5h1Lib
{
    [DisallowMultipleComponent]
    public class View : MonoBehaviour, INavigable
    {
        [SerializeField]
        private UnityEvent _arrived;
        [SerializeField]
        private UnityEvent _left;

        public event Action Arrived;
        public event Action Left;

        public bool visible
        {
            get => gameObject.activeSelf;
            set
            {
                if (value)
                    Show();
                else
                    Hide();
            }
        }

        public void Show()
        {
            if (gameObject.activeSelf)
                return;

            gameObject.SetActive(true);
            Arrived?.Invoke();
            _arrived?.Invoke();
        }

        public void Hide()
        {
            if (!gameObject.activeSelf)
                return;

            Left?.Invoke();
            _left?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
