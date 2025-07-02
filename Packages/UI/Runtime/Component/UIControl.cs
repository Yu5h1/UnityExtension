using UnityEngine;

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
}
