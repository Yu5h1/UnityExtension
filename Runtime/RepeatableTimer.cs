//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace Yu5h1Lib
//{
//    public class RepeatableTimer : Timer
//    {
//        //public virtual void Start()
//        //{
//        //    LastTime = GetTime() + delay;
//        //    repeatCounter = 0;
//        //    IsCompleted = false;
//        //}
//        //public virtual void Stop()
//        //    => LastTime = time - duration;

//        //public virtual void Tick()
//        //{
//        //    if (IsCompleted)
//        //        return;
//        //    if (IsCompleting)
//        //    {
//        //        IsCompleted = true;
//        //        OnCompleted();
//        //        Completed?.Invoke(this);
//        //    }
//        //    else if (TimesUp)
//        //    {
//        //        if (repeatType != RepeatType.None && repeatCounter != repeatCount)
//        //        {
//        //            if (repeatCounter + 1 == repeatCount)
//        //            {
//        //                OnFinalRepetition();
//        //                FinalRepetition?.Invoke(this);
//        //            }
//        //            repeatCounter++;
//        //            OnRepeat();
//        //            Repeated?.Invoke(this);
//        //            LastTime = GetTime();
//        //        }
//        //    }
//        //    else
//        //        Update?.Invoke(this);
//        //}
//        //protected virtual void OnRepeat() { }
//        //protected virtual void OnFinalRepetition() { }
//    }
//}
