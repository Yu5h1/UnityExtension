using UnityEngine;

namespace Yu5h1Lib.UI
{
    [RequireComponent(typeof(RectTransform))]
    public abstract class UIControl : MonoBehaviour
    {
        [SerializeField, ReadOnly]
        private RectTransform _rectTransform;
        public RectTransform rectTransform => _rectTransform;

        protected virtual void Awake()
           => this.GetComponent(ref _rectTransform);
        protected virtual void Reset()
        => this.GetComponent(ref _rectTransform);

        public void Log(string msg) => msg.print();
    } 
}
