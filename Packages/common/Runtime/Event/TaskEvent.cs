namespace Yu5h1Lib
{
    [System.Serializable]
    public class TaskEvent : RouteBase
    {
        public LifeCycleEvent lifeCycle;
        public OutcomeEvent _result;

        public void Begin() => lifeCycle.Begin();
        public void End() => lifeCycle.End();
        public void Report(bool success) => _result.Invoke(success);
    }

    [System.Serializable]
    public class TaskEvent<T> : RouteBase
    {
        public LifeCycleEvent<T> lifeCycle;
        public OutcomeEvent<T> _result;

        public void Begin(T t) => lifeCycle?.Begin(t);
        public void End(T t) => lifeCycle?.End(t);
        public void Report(bool success, T t) => _result?.Invoke(success, t);
    } 
}

