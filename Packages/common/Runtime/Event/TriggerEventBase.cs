using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    public abstract class TriggerEvent : BaseMonoBehaviour { }
    public abstract class TriggerEvent<TEvent, TCollider> : TriggerEvent
                                                            where TEvent : UnityEventBase
                                                            where TCollider : Component
    {
        [SerializeField]
        private bool _ignoreTrigger = true;
        public bool ignoreTrigger => _ignoreTrigger;

        [SerializeField] protected TEvent _entered;
        [SerializeField] protected TEvent _exited;

        /// <summary> Maximum trigger count. -1 or 0 = unlimited. </summary>
        [SerializeField, Header("limit of trigger")]
        private int _count = -1;
        public int count => _count;
        public int counter { get; set; }

        public bool NotAllowTriggerExit;

        public abstract bool GetIsTrigger(TCollider collider);
        public abstract bool SetIsTrigger(TCollider collider,bool flag);
        public abstract void InvokeTEvent(TCollider c, TEvent e);

        protected override void OnInitializing()
        {
            foreach (var c in GetComponents<TCollider>())
                SetIsTrigger(c,true);
            counter = 0;
        }
        public virtual bool Validate(TCollider other) => true;

        protected void OnEntered(TCollider other)
        {
            if (ignoreTrigger && GetIsTrigger(other))
                return;
            if (!Validate(other))
                return;

            if (count > 0)
            {
                counter++;
                if (counter >= count)
                    enabled = false;
            }
            if (other.TryGetComponent(out TriggerReceiver receiver))
                receiver.InvokeEnter(this);
            InvokeTEvent(other, _entered);
        }
        protected void OnExited(TCollider other)
        {
            if (NotAllowTriggerExit)
                return;
            if (!Validate(other))
                return;
            if (other.TryGetComponent(out TriggerReceiver receiver))
                receiver.InvokeExit(this);
            InvokeTEvent(other, _exited);
        }
        public void Log(Object obj) => obj.print();
    }
}