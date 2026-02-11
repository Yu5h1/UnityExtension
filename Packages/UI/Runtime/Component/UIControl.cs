using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    [RequireComponent(typeof(RectTransform))]
    public abstract class UIControl : BaseMonoBehaviour
    {
        [SerializeField, ReadOnly]
        private RectTransform _rectTransform;
        public RectTransform rectTransform => _rectTransform;

        protected virtual void Reset()
        {
            Init();
        }
        protected override void OnInitializing()
        {
            this.GetComponent(ref _rectTransform);
        }
    }

    public abstract class UIControl<T> : UIControl where T : UIBehaviour
    {
        [SerializeField, ReadOnly] private T _ui;
        public T ui => _ui;

        protected override void OnInitializing()
        {
            base.OnInitializing();
            _ui = this.GetComponent(ref _ui);
        }
    }
}
