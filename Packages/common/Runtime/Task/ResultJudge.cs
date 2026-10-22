using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    /// <summary>
    /// Decides whether a job's End content means success, and owns the True, False and Completed events raised with
    /// that decision. An <see cref="AsyncTaskObject"/> awaits the judge in its judge field after End, keeps the
    /// result in <see cref="AsyncTaskObject.Outcome"/>, then calls <see cref="Report"/>.
    /// <para>
    /// Override <see cref="Judge"/> for a synchronous rule, or <see cref="JudgeAsync"/> for one that has to await
    /// (a second-stage check). Consumers write their own variants, for example one that parses a protocol message.
    /// </para>
    /// </summary>
    public abstract class ResultJudge : ScriptableObject
    {
        [SerializeField] private UnityEvent<string> _true = new UnityEvent<string>();
        [SerializeField] private UnityEvent<string> _false = new UnityEvent<string>();
        [SerializeField] private UnityEvent<bool> _completed = new UnityEvent<bool>();

        /// <summary>Raised with <see cref="JudgeResult.Content"/> when the result is a success.</summary>
        public UnityEvent<string> onTrue => _true;
        /// <summary>Raised with <see cref="JudgeResult.Content"/> when the result is a failure.</summary>
        public UnityEvent<string> onFalse => _false;
        /// <summary>Raised last, with success or failure; also raised with false when the job is canceled.</summary>
        public UnityEvent<bool> onCompleted => _completed;

        /// <summary>
        /// Judges <paramref name="input"/>, the job's End content. The default completes at once with
        /// <see cref="Judge"/>; override it when the decision has to await.
        /// </summary>
        public virtual Awaitable<JudgeResult> JudgeAsync(string input, CancellationToken token = default)
        {
            var source = new AwaitableCompletionSource<JudgeResult>();
            source.SetResult(Judge(input));
            return source.Awaitable;
        }

        /// <summary>The synchronous rule behind the default <see cref="JudgeAsync"/>.</summary>
        protected virtual JudgeResult Judge(string input)
            => throw new NotImplementedException($"{GetType().Name} overrides neither Judge nor JudgeAsync.");

        /// <summary>Raises True or False with the content, then Completed with the success flag.</summary>
        public void Report(JudgeResult result)
        {
            (result.Succeeded ? _true : _false)?.Invoke(result.Content);
            _completed?.Invoke(result.Succeeded);
        }

        /// <summary>Raises only Completed(false): the job was canceled or failed before it could be judged.</summary>
        public void ReportInterrupted() => _completed?.Invoke(false);
    }
}
