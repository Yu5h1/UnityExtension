using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
	public class TimerBehaviour : MonoBehaviour
	{
        [SerializeField] private Timer timer;

        [SerializeField] private UnityEvent<Timer> _Repeated = new UnityEvent<Timer>();
        [SerializeField] private UnityEvent<Timer> _FinalRepetition = new UnityEvent<Timer>();
        [SerializeField] private UnityEvent<Timer> _Completed = new UnityEvent<Timer>();

        private void Awake()
        {
            Register();
        }
        public void Start()
        {
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
        private void OnCompleted(Timer t) => _Completed?.Invoke(t);
        private void OnRepeated(Timer t) => _Repeated?.Invoke(t);
        private void OnFinalRepetition(Timer t) => _FinalRepetition?.Invoke(t);
        

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
