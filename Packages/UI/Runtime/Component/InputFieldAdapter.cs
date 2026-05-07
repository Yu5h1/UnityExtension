using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Yu5h1Lib.MVVM;

namespace Yu5h1Lib.UI
{
    public class InputFieldAdapter : UI_Adapter<IInputFieldOps>, IInputFieldOps
        , IScrollHandler
    {
        [SerializeField] private Toggle _PasswordMaskToggle;
        public bool showPasswordMaskToggle
        {
            get => _PasswordMaskToggle?.gameObject?.activeInHierarchy == true;
            set => _PasswordMaskToggle?.gameObject?.SetActive(value);
        }
        #region ValuePort
        public event UnityAction<string> ChangedCallback
        {
            add => adapter.ChangedCallback += value;
            remove => adapter.ChangedCallback -= value;
        }
        public string GetFieldName() => adapter.GetFieldName();
        public string GetValue() => adapter.GetValue();
        public void SetValue(string value) => adapter.SetValue(value);
        public void SetValue(IValuePort Ibindable) => adapter.SetValue(Ibindable);
        public void BindTo(IDataView other) => adapter.BindTo(other);
        public void Unbind() => adapter.Unbind();
        public string Get() => adapter.Get();
        public void Set(string value) => adapter.Set(value);
        #endregion
        #region Ops 
        public string text { get => adapter.text; set => adapter.text = value; }
        public string placeholder { get => adapter.placeholder; set => adapter.placeholder = value; }
        public Component textComponent => adapter.textComponent;
        public TextAdapter textAdapter => adapter.textAdapter;
        public int lineCount => adapter.lineCount;
        public int lineType { get => adapter.lineType; set => adapter.lineType = value; } 
        public bool MaskPassword { get => adapter.MaskPassword; set => adapter.MaskPassword = value; }
        public bool isFocused => adapter.isFocused;
        public int characterLimit { get => adapter.characterLimit; set => adapter.characterLimit = value; }
        public void SetTextWithoutNotify(string value) => adapter.SetTextWithoutNotify(value);
        public void DeactivateInputField() => adapter.DeactivateInputField();        
        public int caretPosition { get => adapter.caretPosition; set => adapter.caretPosition = value; }
        public int selectionAnchorPosition { get => adapter.selectionAnchorPosition; set => adapter.selectionAnchorPosition = value; }
        public int selectionFocusPosition { get => adapter.selectionFocusPosition; set => adapter.selectionFocusPosition = value; }

        public void ActivateInputField() => adapter.ActivateInputField();
        public void ActivateInputField(int caret)
        {
            ActivateInputField();
            caretPosition = caret;
            selectionAnchorPosition = caret;
            selectionFocusPosition = caret;
        }
        
        #endregion
        [SerializeField] private bool _allowSubmit = true;
        public bool allowSubmit { get => _allowSubmit; set => _allowSubmit = value; }
        public string value { get => adapter.value; set => adapter.value = value; }

        [SerializeField] private bool autoWrappingOverflowMode;
        [SerializeField] private bool DontSubmitIfIsNullOrWhiteSpace;

        public UnityEvent<string> _submit;
        public event UnityAction<string> submit
        {
            add => adapter.submit += value;
            remove => adapter.submit -= value;
        }

        public event UnityAction<string> textChanged
        {
            add => adapter.textChanged += value;
            remove => adapter.textChanged -= value;
        }

        public event UnityAction<string> endEdit
        {
            add => adapter.endEdit += value;
            remove => adapter.endEdit -= value;
        }

 

        public void Select()
        { 
            if (IsAvailable())
                selectable.Select();
        }

        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private int visibleLineCount = 5;
        public bool AutoResizeScrollRect = true;

        public bool EnableScrollToCaret = true;

        private Vector2Int VisibleLineArea;
        private int lastCaretPosition = -1;
        private int lastSelectionStart = -1;
        private int lastSelectionEnd = -1;
        private int lastLineCount = -1;

        protected override void OnInitializing()
        {
            base.OnInitializing();

            submit += OnSubmit;
            textChanged += TextChanged;
            VisibleLineArea = new Vector2Int(0, visibleLineCount-1);
            scrollRect = GetComponentInParent<ScrollRect>();

        }
        private void Start()
        {
            ((RectTransform)rectTransform.parent).ForceRebuildLayoutImmediate();
            if (AutoResizeScrollRect && scrollRect != null )
                ResizeScrollRect();
 
        }
        private void OnEnable()
        {
            if (autoWrappingOverflowMode)
                textAdapter.SetWrappingOverflowMode(lineType > 0);
        }

        void Update()
        {
            if (EnableScrollToCaret)
                CheckCaretPositionChange();
#if UNITY_STANDALONE || UNITY_PLATFORM_STANDALONE_WIN || UNITY_EDITOR
            if ( lineType == 2 &&
             !Application.isMobilePlatform && Application.platform != RuntimePlatform.WebGLPlayer &&
             (InputHandler.GetKeyDown(KeyCode.Return) || InputHandler.GetKeyDown(KeyCode.KeypadEnter)))
                HandleEnterKey(true);
#endif
        }
        private void HandleEnterKey(bool shiftNewLine)
        {
            if (!isFocused || lineType != 2)
                return;
            
            if (IsShiftPressed())
            {
                if (shiftNewLine)
                    InsertNewline();
                return;
            }
            if (IsCaretNextToNewLine())
                InvokeSubmitEvent();
        }
        public bool IsCaretNextToNewLine()
        {
            var caretPos = caretPosition;
            return caretPos > 0 && text[caretPos - 1] == '\n';
        }
        //private void OnValidate()
        //{
        //    if (Application.isPlaying)
        //        return;
        //    if (scrollRect == null)
        //        scrollRect = GetComponentInParent<ScrollRect>();
        //    CheckLineCount();
        //}
        void CheckCaretPositionChange()
        {
            if ( scrollRect == null || Raw == null || lineType == 0)
                return;

            int currentCaret = caretPosition;
            int currentSelectionStart = Mathf.Min(selectionAnchorPosition, selectionFocusPosition);
            int currentSelectionEnd = Mathf.Max(selectionAnchorPosition, selectionFocusPosition);

            if (currentCaret != lastCaretPosition ||
                currentSelectionStart != lastSelectionStart ||
                currentSelectionEnd != lastSelectionEnd)
            {
                OnCaretPositionChanged();
                lastCaretPosition = currentCaret;
                lastSelectionStart = currentSelectionStart;
                lastSelectionEnd = currentSelectionEnd;
            }
        }

        void OnCaretPositionChanged()
        {
            var caretlineIndex = textAdapter.GetLineIndexByPosition(selectionFocusPosition);

            if (caretlineIndex.Between(VisibleLineArea.x, VisibleLineArea.y))
                return;

            UpdateVisibleLineArea(caretlineIndex);
            ScrollToLine(VisibleLineArea.x);
        }
        public void ScrollToCaretPosition()
            => ScrollToLine(textAdapter.GetLineIndexByPosition(selectionFocusPosition));
        public void ScrollToLine(int lineIndex)
        {
            var lineY = textAdapter.GetLineY(lineIndex, true);
            var v = textAdapter.transform.TransformPoint(0, lineY + 10, 0);
            scrollRect.ScrollToPosition(v);
        }

        void UpdateVisibleLineArea(int caretlineIndex)
        {
            var lastCaretlineIndex = textAdapter.GetLineIndexByPosition(lastCaretPosition);

            var dir = caretlineIndex > lastCaretlineIndex ? 1 : -1;

            if (dir > 0)
            {
                var min = caretlineIndex - visibleLineCount + 1;
                VisibleLineArea = new Vector2Int(min < 0 ? 0 : min, caretlineIndex);
            }
            else
            {
                var max = caretlineIndex + visibleLineCount - 1;
                VisibleLineArea = new Vector2Int(caretlineIndex, max > lineCount ? lineCount : max);
            }
        }
        private bool IsShiftPressed()
        {
            if (Application.isMobilePlatform )
                return false;
            bool result = InputHandler.GetKey(KeyCode.LeftShift) || InputHandler.GetKey(KeyCode.RightShift);
 
            return result;
        }

        private void OnSubmit(string txt)
        {
            // multiline submit
            if (lineType == 1 && IsShiftPressed())
            {
                int caretPos = caretPosition;
                text = text.Insert(caretPos, "\n");
                ActivateInputField(caretPos + 1);
                return;
            }
            if (!allowSubmit)
                return;
            if (lineType == 2)
                return;
            if (DontSubmitIfIsNullOrWhiteSpace && txt.IsEmpty())
                return;
            _submit?.Invoke(txt);
        }
        private void TextChanged(string txt)
        {
            if (textComponent == null)
                return;

            if (isActiveAndEnabled)
                this.DelayInvoke(CheckLineCount, 1);
            else
                CheckLineCount();
        }

        public void CheckLineCount()
        {
            if (!AutoResizeScrollRect)
                return;
            if (lastLineCount != lineCount)
            {
                OnLineCountChanged(lineCount);
                lastLineCount = lineCount;
            }
//#if UNITY_WEBGL
//            if (!Application.isMobilePlatform )
//                ScrollToLine(VisibleLineArea.x);
//#endif
        }
        public void OnLineCountChanged(int count)
        {
            ((RectTransform)rectTransform.parent).ForceRebuildLayoutImmediate();
            if (scrollRect == null)
                return;
            var caretlineIndex = textAdapter.GetLineIndexByPosition(selectionFocusPosition);

            if (!AutoResizeScrollRect)
                return;
            UpdateVisibleLineArea(caretlineIndex);
            ResizeScrollRect();
        }
        private void ResizeScrollRect()
        {
            var dynamicLineCount = Mathf.Clamp(Mathf.Min(visibleLineCount, lineCount), 1, visibleLineCount);
            if (lastLineCount != dynamicLineCount)
            {
                var rt = (RectTransform)scrollRect.transform;
                var sizeDelta = rt.sizeDelta;
                sizeDelta.y = (dynamicLineCount * textAdapter.GetBaseLineHeight()) + 20;
                rt.sizeDelta = sizeDelta;
            }
        }
        public void Submit() => TriggerEvent(ExecuteEvents.submitHandler);

        public void InvokeSubmitEvent() 
        {
            if (!allowSubmit || !IsAvailable())
                return;
            _submit?.Invoke(text);        
        }
        public void InvokeSubmitEventAfterFrames(int frames) => this.DelayInvoke(InvokeSubmitEvent, frames);

        public void OnScroll(PointerEventData eventData)
        {
            scrollRect?.OnScroll(eventData);
        }

        public void InsertNewline()
        {
            int caretPos = caretPosition;
            string currentText = text;

            string newText = currentText.Insert(caretPos, "\n");
            text = newText;

            ActivateInputField();

            caretPos += 1;
            caretPosition = caretPos;
            selectionAnchorPosition = caretPos;
            selectionFocusPosition = caretPos;
        }
        public void AppendNewLine(string content) => Append($"\n{content}");
        public void ScrollToBottom()
        {
            if (scrollRect == null)
                return;
            ((RectTransform)scrollRect.transform).ForceRebuildLayoutImmediate();
            scrollRect.verticalNormalizedPosition = 0;
        }

        public void Append(string content) => text = text + content;
  

        //public bool DisableIMEOnSelect;
        //public static IMECompositionMode? forerlyMode;
        //public override void OnSelect(BaseEventData eventData)
        //{
        //    base.OnSelect(eventData);
        //    if (DisableIMEOnSelect)
        //    {
        //        forerlyMode = Input.imeCompositionMode;
        //        Input.imeCompositionMode = IMECompositionMode.On;
        //    }

        //}
        //public override void OnDeselect(BaseEventData eventData)
        //{
        //    base.OnDeselect(eventData);
        //    //if (DisableIMEOnSelect && forerlyMode != null)
        //    //    Input.imeCompositionMode = forerlyMode.Value;
        //}
        //void DisableIME()
        //{

        //    Input.imeCompositionMode = IMECompositionMode.;
        //}

        public bool TryGetTipComponent(out TextAdapter tip)
        {
            tip = null;
            if (transform.TryFind("Text Area/tip", out Transform t))
                return t.TryGetComponent(out tip);
            return false;
        }

 

    }
}