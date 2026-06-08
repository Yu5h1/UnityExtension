using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    public class TriggerEvent3D : TriggerEvent<UnityEvent<Collider>, Collider>
    {
        private void OnTriggerEnter(Collider other) => OnEntered(other);
        private void OnTriggerExit(Collider other) => OnExited(other);

        public override bool GetIsTrigger(Collider collider) => collider.isTrigger;
        public override bool SetIsTrigger(Collider collider, bool flag) => collider.isTrigger = flag;
        public override void InvokeTEvent(Collider c, UnityEvent<Collider> e) => e?.Invoke(c);
    }


    public class TriggerEvent<T> : TriggerEvent<UnityEvent<T>,Collider> where T : Component
    {
        private void OnTriggerEnter(Collider other) => OnEntered(other);
        private void OnTriggerExit(Collider collision) => OnExited(collision);

        protected bool IsValid(T component)
            => component != null && (string.IsNullOrEmpty(tag) || tag.Equals("Untagged") || component.gameObject.tag.Equals(tag));

        // Validate by resolving T first — prevents null-invoke and enforces tag filter.
        public override bool Validate(Collider other) => IsValid(other.GetComponent<T>());

        public override bool GetIsTrigger(Collider collider) => collider.isTrigger;
        public override bool SetIsTrigger(Collider collider, bool flag) => collider.isTrigger = flag;

        public override void InvokeTEvent(Collider c, UnityEvent<T> e)
        {
            var component = c.GetComponent<T>();
            if (component != null)
                e?.Invoke(component);
        }
    }
}
