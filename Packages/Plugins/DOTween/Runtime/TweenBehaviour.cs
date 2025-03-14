using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;
using System.Collections;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using System;

#if UNITY_EDITOR
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DOTweenBehaviour.Editor")]
#endif
namespace Yu5h1Lib
{
    public abstract class TweenBehaviour : MonoBehaviour
    {
        public float Delay;
        public float Duration = 1;
        public int LoopCount = 0;
        public LoopType LoopType = LoopType.Yoyo;
        public Ease EaseType;
        public DG.Tweening.UpdateType updateType;
        public bool isIndependentUpdate;
        public bool playOnEnable = true;
        public bool RewindOnDisable = true;
        public bool UseUnscaledTime = false;
        public bool IsWaiting { get; protected set; }

        public Tweener tweener { get; protected set; }
        public float normalizedTime => tweener.ElapsedPercentage();
        protected internal abstract Tweener Create();
        public bool IsInitinalized => tweener != null && DOTween.IsTweening(tweener);
        protected internal abstract void Init();
        public void Kill()
        {
            if (!IsInitinalized)
                return;
            tweener.Kill();
            tweener = null;
        }
        [RuntimeInitializeOnLoadMethod]
        private static void Initinalize()
        {
            Application.wantsToQuit -= Application_wantsToQuit;
            Application.wantsToQuit += Application_wantsToQuit;
        }
        private static bool Application_wantsToQuit()
        {
            DOTween.Clear(true);
            return true;
        }
    }
    public abstract class TweenBehaviour<TComponent, T1, T2, TPlugOptions> : TweenBehaviour
        where TComponent : Component
        where TPlugOptions : struct, IPlugOptions
    {
        [SerializeField, ReadOnly]
        protected TComponent _component;
        public virtual TComponent component => _component;
        public virtual TComponent OverrideGetComponent() => null;

        protected TweenerCore<T1, T2, TPlugOptions> TweenerCore
        {
            get => (TweenerCore<T1, T2, TPlugOptions>)tweener;
            set => tweener = value;
        }

        [SerializeField, ContextMenuItem("Reset", nameof(ResetStartValue))]
        protected T2 _startValue;
        public T2 startValue
        {
            get => _startValue;
            protected set
            {
                _startValue = value;
                if (TweenerCore != null)
                    TweenerCore.startValue = value;
            }
        }

        [SerializeField]
        protected bool ChangeStartValue;

        [SerializeField]
        [ContextMenuItem("Reset", nameof(ResetEndValue))]
        protected T2 _endValue;
        public T2 endValue
        {
            get => _endValue;
            internal set
            {
                _endValue = value;
                if (TweenerCore != null)
                    TweenerCore.endValue = value;
            }
        }

        [SerializeField]
        private UnityEvent _PlayEvent;
        public event UnityAction playEvent
        {
            add => _PlayEvent.AddListener(value);
            remove => _PlayEvent.RemoveListener(value);
        }

        [SerializeField]
        protected UnityEvent<T2> OnCompleteEvent;
        public event UnityAction<TweenBehaviour, T2> CompleteEvent;

        [SerializeField]
        protected UnityEvent<T2> OnRewindEvent;
        public event UnityAction<TweenBehaviour, T2> RewindEvent;

        protected internal override Tweener Create() => CreateTweenCore();
        protected abstract TweenerCore<T1, T2, TPlugOptions> CreateTweenCore();

        protected virtual void ResetStartValue() { "NotImplementedException ResetStartValue".printWarning(); }
        protected virtual void ResetEndValue() { "NotImplementedException ResetEndValue ".printWarning(); }

        protected internal override void Init()
        {
            if (IsInitinalized)
                return;
            "component does not exist !".printWarningIf(!(_component = 
                OverrideGetComponent() ?? GetComponent<TComponent>()));

            TweenerCore = CreateTweenCore();
            if (ChangeStartValue)
                TweenerCore.ChangeStartValue(startValue);
            if (UseUnscaledTime)
                TweenerCore.SetUpdate(UseUnscaledTime);
            if (LoopCount != 0)
                TweenerCore.SetLoops(LoopCount, LoopType);
            if (!playOnEnable)
                TweenerCore.Pause();
            if (Delay > 0)
                TweenerCore.SetDelay(Delay);
            if (updateType != DG.Tweening.UpdateType.Normal)
                TweenerCore.SetUpdate(updateType);
            if (isIndependentUpdate)
                TweenerCore.SetUpdate(isIndependentUpdate);

            TweenerCore.onPlay += OnPlay;
            TweenerCore.onComplete += OnComplete;
            TweenerCore.onStepComplete += OnLoop;
            TweenerCore.onRewind += OnRewind;
            TweenerCore.SetAutoKill(false);
            if (EaseType != Ease.Unset)
                TweenerCore.SetEase(EaseType);
        }

        protected virtual void Start()
        {
            Init();
        }
        protected virtual void OnEnable()
        {
            Init();
            if (!playOnEnable)
                return;
            PlayTween();
        }
        protected void OnRewind(T2 value)
        {
            OnRewindEvent?.Invoke(value);
            RewindEvent?.Invoke(this, value);
        }

        protected virtual void OnRewind()
            => OnRewind(TweenerCore.changeValue);

        protected virtual void OnLoop()
        {

        }
        protected void OnComplete(T2 value)
        {
            OnCompleteEvent?.Invoke(value);
            CompleteEvent?.Invoke(this, TweenerCore.changeValue);
        }
        protected virtual void OnPlay()
            => _PlayEvent?.Invoke();
        protected virtual void OnComplete()
            => OnComplete(TweenerCore.changeValue);

        private void OnDisable()
        {
            if (RewindOnDisable)
                tweener.Rewind();
            else
                tweener.Pause();

            //Kill();

            //if (!playOnEnable && TweenerCore?.IsComplete() == true)
            //{
            //    tweener.Pause();
            //    tweener.Rewind();
            //}
        }
        protected void PlayTween()
        {
            if (!gameObject.activeSelf || !enabled)
                return;
            tweener.PlayForward();
        }
        public void TryPlayTween()
        {
            if (!gameObject.activeSelf || !enabled || TweenerCore.IsPlaying() || IsWaiting)
                return;
            PlayTween();
        }
        public void TryPlayTween(float delay)
        {
            if (!gameObject.activeSelf || !enabled || TweenerCore.IsPlaying() || IsWaiting)
                return;
            tweener.onComplete += RevertToOriginal;
            tweener.SetDelay(delay);
            PlayTween();
        }
        void RevertToOriginal()
        {
            tweener.SetDelay(Delay);
            tweener.onComplete = RevertToOriginal;
        }
        Coroutine coroutine;
        public void TryPlayBackwards(float delay)
        {
            if (!isActiveAndEnabled || TweenerCore.IsPlaying() || IsWaiting)
                return;
            if (coroutine != null)
                StopCoroutine(coroutine);
            coroutine = StartCoroutine(EnumPlayBackwards(delay));
        }
        IEnumerator EnumPlayBackwards(float delay)
        {
            IsWaiting = true;
            yield return new WaitForSeconds(delay);
            TweenerCore.PlayBackwards();
            IsWaiting = false;
        }
        private void OnDestroy()
        {
            TweenerCore.Kill();
        }
    }
    public abstract class TweenBehaviour<TComponent, TValue, TPlugOptions> : TweenBehaviour<TComponent, TValue, TValue, TPlugOptions>
                where TComponent : Component
                where TPlugOptions : struct, IPlugOptions
    {
    }
}