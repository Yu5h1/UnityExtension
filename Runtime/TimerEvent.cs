using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    [System.Serializable]
    public class TimerEvent : Timer
    {
        [SerializeField]
        private UnityEvent<Timer> _Completed = new UnityEvent<Timer>();
        [SerializeField]
        private UnityEvent<Timer> _Repeated = new UnityEvent<Timer>();
        [SerializeField]
        private UnityEvent<Timer> _FinalRepetition = new UnityEvent<Timer>();
        [SerializeField]
        private UnityEvent<Timer> _Update = new UnityEvent<Timer>();

        private void OnCompleted(Timer t ) => _Completed?.Invoke(t);
        private void OnRepeated(Timer t) => _Repeated?.Invoke(t);
        private void OnFinalRepetition(Timer t) => _FinalRepetition?.Invoke(t);
        private void OnUpdate(Timer t) => _Update?.Invoke(t);

        public void Register()
        {
            Unregister();
            Completed += OnCompleted;
            Repeated += OnRepeated;
            FinalRepetition += OnFinalRepetition;
            Update += OnUpdate;
        }
        public void Unregister()
        {
            Completed -= OnCompleted;
            Repeated -= OnRepeated;
            FinalRepetition -= OnFinalRepetition;
            Update -= OnUpdate;
        }
    }
}
