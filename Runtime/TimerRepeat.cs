//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace Yu5h1Lib
//{
//    public class TimerRepeat : Timer
//    {

//        public override void Tick()
//        {
//            if (IsCompleted)
//                return;
//            if (IsCompleting)
//            {
//                IsCompleted = true;
//                OnCompleted();
//                _Completed?.Invoke();
//            }
//            else if (TimesUp)
//            {
//                if (repeatType != RepeatType.None && repeatCounter != repeatCount)
//                {
//                    if (repeatCounter + 1 == repeatCount)
//                    {
//                        OnFinalRepetition();
//                        _FinalRepetition?.Invoke();
//                    }
//                    repeatCounter++;
//                    OnRepeat();
//                    _Repeated?.Invoke();
//                    LastTime = GetTime();
//                }
//            }
//            else
//                _Update?.Invoke();
//        }
//    }
//}
