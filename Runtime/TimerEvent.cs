//using UnityEngine;
//using UnityEngine.Events;

//namespace Yu5h1Lib
//{
//    public partial class Timer : ITimer
//    {
//        public class Event
//        {
//            [SerializeField]
//            private UnityEvent _Update = new UnityEvent();
//            public event UnityAction Update
//            {
//                add => _Update?.AddListener(value);
//                remove => _Update.RemoveListener(value);
//            }
//            [SerializeField]
//            private UnityEvent _Completed = new UnityEvent();
//            public event UnityAction Completed
//            {
//                add => _Completed?.AddListener(value);
//                remove => _Completed?.RemoveListener(value);
//            }
//            [SerializeField]
//            private UnityEvent _Repeated = new UnityEvent();
//            public event UnityAction Repeated
//            {
//                add => _Repeated.AddListener(value);
//                remove => _Repeated.RemoveListener(value);
//            }
//            [SerializeField]
//            private UnityEvent _FinalRepetition = new UnityEvent();
//            public event UnityAction FinalRepetition
//            {
//                add => _FinalRepetition.AddListener(value);
//                remove => _FinalRepetition.RemoveListener(value);
//            }


//        }

//    }
//}
