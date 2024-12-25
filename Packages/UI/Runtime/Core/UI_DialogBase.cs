using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Yu5h1Lib;

public abstract class UI_DialogBase : MonoBehaviour
{
    public string[] lines;
    public string Content 
    {
        get => GetText();
        set {
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

    public float Speed = 0.05f;
    public int StepIndex { get; private set; }
    public bool IsPerforming { get; private set; }
    public bool NothingToSay => StepIndex >= lines.Length - 1 && !IsPerforming;
    public bool PerformOnEnable = true;


    [SerializeField]
    private UnityEvent OnSkip;
    [SerializeField]
    private UnityEvent OnNext;

    private Timer timer = new Timer();
    private Timer.Wait<Timer> wait;

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

    private IEnumerator lastCoroutine;

    #region Cache
    private int LineCountCache { get; set; }
    #endregion

    protected virtual void Start()
    {
        timer.duration = Speed;
        wait = timer.Waiting();

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
        if (lastCoroutine != null)
            StopCoroutine(lastCoroutine);        
        StartCoroutine(lastCoroutine = PerformVerbatimProcess(lines[StepIndex = 0]));
    }
    public virtual void PerformVerbatim(string content)
    {
        lines = new string[] { content };
        Perform();
    }

    IEnumerator PerformVerbatimProcess(string text)
    {
        if (Speed == 0 || string.IsNullOrEmpty(text))
        {
            Content = text;
            yield break;
        }
        IsPerforming = true;
        Content = Speed > 0 ? "" : text;

        if (Speed > 0)
            for (int i = 0; i < text.Length; i++)
            {
                var letter = text[i];
                if (!IsPerforming)
                {
                    Content = text;
                    OnSkip?.Invoke();
                    _PerformCompleted?.Invoke();
                    yield break;
                }
                Content += letter;
                
                _PerformBegin?.Invoke();
                timer.Start();
                yield return wait;
            }

        _PerformCompleted?.Invoke();
        IsPerforming = false;
    }

    public void PerformThinking()
    {
        PerformVerbatim("......");
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
            OnNext?.Invoke();
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
    public void Log(string msg) => msg.print();
}
