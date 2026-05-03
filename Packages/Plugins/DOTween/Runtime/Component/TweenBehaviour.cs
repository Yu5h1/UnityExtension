using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;
using System.Collections;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using System;

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

        [ReadOnly]
        public bool isBackwards;

        public Tweener tweener { get; protected set; }
        public float normalizedTime => tweener.ElapsedPercentage();
        protected internal abstract Tweener Create();

        public abstract void Reset();

        public abstract void Play(bool forward, bool restart);

        [ContextMenu(nameof(Rewind))]
        public void Rewind() => RewindImplitation();
        protected abstract void RewindImplitation();

        [ContextMenu(nameof(PlayForward))]
        public void PlayForward() => Play(true, true);
        [ContextMenu(nameof(PlayBackwards))]
        public void PlayBackwards() => Play(false, true);




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
        public event UnityAction<TValue2> CompleteEvent
        {
            add => OnCompleteEvent.AddListener(value);
            remove => OnCompleteEvent.RemoveListener(value);
        }

        [SerializeField]
        protected UnityEvent<TValue2> OnRewindEvent;
        public event UnityAction<TValue2> Rewinded
        {
            add => OnRewindEvent.AddListener(value);
            remove => OnRewindEvent.RemoveListener(value);
        }

        protected internal override Tweener Create() => CreateTweenCore();
        protected abstract TweenerCore<TValue1, TValue2, TPlugOptions> CreateTweenCore();

        protected virtual void ResetStartValue() {   }
        protected virtual void ResetEndValue() { }

        protected override void OnInitializing()
        {
            "component does not exist !".printWarningIf(!(_component = 
                OverrideGetComponent() ?? GetComponent<TComponent>()));

            TweenerCore = CreateTweenCore();
            TweenerCore.Pause();

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
            TweenerCore.onComplete -= OnCompleted;
            TweenerCore.onComplete += OnCompleted;
            TweenerCore.onStepComplete += OnLoop;
            TweenerCore.onRewind += OnRewind;
            
            TweenerCore.SetAutoKill(false);
            if (EaseType != Ease.Unset)
                TweenerCore.SetEase(EaseType);
        }

        public override void Reset()
        {
            ResetStartValue();
            ResetEndValue();
            component = OverrideGetComponent() ?? GetComponent<TComponent>();
        }

        protected virtual void Start() {}
        protected virtual void OnEnable()
        {
            if (!playOnEnable)
                return;
            tweener.PlayForward();
        }
        public override bool Kill()
        {
            if (TweenerCore == null)
                return false;
            TweenerCore.onPlay -= OnPlay;
            TweenerCore.onComplete -= OnCompleted;
            TweenerCore.onStepComplete -= OnLoop;
            TweenerCore.onRewind -= OnRewind;

            return base.Kill();
        }
        protected void OnRewind(TValue2 value)
        {
            OnRewindEvent?.Invoke(value);
        }

        protected virtual void OnRewind()
            => OnRewind(TweenerCore.changeValue);

        protected override void RewindImplitation()
        {
            if (!isActiveAndEnabled)
                return;
            tweener.Rewind();
        }

        protected virtual void OnLoop()
        {

        }
        protected virtual void OnPlay()
            => _PlayEvent?.Invoke();

        protected virtual TValue2 CompleteValue => TweenerCore.endValue;

        private void OnCompleted()
            => OnCompleteEvent?.Invoke(CompleteValue);

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
        public override void Play(bool forward ,bool restart )
        {
            if (!isActiveAndEnabled)
                return;
            if (!restart && (TweenerCore.IsPlaying() || IsWaiting))
                return;

            if (forward)
                tweener.PlayForward();
            else
                tweener.PlayBackwards();
        }


        public void TryPlayTween()
        {
            if (TweenerCore.IsPlaying() || IsWaiting)
                return;
            Play(true,false);
        }
        public void TryPlayTween(float delay)
        {
            if (!gameObject.activeSelf || !enabled || TweenerCore.IsPlaying() || IsWaiting)
                return;
            //tweener.onComplete -= RestoreDefaultOnComplete;
            //tweener.onComplete += RestoreDefaultOnComplete;
            tweener.SetDelay(delay);
            Play(true, false);
        }

        void RestoreDefaultOnComplete()
        {
            tweener.SetDelay(Delay);
            tweener.onComplete = OnCompleted;
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