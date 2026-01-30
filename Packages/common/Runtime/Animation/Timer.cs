using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting.APIUpdating;

namespace Yu5h1Lib.Behaviours
{
    using CoreTimer = Yu5h1Lib.Timer;
    [MovedFrom("TimerBehaviour")]
	public class Timer : MonoBehaviour
	{
        [SerializeField,AlwaysExpanded] private CoreTimer timer;

        [SerializeField] private UnityEvent<CoreTimer> _Repeated = new UnityEvent<CoreTimer>();
        [SerializeField] private UnityEvent<CoreTimer> _FinalRepetition = new UnityEvent<CoreTimer>();
        [SerializeField] private UnityEvent<CoreTimer> _Completed = new UnityEvent<CoreTimer>();

        private void Awake()
        {
            Register();
        }
        public void Start()
        {
            if (!enabled)
                enabled = true;
            timer.Start();
        }
        private void OnEnable()
        {
            Start();
        }
        private void Update()
        {
            timer.Tick();
        }
        private void OnDisable()
        {
       
        }
        private void OnCompleted(CoreTimer t) => _Completed?.Invoke(t);
        private void OnRepeated(CoreTimer t) => _Repeated?.Invoke(t);
        private void OnFinalRepetition(CoreTimer t) => _FinalRepetition?.Invoke(t);
        
        public void Stop(bool notify) => timer.Stop(notify);
        [ContextMenu(nameof(Stop))]
        public void Stop() => timer.Stop(true);


        public void Register()
        {
            Unregister();
            timer.Completed += OnCompleted;
            timer.Repeated += OnRepeated;
            timer.FinalRepetition += OnFinalRepetition;
        }
        public void Unregister()
        {
            timer.Completed -= OnCompleted;
            timer.Repeated -= OnRepeated;
            timer.FinalRepetition -= OnFinalRepetition;            
        }


        public void Log(string message) => message.print();
    } 
}
