using System;
using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib.Mathematics;

namespace Yu5h1Lib
{
    [System.Serializable]
    public partial class Timer : ITimer
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

        public float delay { get => Delay; set => Delay = value; }
        public float duration { get => Duration; set => Duration = value; }
        public int repeatCount { get => RepeatCount; set => RepeatCount = value; }
        public RepeatType repeatType => (RepeatType)Math.Sign(repeatCount);

        public event Func<bool> KeepwaitingHandler;
        public bool keepWaiting
        {
            get
            {
                if (KeepwaitingHandler == null)
                    return true;
                var conditions = KeepwaitingHandler.GetInvocationList();
                foreach (var condition in conditions)
                    if (!(condition as Func<bool>)())
                        return false;
                return true;
            }
        }

        [SerializeField]
        private UnityEvent _Update = new UnityEvent();
        public event UnityAction Update
        {
            add => _Update?.AddListener(value);
            remove => _Update.RemoveListener(value);
        }
        [SerializeField]
        private UnityEvent _Completed = new UnityEvent();
        public event UnityAction Completed
        {
            add => _Completed?.AddListener(value);
            remove => _Completed?.RemoveListener(value);
        }
        [SerializeField]
        private UnityEvent _Repeated = new UnityEvent();
        public event UnityAction Repeated
        {
            add => _Repeated.AddListener(value);
            remove => _Repeated.RemoveListener(value);
        }
        [SerializeField]
        private UnityEvent _FinalRepetition = new UnityEvent();
        public event UnityAction FinalRepetition
        {
            add => _FinalRepetition.AddListener(value);
            remove => _FinalRepetition.RemoveListener(value);
        }

        #region Caches
        protected float LastTime { get; private set; }
        public int repeatCounter { get; private set; }
        public bool IsCompleted { get; private set; }
        public bool CheckIsCompleted() => IsCompleted;
        #endregion

        protected virtual float GetTime() => Time.time;
        public float time => GetTime() - LastTime;
        public float timeElapsed => (repeatCounter * duration) + time;
        
        public bool IsStart => time > 0;
        public bool TimesUp => time >= duration;
        public bool IsRepeatComplete => repeatType == RepeatType.Infinite ? false : repeatCounter == repeatCount;
        
        public bool IsCompleting => IsRepeatComplete && TimesUp;
            
        public float normal => time.GetNormal(duration);

        public virtual void Start()
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
                _Completed?.Invoke();
            }
            else if (TimesUp)
            {
                if (repeatType != RepeatType.None && repeatCounter != repeatCount)
                {
                    if (repeatCounter + 1 == repeatCount)
                    {
                        OnFinalRepetition();
                        _FinalRepetition?.Invoke();
                    }
                    repeatCounter++;
                    OnRepeat();
                    _Repeated?.Invoke();
                    LastTime = GetTime();
                }
            }
            else
                _Update?.Invoke();
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
        float delay { get; set; }
        float duration { get; set; }
        int repeatCount { get; set; }
    }
}