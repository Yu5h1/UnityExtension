using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    /// <summary>
    /// An inline asynchronous job run with <see cref="Awaitable"/>. <see cref="ExecuteAsync"/> raises
    /// Begin, runs <see cref="Processing"/>, raises End with the content it returned, then awaits the optional
    /// <see cref="judge"/>, keeps its result in <see cref="Outcome"/> and lets the judge raise True or False and
    /// Completed:
    /// <code>Begin&lt;string&gt; → [Progress&lt;float&gt;] → End&lt;string&gt; → judge (awaited) → True&lt;string&gt;／False&lt;string&gt; → Completed&lt;bool&gt;</code>
    /// Without a judge only Begin and End are raised. A canceled run raises End and the judge's Completed(false),
    /// never calls the judge and never raises True or False.
    /// <para>
    /// <see cref="Awaitable"/> stays inside code: no serialized field or UnityEvent signature uses it, so the scene
    /// side only sees events.
    /// </para>
    /// </summary>
    public abstract class AsyncTaskObject : BehaviourObject
    {
        [SerializeField] private UnityEvent<string> _begin = new UnityEvent<string>();
        [SerializeField] private UnityEvent<string> _end = new UnityEvent<string>();
        [SerializeField, Inline] private ResultJudge _judge;

        /// <summary>Raised first, with <see cref="beginContent"/>.</summary>
        public UnityEvent<string> onBegin => _begin;
        /// <summary>Raised when the work stops, with its full content; with an empty string when it was interrupted.</summary>
        public UnityEvent<string> onEnd => _end;
        /// <summary>The optional judge that decides success from the End content and raises the result events.</summary>
        public ResultJudge judge
        {
            get => _judge;
            set => _judge = value;
        }

        public bool IsRunning { get; private set; }
        /// <summary>Whether the last run was canceled.</summary>
        public bool IsCanceled { get; private set; }
        /// <summary>The last run's result; a failure with no content after a canceled or failed run.</summary>
        public JudgeResult Outcome { get; private set; }

        /// <summary>The string raised with Begin. Defaults to the object's name.</summary>
        protected virtual string beginContent => name;

        /// <summary>The work itself. Returns the full content raised with End and judged afterwards.</summary>
        protected abstract Awaitable<string> Processing(CancellationToken token);

        /// <summary>
        /// Runs the job and returns whether it succeeded. Cancellation through <paramref name="token"/> returns
        /// false without throwing; any other exception from the work raises End and the judge's Completed(false)
        /// first, then propagates.
        /// </summary>
        public async Awaitable<bool> ExecuteAsync(CancellationToken token = default)
        {
            var activeJudge = _judge;
            IsRunning = true;
            IsCanceled = false;
            Outcome = default;
            OnEnter();
            _begin?.Invoke(beginContent);
            string content;
            try
            {
                token.ThrowIfCancellationRequested();
                content = await Processing(token);
                token.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                _end?.Invoke(string.Empty);
                return Interrupt(activeJudge, true);
            }
            catch
            {
                _end?.Invoke(string.Empty);
                Interrupt(activeJudge, false);
                throw;
            }
            _end?.Invoke(content);
            JudgeResult outcome;
            try
            {
                outcome = await JudgeAsync(activeJudge, content, token);
            }
            catch (OperationCanceledException)
            {
                return Interrupt(activeJudge, true);
            }
            catch
            {
                Interrupt(activeJudge, false);
                throw;
            }
            Outcome = outcome;
            OnJudged(outcome);
            if (activeJudge != null)
                activeJudge.Report(outcome);
            Finish();
            return outcome.Succeeded;
        }

        /// <summary>
        /// Decides the result from the End content. The default asks <paramref name="judge"/>, or succeeds when
        /// there is none.
        /// </summary>
        protected virtual async Awaitable<JudgeResult> JudgeAsync(ResultJudge judge, string content, CancellationToken token)
            => judge == null ? new JudgeResult(true, content) : await judge.JudgeAsync(content, token);

        /// <summary>Called before Begin.</summary>
        protected virtual void OnEnter() { }
        /// <summary>Called after judging and before the judge raises True or False and Completed.</summary>
        protected virtual void OnJudged(JudgeResult outcome) { }
        /// <summary>Called last, after every event of the run, including canceled and failed runs.</summary>
        protected virtual void OnExit() { }

        private bool Interrupt(ResultJudge activeJudge, bool canceled)
        {
            IsCanceled = canceled;
            if (activeJudge != null)
                activeJudge.ReportInterrupted();
            Finish();
            return false;
        }

        private void Finish()
        {
            IsRunning = false;
            OnExit();
        }
    }
}
