using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    /// <summary>
    /// An <see cref="AsyncProgressObject"/> that loads one <typeparamref name="T"/> (a GameObject, AudioClip,
    /// Texture…). Begin and End carry <see cref="key"/>, such as an address or a path. The run succeeds when an object
    /// was loaded; a judge in the judge field can still reject a loaded object, but never accepts a missing one.
    /// On success the object is in <see cref="Result"/> and raised through Loaded before the judge's events.
    /// </summary>
    public abstract class AsyncLoadObject<T> : AsyncProgressObject where T : Object
    {
        [SerializeField] private UnityEvent<T> _loaded = new UnityEvent<T>();

        private T _candidate;

        /// <summary>Raised with the loaded object when the run succeeds.</summary>
        public UnityEvent<T> onLoaded => _loaded;
        /// <summary>The object of the last successful run; null after a run that loaded nothing or was rejected.</summary>
        public T Result { get; private set; }

        /// <summary>What to load, such as an address or a path; raised with Begin and End.</summary>
        public abstract string key { get; }

        protected override string beginContent => key;

        /// <summary>Loads the object, or returns null when there is none. Report progress from here.</summary>
        protected abstract Awaitable<T> Load(CancellationToken token);

        protected sealed override async Awaitable<string> Processing(CancellationToken token)
        {
            _candidate = await Load(token);
            return key;
        }

        protected override async Awaitable<JudgeResult> JudgeAsync(ResultJudge judge, string content, CancellationToken token)
            => _candidate == null ? new JudgeResult(false, content) : await base.JudgeAsync(judge, content, token);

        protected override void OnEnter()
        {
            Result = null;
            _candidate = null;
            base.OnEnter();
        }

        protected override void OnJudged(JudgeResult outcome)
        {
            base.OnJudged(outcome);
            if (!outcome.Succeeded)
                return;
            Result = _candidate;
            _loaded?.Invoke(Result);
        }

        protected override void OnExit()
        {
            _candidate = null;
            base.OnExit();
        }
    }
}
