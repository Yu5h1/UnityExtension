using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    public abstract class UI_DialogBase : UIControl
    {
        [Flags]
        public enum Style {
            Verbatim  = 1 << 0,
            Fade      = 1 << 1,
        }
        public Style style = Style.Verbatim;
        public string[] lines;
        public string Content
        {
            get => GetText();
            set
            {
                var previousContent = GetText();
                if (previousContent == value)
                    return;
                SetText(value);
                OnContentChanged(previousContent);
            }
        }
        protected abstract string GetText();
        protected abstract void SetText(string text);
        public Color color
        { 
            get => GetColor();
            set => SetColor(value);
        }
        protected abstract Color GetColor();
        protected abstract void SetColor(Color color);

        public abstract int GetLineCount();

        public float Delay;
        public float Speed = 0.05f;
        public int StepIndex { get; private set; }
        public bool IsPlaying { get; private set; }
        public bool GetIsPlaying() => IsPlaying;
        public bool NothingToSay => StepIndex >= lines.Length - 1 && !IsPlaying;
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
            this.StartCoroutine(ref performCoroutine, BuildPerformRoutine(lines[StepIndex = 0], Delay, Speed,style));
        }
        public virtual void Perform(string content, float? delay = null, float? speed = null,
            Style? style = null, bool append = false)
        {
            if (content.IsEmpty())
                return;
            delay ??= Delay;
            speed ??= Speed;
            style ??= this.style;

            lines = content.Split("\n\n");
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            this.StartCoroutine(ref performCoroutine, BuildPerformRoutine(lines[StepIndex = 0], delay.Value, speed.Value, style.Value, append));
        }
        public IEnumerator BuildPerformRoutine(string text, float delay = 0, float speed = 0.05f,
            Style style = Style.Verbatim, bool append = false)
        {
            if (delay > 0)
                yield return new WaitForSeconds(delay);
            if (speed == 0 || string.IsNullOrEmpty(text))
            {
                Content = text;
                yield break;
            }
            IsPlaying = true;
            
            Content = (append ? $"{Content}\n" : "") + (speed > 0 ? "" : text);
            
                

            timer.useUnscaledTime = useUnscaledTime;

            switch (style)
            {
                case Style.Verbatim:
                    timer.duration = speed;
                    if (speed > 0)
                        for (int i = 0; i < text.Length; i++)
                        {
                            var letter = text[i];
                            if (!IsPlaying)
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
                    break;
                case Style.Fade:
                    var from = color.SetAlpha(0);
                    var to = color.SetAlpha(1);
                    color = from;
                    timer.Start();
                    Content = text;
                    while (!timer.IsCompleted)
                    {
                        color = Color.Lerp(from, to, Mathf.PingPong(timer.normalized,0.5f));
                        timer.Tick();
                        yield return null;
                    }
                    break;
            }

            _PerformCompleted?.Invoke();
            IsPlaying = false;
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
            StartCoroutine(BuildPerformRoutine(lines[++StepIndex]));
            return true;
        }

        public void Skip()
        {
            if (!gameObject.activeSelf)
                return;
            if (IsPlaying)
                IsPlaying = false;
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
