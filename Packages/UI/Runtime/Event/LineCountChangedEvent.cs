using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib;
using Yu5h1Lib.UI;

public class LineCountChangedEvent : InlineEvent
{
    public TextAdapter textAdapter;
    #region Cache
    private int LineCountCache { get; set; }
    #endregion

    [SerializeField] private UnityEvent<int> _LineCountChanged;
    public event UnityAction<int> LineCountChanged
    {
        add => _LineCountChanged.AddListener(value);
        remove => _LineCountChanged.RemoveListener(value);
    }
    public override void Invoke()
    {
        var lineCount = textAdapter.GetLineCount();
        if (LineCountCache == lineCount)
            return;
        LineCountCache = lineCount;
        _LineCountChanged?.Invoke(lineCount);
    }
}
