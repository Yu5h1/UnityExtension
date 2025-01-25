using UnityEngine;

namespace Yu5h1Lib
{
    public class TimerBase
    {
        #region Caches
        public float LastTime { get; protected set; }
        #endregion
        #region Property
        protected virtual float GetTime() => Time.time;
        public float time => GetTime() - LastTime;
        #endregion

        public virtual void Start()
        {
            LastTime = GetTime();
        }
    }

}
