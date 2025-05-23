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
    public abstract class TweenBehaviour : BaseMonoBehaviour
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

        public virtual bool Kill()
        {
            if (!initialized)
                return false;
            tweener.Kill();
            tweener = null;
            return true;
        }
        [RuntimeInitializeOnLoadMethod]
        private static void StaticInitialize()
        {
            Application.wantsToQuit -= Application_wantsToQuit;
            Application.wantsToQuit += Application_wantsToQuit;
        }
        private static bool Application_wantsToQuit()
        {
            DOTween.KillAll(false);
            DOTween.Clear(true);
            return true;
        }
    }
    public abstract class TweenBehaviour<TComponent, TValue1, TValue2, TPlugOptions> : TweenBehaviour
        where TComponent : Component
        where TPlugOptions : struct, IPlugOptions
    {
        [SerializeField, ReadOnly]
        private TComponent _component;
        public virtual TComponent component { get => _component; private set => _component = value; }
        public virtual TComponent OverrideGetComponent() => null;

        protected TweenerCore<TValue1, TValue2, TPlugOptions> TweenerCore
        {
            get => (TweenerCore<TValue1, TValue2, TPlugOptions>)tweener;
            set => tweener = value;
        }

        [SerializeField, ContextMenuItem("Reset", nameof(ResetStartValue))]
        protected TValue2 _startValue;
        public TValue2 startValue
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
        protected TValue2 _endValue;
        public TValue2 endValue
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
        protected UnityEvent<TValue2> OnCompleteEvent;
        public event UnityAction<TweenBehaviour, TValue2> CompleteEvent;

        [SerializeField]
        protected UnityEvent<TValue2> OnRewindEvent;
        public event UnityAction<TweenBehaviour, TValue2> RewindEvent;

        protected internal override Tweener Create() => CreateTweenCore();
        protected abstract TweenerCore<TValue1, TValue2, TPlugOptions> CreateTweenCore();

        protected virtual void ResetStartValue() { "NotImplementedException ResetStartValue".printWarning(); }
        protected virtual void ResetEndValue() { "NotImplementedException ResetEndValue ".printWarning(); }

        protected override void OnInitializing()
        {
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
            TweenerCore.SetUpdate(updateType, isIndependentUpdate);

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
            if (IsAvailable())
                tweener.PlayForward();
        }
        public override bool Kill()
        {
            if (TweenerCore == null)
                return false;
            TweenerCore.onPlay -= OnPlay;
            TweenerCore.onComplete -= OnComplete;
            TweenerCore.onStepComplete -= OnLoop;
            TweenerCore.onRewind -= OnRewind;

            return base.Kill();
        }
        protected void OnRewind(TValue2 value)
        {
            OnRewindEvent?.Invoke(value);
            RewindEvent?.Invoke(this, value);
        }

        protected virtual void OnRewind()
            => OnRewind(TweenerCore.changeValue);

        protected virtual void OnLoop()
        {

        }
        protected void OnComplete(TValue2 value)
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

            //tweener.Pause();
            DOTween.Pause(component);

            //if (!playOnEnable && TweenerCore?.IsComplete() == true)
            //{
            //    tweener.Pause();
            //    tweener.Rewind();
            //}
        }
        protected void PlayTween()
        {
            if (!isActiveAndEnabled)
                return;
            tweener.PlayForward();
        }
        public void TryPlayTween()
        {
            if (TweenerCore.IsPlaying() || IsWaiting)
                return;
            PlayTween();
        }
        public void TryPlayTween(float delay)
        {
            if (!gameObject.activeSelf || !enabled || TweenerCore.IsPlaying() || IsWaiting)
                return;
            tweener.onComplete -= RestoreDefaultOnComplete;
            tweener.onComplete += RestoreDefaultOnComplete;
            tweener.SetDelay(delay);
            PlayTween();
        }
        void RestoreDefaultOnComplete()
        {
            tweener.SetDelay(Delay);
            tweener.onComplete = OnComplete;
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
            Kill();
        }
    }
    public abstract class TweenBehaviour<TComponent, TValue, TPlugOptions> : TweenBehaviour<TComponent, TValue, TValue, TPlugOptions>
                where TComponent : Component
                where TPlugOptions : struct, IPlugOptions
    {
    }
}