using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    public abstract class UI_DialogBase : UIControl
    {
        public string[] lines;
        public string Content
        {
            get => GetText();
            set
            {
                var previouseContent = GetText();
                if (previouseContent == value)
                    return;
                SetText(value);
                OnContentChanged(previouseContent);
            }
        }
        protected abstract string GetText();
        protected abstract void SetText(string text);

        public abstract int GetLineCount();

        public float Delay;
        public float Speed = 0.05f;
        public int StepIndex { get; private set; }
        public bool IsPerforming { get; private set; }
        public bool NothingToSay => StepIndex >= lines.Length - 1 && !IsPerforming;
        public bool PerformOnEnable = true;


        [SerializeField]
        private UnityEvent _Skiped;
        public event UnityAction Skiped
        {
            add => _Skiped.AddListener(value);
            remove => _Skiped.RemoveListener(value);
        }
        [SerializeField]
        private UnityEvent _DialogOver;
        public event UnityAction DialogOver
        {
            add => _DialogOver.AddListener(value);
            remove => _DialogOver.RemoveListener(value);
        }

        private Timer timer = new Timer();
        [SerializeField]
        private bool useUnscaledTime = true;
        private Timer.Wait<Timer> waiter;

        [SerializeField]
        private UnityEvent _PerformBegin;
        public event UnityAction PerformBegin
        {
            add => _PerformBegin.AddListener(value);
            remove => _PerformBegin.RemoveListener(value);
        }
        [SerializeField]
        private UnityEvent _PerformCompleted = default!;
        public event UnityAction PerformCompleted
        {
            add => _PerformCompleted.AddListener(value);
            remove => _PerformCompleted.RemoveListener(value);
        }

        [SerializeField]
        private UnityEvent<string> _ContentChanged;
        public event UnityAction<string> ContentChanged
        {
            add => _ContentChanged.AddListener(value);
            remove => _ContentChanged.RemoveListener(value);
        }
        [SerializeField]
        private UnityEvent<int> _LineCountChangedEvent;
        public event UnityAction<int> LineCountChangedEvent
        {
            add => _LineCountChangedEvent.AddListener(value);
            remove => _LineCountChangedEvent.RemoveListener(value);
        }

        private Coroutine performCoroutine;

        #region Cache
        private int LineCountCache { get; set; }
        #endregion

        protected override void Reset()
        {
            base.Reset();
        }
        protected override void Awake()
        {
            base.Awake();
        }
        protected virtual void Start()
        {
            timer.duration = Speed;
            waiter = timer.Waiting();
        }
        private void OnEnable()
        {
            if (lines.IsEmpty())
                return;
            if (PerformOnEnable)
                Perform();
        }
        private void OnDisable()
        {

        }
        private void Perform()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            this.StartCoroutine(ref performCoroutine, PerformVerbatimProcess(lines[StepIndex = 0], Delay, Speed));
        }
        public virtual void Perform(string content, float delay, float speed)
        {
            lines = content.Split("\n\n");
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            this.StartCoroutine(ref performCoroutine, PerformVerbatimProcess(lines[StepIndex = 0], delay, speed));
        }
        public virtual void Perform(string content)
        {
            lines = content.Split("\n\n");
            Perform();
        }
        public IEnumerator PerformVerbatimProcess(string text, float delay = 0, float speed = 0.05f)
        {
            if (delay > 0)
                yield return new WaitForSeconds(delay);
            if (speed == 0 || string.IsNullOrEmpty(text))
            {
                Content = text;
                yield break;
            }
            IsPerforming = true;
            Content = speed > 0 ? "" : text;
            timer.useUnscaledTime = useUnscaledTime;
            if (speed > 0)
                for (int i = 0; i < text.Length; i++)
                {
                    var letter = text[i];
                    if (!IsPerforming)
                    {
                        Content = text;
                        _Skiped?.Invoke();
                        _PerformCompleted?.Invoke();
                        yield break;
                    }
                    Content += letter;

                    _PerformBegin?.Invoke();
                    timer.Start();
                    yield return waiter;
                }

            _PerformCompleted?.Invoke();
            IsPerforming = false;
        }
        public void PerformThinking()
        {
            Perform("......");
        }
        #region Event

        private void OnContentChanged(string oldContent)
        {
            _ContentChanged?.Invoke(oldContent);
            var newLineCount = GetLineCount();
            if (LineCountCache != newLineCount)
            {
                OnLineCountChanged(LineCountCache);
                LineCountCache = newLineCount;
            }
        }
        private void OnLineCountChanged(int oldCount)
        {
            _LineCountChangedEvent?.Invoke(oldCount);
        }
        #endregion
        #region Action
        public bool Next()
        {
            if (NothingToSay)
                return false;
            StartCoroutine(PerformVerbatimProcess(lines[++StepIndex]));
            return true;
        }

        public void Skip()
        {
            if (!gameObject.activeSelf)
                return;
            if (IsPerforming)
                IsPerforming = false;
            else if (!Next())
                _DialogOver?.Invoke();
        }
        public void RefreshLayout(VerticalLayoutGroup verticalLayoutGroup)
        {

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)verticalLayoutGroup.transform);
            verticalLayoutGroup.CalculateLayoutInputHorizontal();
            verticalLayoutGroup.CalculateLayoutInputVertical();
            verticalLayoutGroup.SetLayoutHorizontal();
            verticalLayoutGroup.SetLayoutVertical();
        }
        #endregion
        public void AddLinesFromContent()
        {
            var newIndex = lines.Length;
            System.Array.Resize(ref lines, newIndex + 1);
            lines[newIndex] = Content;
        }
    } 
}
