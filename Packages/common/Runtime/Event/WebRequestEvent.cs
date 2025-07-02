using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Yu5h1Lib
{
    [System.Serializable]
    public class WebRequestEvent
    {
        public interface IResult
        { 
            bool Succeeded { get; }
            string Message { get; }
        }
        [SerializeField] private string _command;
        public string command { get => _command; protected set => _command = value; }
        [SerializeField] private bool _enabled = true;
        public bool enabled => _enabled;

        public LifeCycleEvent lifeCycle;
        public ResultEvent _result;
        public void Begin() => lifeCycle.Begin();
        public void End() => lifeCycle.End();
        public void Report(bool result) => _result.Invoke(result);

        public UnityWebRequest request { get; set; }
        public Coroutine coroutine;

        public virtual IEnumerator Processing(UnityWebRequest request, Timer timer)
        {
            while (!request.isDone)
            {
                timer?.Tick();
                yield return null;
            }
        } 
        public virtual IEnumerator Finish<T>(T result) where T : IResult
        {
            yield break;
        }

        //public void Send<T>(ChatRequest chatRequest, T data, Timer timer)
        //{
        //    commander = chatRequest;
        //    chatRequest.StartCoroutine(ref coroutine,
        //        chatRequest.SendApiRequest(command, data, timer
        //        , callbacks, Finish, Processing));

        //}

    } 
}