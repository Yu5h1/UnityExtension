using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib.Mathematics;

namespace Yu5h1Lib
{
    [System.Serializable]
    public class Timer 
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

        #region Cache
        public float LastTime { get; protected set; } 
        #endregion

        public float delay { get => Delay; set => Delay = value; }
        public float duration { get => Duration; set => Duration = value; }
        public int repeatCount { get => RepeatCount; set => RepeatCount = value; }
        public bool useUnscaledTime { get => UseUnscaledTime; set => UseUnscaledTime = value; }
        public RepeatType repeatType => (RepeatType)System.Math.Sign(repeatCount);

        protected float GetTime() => UseUnscaledTime ? Time.unscaledTime : Time.time;
        public float time => GetTime() - LastTime;
        public float normalized => time.GetNormal(duration);

        

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
        public event UnityAction<Timer> Update;
        public event UnityAction<Timer> Completed;
        public event UnityAction<Timer> Repeated;
        public event UnityAction<Timer> FinalRepetition;

        #region Caches
        public int repeatCounter { get; private set; }
        public bool IsCompleted { get; private set; }
        #endregion

        public float timeElapsed => (repeatCounter * duration) + time;
        
        public bool IsStart => time > 0;
        public bool TimesUp => time >= duration;
        public bool IsRepeatComplete => repeatType == RepeatType.Infinite ? false : repeatCounter == repeatCount;        
        public bool IsCompleting => IsRepeatComplete && TimesUp;            

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
                Completed?.Invoke(this);
            }
            else if (TimesUp)
            {
                if (repeatType != RepeatType.None && repeatCounter != repeatCount)
                {
                    if (repeatCounter + 1 == repeatCount)
                    {
                        OnFinalRepetition();
                        FinalRepetition?.Invoke(this);
                    }
                    repeatCounter++;
                    OnRepeat();
                    Repeated?.Invoke(this);
                    LastTime = GetTime();
                }
            }
            else
                Update?.Invoke(this);
        }
        protected virtual void OnRepeat() { }
        protected virtual void OnFinalRepetition() { }
        protected virtual void OnCompleted() { }
        public void StopRepeat() => repeatCounter = repeatCount;

        public Wait<Timer> Waiting() => new Wait<Timer>(this);

        public override string ToString() => $"{time} Duration:{Duration} IsCompleted:{IsCompleted} repeatCount:{repeatCounter}";

        //public IEnumerator GetEnumerator(IEnumerator[] enumerators)
        //{
        //    for (int i = 0; i < enumerators.Length; i++)
        //    {
        //        yield return enumerators[i];
        //    }
        //}

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

}