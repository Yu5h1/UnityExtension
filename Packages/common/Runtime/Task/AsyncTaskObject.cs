using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
	public abstract class AsyncTaskObject : BehaviourObject
	{
        public object parameter { get; set; }
        public object result { get; set; }
        public abstract bool CheckSucceeded();

        [SerializeField] private UnityEvent _begin;
        public UnityEvent beginCallBack => _begin;
        public event UnityAction begin
        {
            add => _begin.AddListener(value);
            remove => _begin.RemoveListener(value);
        }
        [SerializeField] private UnityEvent _end;
        public UnityEvent endCallBack => _end;
        public event UnityAction end
        {
            add => _end.AddListener(value);
            remove => _end.RemoveListener(value);
        }
        [SerializeField] private OutcomeEvent _resultRecived;
        public OutcomeEvent resultRecivedCallBack => _resultRecived;

        public virtual void Init() { }
        protected abstract IEnumerator Processing();
        protected virtual void OnEnter() {}
        public IEnumerator Execute()
        {
            OnEnter();
            _begin?.Invoke();
            yield return Processing();
            _end?.Invoke();
            OnExit();
        }
        protected virtual void OnExit() { }
    }
}