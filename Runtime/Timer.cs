using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Yu5h1Lib.Mathematics;

namespace Yu5h1Lib
{
    [System.Serializable]
    public class Timer 
    {
        public enum TickMode
        {
            Cumulative,
            TimeStamp
        }
        public delegate void TickMethod(ISource source, float lastTime,ref float time);

        public static void Cumulative(ISource source, float lastTime,ref float time)
                  => time += source.deltaTime;
        public static void TimeStamp(ISource source, float lastTime,ref float time)
                  => time = source.time - lastTime;
        public interface ISource
        {
            float deltaTime { get; }
            float time { get; }
        }
        public static readonly ISource Scaled = new ScaledTime();
        public static readonly ISource Unscaled = new UnscaledTime();
        private class ScaledTime : ISource
        {
            public float deltaTime => Time.deltaTime;
            public float time => Time.time;
        }
        private class UnscaledTime : ISource
        {
            public float deltaTime => Time.unscaledDeltaTime;
            public float time => Time.unscaledTime;
        }
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

        [SerializeField] private TickMode _tickMode = TickMode.TimeStamp;
        [SerializeField,FormerlySerializedAs("UseUnscaledTime")] private bool _useUnscaledTime;
  

        #region Cache
        public float LastTime { get; protected set; } 
        #endregion

        public float delay { get => Delay; set => Delay = value; }
        public float duration { get => Duration; set => Duration = value; }
        public int repeatCount { get => RepeatCount; set => RepeatCount = value; }

        private TickMethod tickMethod;
        private void CheckTickMethod()
        {
            switch (tickMode)
            {
                case TickMode.Cumulative:
                    tickMethod = Cumulative;
                    break;
                case TickMode.TimeStamp:
                    tickMethod = TimeStamp;
                    break;
                default:
                    tickMethod = TimeStamp;
                    break;
            }
        }
        public TickMode tickMode 
        { 
            get => _tickMode; 
            set
            { 
                if (_tickMode == value) return;
                _tickMode = value;
                CheckTickMethod();
            }
        }
        public void CheckTimeSource() => source = useUnscaledTime ? Unscaled : Scaled;
        public bool useUnscaledTime 
        { 
            get => _useUnscaledTime; 
            set
            { 
                if (_useUnscaledTime == value) return;
                _useUnscaledTime = value;
                CheckTimeSource();
            }
        }
        public RepeatType repeatType => (RepeatType)System.Math.Sign(repeatCount);

        public ISource source { get; protected set; }

        private float _time;
        public float time { get => _time; private set => _time = value; }

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
            CheckTimeSource(); // 確保初始化
            CheckTickMethod();

            LastTime = source.time + delay;
            time = 0;
            repeatCounter = 0;
            IsCompleted = false;
        }

        public virtual void Stop(bool notifiy = true)
        {
            if (!notifiy)
                IsCompleted = true;
            time = duration;
        }

        public virtual void Tick()
        {
            if (IsCompleted)
                return;
            if (source.time < LastTime)
                return;
            
            tickMethod(source, LastTime, ref _time);

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
                    LastTime = source.time;
                    time = 0;
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
                    return !timer.IsCompleted;
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