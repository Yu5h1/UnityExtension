using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib.Mathematics;

namespace Yu5h1Lib
{
    [System.Serializable]
    public class Timer : TimerBase, ITimer
    {
        public enum RepeatType : int {
            Infinite = -1,
            None = 0,
            Standard = 1
        }
        [SerializeField]
        private float Delay = 0;
        [SerializeField]
        private float Duration = 1;
        [SerializeField]
        private int RepeatCount;
        [SerializeField]
        private bool UseUnscaledTime;

        public float delay { get => Delay; set => Delay = value; }
        public float duration { get => Duration; set => Duration = value; }
        public int repeatCount { get => RepeatCount; set => RepeatCount = value; }
        public RepeatType repeatType => (RepeatType)System.Math.Sign(repeatCount);
        public float normalized => time.GetNormal(duration);

        protected override float GetTime()
            => UseUnscaledTime ? base.GetTime() : Time.unscaledTime;

        public event System.Func<bool> keepwaitingCondition;
        public bool keepWaiting
        {
            get
            {
                if (keepwaitingCondition == null)
                    return true;
                var conditions = keepwaitingCondition.GetInvocationList();
                foreach (var condition in conditions)
                    if (!(condition as System.Func<bool>)())
                        return false;
                return true;
            }
        }
        public event UnityAction Update;
        public event UnityAction Completed;
        public event UnityAction Repeated;
        public event UnityAction FinalRepetition;

        #region Caches
        public int repeatCounter { get; private set; }
        public bool IsCompleted { get; private set; }
        #endregion

        public float timeElapsed => (repeatCounter * duration) + time;
        
        public bool IsStart => time > 0;
        public bool TimesUp => time >= duration;
        public bool IsRepeatComplete => repeatType == RepeatType.Infinite ? false : repeatCounter == repeatCount;        
        public bool IsCompleting => IsRepeatComplete && TimesUp;            


        public override void Start()
        {
            LastTime = GetTime() + delay;
            repeatCounter = 0;
            IsCompleted = false;
        }
        public virtual void Stop()
            => LastTime = time - duration;

        public virtual void Tick()
        {
            if (IsCompleted)
                return;
            if (IsCompleting)
            {
                IsCompleted = true;
                OnCompleted();
                Completed?.Invoke();
            }
            else if (TimesUp)
            {
                if (repeatType != RepeatType.None && repeatCounter != repeatCount)
                {
                    if (repeatCounter + 1 == repeatCount)
                    {
                        OnFinalRepetition();
                        FinalRepetition?.Invoke();
                    }
                    repeatCounter++;
                    OnRepeat();
                    Repeated?.Invoke();
                    LastTime = GetTime();
                }
            }
            else
                Update?.Invoke();
        }
        protected virtual void OnRepeat() { }
        protected virtual void OnFinalRepetition() { }
        protected virtual void OnCompleted() { }
        public void StopRepeat() => repeatCounter = repeatCount;

        public Wait<Timer> Waiting() => new Wait<Timer>(this);

        public override string ToString() => $"{time} Duration:{Duration} IsCompleted:{IsCompleted} repeatCount:{repeatCounter}";

        public abstract class Wait : CustomYieldInstruction 
        {
            public override bool keepWaiting => throw new System.NotImplementedException();
        }
        public class Wait<T> : Wait where T : Timer
        {
            public T timer { get; private set; }
            public override bool keepWaiting
            {
                get
                {
                    if (!timer.keepWaiting)
                        return false;
                    timer.Tick();
                    return !timer.IsCompleted ;
                }
            }
            public Wait(T t)
            {
                timer = t;
            }
            public override void Reset()
            {
                timer.Start();
            }
        }

    }
    public interface ITimer
    {
        float time { get; }
        float timeElapsed { get; }
        float normalized { get; }
        int repeatCounter { get; }
    }
}