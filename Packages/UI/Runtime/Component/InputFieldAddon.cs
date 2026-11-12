using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Yu5h1Lib.MVVM;

namespace Yu5h1Lib.UI
{
    public class InputFieldAddon : UI_Adapter<IInputFieldOps>, IInputFieldOps
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
        public void NotifyValueChanged() => adapter.NotifyValueChanged();

        public void Unbind() => adapter.Unbind();
        public string Get() => adapter.Get();
        public void Set(string value) => adapter.Set(value);
        #endregion
        #region Ops 
        public string text { get => adapter.text; set => adapter.text = value; }
        public string placeholder { get => adapter.placeholder; set => adapter.placeholder = value; }
        public Component textComponent => adapter.textComponent;
        public TextAddon textAdapter => adapter.textAdapter;
        public int lineCount => adapter.lineCount;
        public int lineType
        {
            get => adapter.lineType;
            set
            {
                adapter.lineType = value;
                useNativeMultilineSwitch = false;
                if (multilineInput != null)
                    GetComponentInParent<MultilineInputField>()?.RefreshLayout();
            }
        }

        /// <summary>Switches between single-line and multiline editing without changing the draft.</summary>
        public bool Multiline
        {
            get => lineType != (int)InputField.LineType.SingleLine;
            set
            {
                bool nativeField = multilineInput == null && Raw is InputField;
                if (!nativeField && Multiline == value)
                    return;
                var multilineMode = multilineInput != null ? InputField.LineType.MultiLineNewline : InputField.LineType.MultiLineSubmit;
                lineType = (int)(value ? multilineMode : InputField.LineType.SingleLine);
                useNativeMultilineSwitch = nativeField;
            }
        }
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

        public UnityEvent<string> _submit = new();
        /// <summary>Fires when the addon requests submission with the current field value.</summary>
        public event UnityAction<string> Submitted
        {
            add => _submit.AddListener(value);
            remove => _submit.RemoveListener(value);
        }
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
        private MultilineSubmitInputField multilineInput;
        private bool useNativeMultilineSwitch;
        private Vector2Int? pendingNativeSelection;

        /// <summary>Creates a uGUI field with multiline editing enabled.</summary>
        public static InputFieldAddon CreateMultiline(RectTransform field, Text text, Graphic placeholder,
            Graphic targetGraphic, float minHeight = 44f, int maxVisibleLines = 5, float scrollbarWidth = 16f)
        {
            var addon = Create(field, text, placeholder, targetGraphic, minHeight, maxVisibleLines, scrollbarWidth);
            addon.Multiline = true;
            return addon;
        }

        /// <summary>
        /// Creates a single-line uGUI field with optional multiline editing in a dedicated parent viewport.
        /// The text must stretch across the field. The viewport supplies layout height and scrolling.
        /// </summary>
        public static InputFieldAddon Create(RectTransform field, Text text, Graphic placeholder,
            Graphic targetGraphic, float minHeight = 44f, int maxVisibleLines = 5, float scrollbarWidth = 16f)
        {
            if (field == null || field.parent is not RectTransform viewport)
                throw new ArgumentException("A field with a RectTransform parent is required.", nameof(field));
            if (text == null || text.transform.parent != field ||
                text.rectTransform.anchorMin != Vector2.zero || text.rectTransform.anchorMax != Vector2.one)
                throw new ArgumentException("The text must be a direct child that stretches across the field.", nameof(text));
            if (field.GetComponent<Selectable>() != null || field.GetComponent<InputFieldAddon>() != null ||
                viewport.GetComponent<MultilineInputField>() != null)
                throw new InvalidOperationException("The field and its viewport must be dedicated to this multiline input.");

            var input = field.gameObject.AddComponent<MultilineSubmitInputField>();
            input.targetGraphic = targetGraphic;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.lineType = InputField.LineType.SingleLine;
            viewport.gameObject.AddComponent<MultilineInputField>()
                .Configure(input, minHeight, maxVisibleLines, scrollbarWidth);

            var addon = field.gameObject.AddComponent<InputFieldAddon>();
            addon.AutoResizeScrollRect = false;
            addon.EnableScrollToCaret = false;
            addon.Init(false);
            return addon;
        }

        protected override void OnInitializing()
        {
            base.OnInitializing();
            scrollRect = GetComponentInParent<ScrollRect>();

            if (Raw is MultilineSubmitInputField input)
            {
                multilineInput = input;
                input.SubmitRequested -= InvokeSubmitEvent;
                input.SubmitRequested += InvokeSubmitEvent;
                return;
            }

            submit += OnSubmit;
            textChanged += TextChanged;
            VisibleLineArea = new Vector2Int(0, visibleLineCount-1);

        }
        private void OnDestroy()
        {
            if (multilineInput != null)
                multilineInput.SubmitRequested -= InvokeSubmitEvent;
        }

        private void Start()
        {
            if (multilineInput != null)
                return;

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
            if (multilineInput != null)
                return;

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

        /// <summary>
        /// TMP input fields shift their text up (the text's anchoredPosition.y) to keep the caret inside their own
        /// viewport (the Text Area). On a new line the viewport has not grown yet, so TMP computes the offset from the old,
        /// shorter viewport height: (textHeight - oldViewportHeight) * 0.5, about half a line. When the field then grows to
        /// fit the text, TMP does not recompute, so the text stays pushed up (first line clipped, a gap at the bottom) until
        /// the caret leaves the viewport and TMP recomputes. Seen on WebGL, where the HTML input's events reach TMP before the
        /// layout has grown the field.
        /// The field is sized to its text by a ContentSizeFitter, so once the viewport height equals the text height the
        /// correct offset is 0 for any vertical alignment; this puts it back. Not touched while the text is taller or
        /// shorter than the viewport, where TMP's own offset is meaningful.
        /// </summary>
        private void RestoreTextOffsetIfFits()
        {
            if (!AutoResizeScrollRect || scrollRect == null || textComponent == null || lineType == 0)
                return;
            var textRt = textComponent.transform as RectTransform;
            var viewport = textRt != null ? textRt.parent as RectTransform : null;
            if (viewport == null || viewport == rectTransform)
                return;
            if (textRt.anchoredPosition.y <= 0.01f)
                return;
            if (Mathf.Abs(LayoutUtility.GetPreferredHeight(textRt) - viewport.rect.height) > 1f)
                return;

            var position = textRt.anchoredPosition;
            position.y = 0f;
            textRt.anchoredPosition = position;
            // TMP keeps its caret object (a sibling of the text, named "Caret") at the text's position.
            if (viewport.Find("Caret") is RectTransform caret)
                caret.anchoredPosition = position;
        }
        private void LateUpdate()
        {
            if (pendingNativeSelection is Vector2Int selection)
            {
                if (EventSystem.current?.currentSelectedGameObject != gameObject)
                    pendingNativeSelection = null;
                else if (isFocused)
                {
                    selectionAnchorPosition = selection.x;
                    selectionFocusPosition = selection.y;
                    pendingNativeSelection = null;
                }
            }
            RestoreTextOffsetIfFits();
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
            if (useNativeMultilineSwitch && Raw is InputField input)
            {
                OnNativeSubmit(input, txt);
                return;
            }

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
        private void OnNativeSubmit(InputField input, string draft)
        {
            if (!input.IsInteractable())
                return;

            bool composing = MultilineSubmitInputField.HasComposition();
            bool shift = IsShiftPressed();
            bool modified = InputHandler.GetKey(KeyCode.LeftControl) || InputHandler.GetKey(KeyCode.RightControl) ||
                InputHandler.GetKey(KeyCode.LeftAlt) || InputHandler.GetKey(KeyCode.RightAlt) ||
                InputHandler.GetKey(KeyCode.LeftCommand) || InputHandler.GetKey(KeyCode.RightCommand);
            if (!composing && !shift && !modified && !input.readOnly)
            {
                InvokeSubmitEvent();
                return;
            }

            int anchor = Mathf.Clamp(input.selectionAnchorPosition, 0, draft.Length);
            int focus = Mathf.Clamp(input.selectionFocusPosition, 0, draft.Length);
            if (shift && !modified && !composing && !input.readOnly && Multiline)
            {
                int start = Mathf.Min(anchor, focus);
                int length = Mathf.Abs(anchor - focus);
                if (input.characterLimit == 0 || draft.Length - length < input.characterLimit)
                {
                    input.text = draft.Remove(start, length).Insert(start, "\n");
                    anchor = focus = start + 1;
                }
            }

            if (input != null && input.IsActive() && input.IsInteractable() &&
                EventSystem.current?.currentSelectedGameObject == input.gameObject)
            {
                pendingNativeSelection = new Vector2Int(anchor, focus);
                input.ActivateInputField();
            }
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
            if ((multilineInput != null || useNativeMultilineSwitch) && Raw is InputField input &&
                (input.readOnly || !input.IsInteractable() || string.IsNullOrWhiteSpace(input.text)))
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

        public bool TryGetTipComponent(out TextAddon tip)
        {
            tip = null;
            if (transform.TryFind("Text Area/tip", out Transform t))
                return t.TryGetComponent(out tip);
            return false;
        }

 

    }
}