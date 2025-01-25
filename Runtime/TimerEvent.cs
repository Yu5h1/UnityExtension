using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    public abstract class TimerBehaviour : MonoBehaviour , ITimer
    {
        [SerializeField]
        private Timer timer;
        
        [SerializeField]
        private UnityEvent<float> Completed = new UnityEvent<float>();
        [SerializeField]
        private UnityEvent<int> Repeated = new UnityEvent<int>();
        [SerializeField]
        private UnityEvent<int> FinalRepetition = new UnityEvent<int>();
        [SerializeField]
        private UnityEvent<ITimer> Update = new UnityEvent<ITimer>();

        #region interface properties
        public float time => ((ITimer)timer).time;
        public float timeElapsed => ((ITimer)timer).timeElapsed;
        public float normalized => ((ITimer)timer).normalized;
        public int repeatCounter => ((ITimer)timer).repeatCounter;
        #endregion

        public void RegeristerTimerEvents()
        {
            timer.Update += timer_Update;
            timer.Completed += Timer_Completed;
            timer.Repeated += Timer_Repeated;
            timer.FinalRepetition += Timer_FinalRepetition;
        }

        private void Timer_FinalRepetition() => FinalRepetition?.Invoke(repeatCounter);
        private void Timer_Repeated() => Repeated?.Invoke(repeatCounter);
        private void Timer_Completed() => Completed?.Invoke(timeElapsed);
        private void timer_Update() => Update?.Invoke(timer);
    }
}
