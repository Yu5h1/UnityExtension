using UnityEngine;

namespace Yu5h1Lib
{
    public abstract class PhysicsEventBase : BaseMonoBehaviour
    {
        [SerializeField] protected TagLayerMask _filter;

        [SerializeField, Tooltip("-1 = unlimited")]
        private int _maxCount = -1;

        private int _counter;

        public TagLayerMask filter => _filter;
        public int maxCount => _maxCount;
        public int counter => _counter;

        protected virtual void OnEnable()
        {
            _counter = 0;
        }

        protected bool Pass(Component other)
        {
            if (!isActiveAndEnabled) return false;
            if (!_filter.Validate(other)) return false;
            if (_maxCount > 0 && _counter >= _maxCount) return false;
            return true;
        }

        protected void IncrementCounter()
        {
            if (_maxCount <= 0) return;
            _counter++;
            if (_counter >= _maxCount)
                enabled = false;
        }
    }
}
