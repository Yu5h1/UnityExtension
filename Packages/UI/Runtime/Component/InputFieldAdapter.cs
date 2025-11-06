using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Yu5h1Lib.Runtime;
using Yu5h1Lib.WebSupport;

namespace Yu5h1Lib.UI
{
    public class InputFieldAdapter : SelectableAdapter<IInputFieldOps>, IInputFieldOps,IBindable
        , IScrollHandler
    {
        [SerializeField] private Toggle _PasswordMaskToggle;
        public bool showPasswordMaskToggle
        {
            get => _PasswordMaskToggle?.gameObject?.activeInHierarchy == true;
            set => _PasswordMaskToggle?.gameObject?.SetActive(value);
        }

        #region Ops 
        public string text { get => Ops.text; set => Ops.text = value; }
        public string placeholder { get => Ops.placeholder; set => Ops.placeholder = value; }
        public Component textComponent => Ops.textComponent;
        public TextAdapter textAdapter => Ops.textAdapter;
        public int lineCount => Ops.lineCount;
        public int lineType { get => Ops.lineType; set => Ops.lineType = value; } 
        public bool MaskPassword { get => Ops.MaskPassword; set => Ops.MaskPassword = value; }
        public bool isFocused => Ops.isFocused;
        public int characterLimit { get => Ops.characterLimit; set => Ops.characterLimit = value; }
        public void SetTextWithoutNotify(string value) => Ops.SetTextWithoutNotify(value);
        public void DeactivateInputField() => Ops.DeactivateInputField();        
        public int caretPosition { get => Ops.caretPosition; set => Ops.caretPosition = value; }
        public int selectionAnchorPosition { get => Ops.selectionAnchorPosition; set => Ops.selectionAnchorPosition = value; }
        public int selectionFocusPosition { get => Ops.selectionFocusPosition; set => Ops.selectionFocusPosition = value; }

        public void ActivateInputField() => Ops.ActivateInputField();
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
        [SerializeField] private bool autoWrappingOverflowMode;
        [SerializeField] private bool DontSubmitIfIsNullOrWhiteSpace;

        public UnityEvent<string> _submit;
        public event UnityAction<string> submit
        {
            add => Ops.submit += value;
            remove => Ops.submit -= value;
        }

        public event UnityAction<string> textChanged
        {
            add => Ops.textChanged += value;
            remove => Ops.textChanged -= value;
        }

        public event UnityAction<string> endEdit
        {
            add => Ops.endEdit += value;
            remove => Ops.endEdit -= value;
        }

        public void Select()
        { 
            if (IsAvailable())
                selectable.Select();
        }

        #region IBingable
        public string GetFieldName() => gameObject.name;
        public string GetValue() => text;
        public void SetValue(string value) => text = value;
        public void SetValue(Object bindable)
        {
            if (bindable is IBindable Ibindable)
                SetValue(Ibindable.GetValue());
        }
        #endregion

        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private int visibleLineCount = 5;

        public bool EnableScrollToCaret = true;

        private Vector2Int VisibleLineArea;
        private int lastCaretPosition = -1;
        private int lastSelectionStart = -1;
        private int lastSelectionEnd = -1;
        private int lastLineCount = -1;

        public bool UseWebKeyboardInput = false;
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
            if (scrollRect != null)
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
             (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
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
            if ( scrollRect == null || RawComponent == null || lineType == 0)
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
            bool result = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
 
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
            InvokeAfterFrames(CheckLineCount,1);
        }
        public void CheckLineCount()
        {
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
        public void InvokeSubmitEventAfterFrames(int frames)
            => InvokeAfterFrames(InvokeSubmitEvent, frames);
        protected override void OnDisable()
        {
            base.OnDisable();
        }

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
    }
}