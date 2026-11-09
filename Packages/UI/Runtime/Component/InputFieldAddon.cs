using System.Collections;
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

        /// <summary>WebGL 換行、移動游標後等幾幀再捲（等行數、輸入框高度、版面都更新）。1 幀實測不夠，先試 5 幀。</summary>
        private const int WebGLScrollDelayFrames = 5;
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
            if (scrollRect != null)
                Diag($"config movement={scrollRect.movementType} contentPivot={scrollRect.content.pivot} contentAnchors={scrollRect.content.anchorMin}-{scrollRect.content.anchorMax} viewportAnchors={scrollRect.viewport.anchorMin}-{scrollRect.viewport.anchorMax} {State()}");
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
            var inArea = caretlineIndex.Between(VisibleLineArea.x, VisibleLineArea.y);
            Diag($"caretChanged caret={caretPosition} line={caretlineIndex} area={VisibleLineArea} inArea={inArea}");
            if (inArea)
                return;

            UpdateVisibleLineArea(caretlineIndex);
            // WebGL：HTML 輸入框的鍵盤事件比 Unity 自己的輸入晚到，游標移動時行數與輸入框高度還沒更新（高度晚一幀才跟上），
            // 現在捲會捲到舊的版面；晚一幀再捲。其他平台維持立刻捲。
            if (Application.platform == RuntimePlatform.WebGLPlayer)
                this.DelayInvoke(ScrollToVisibleLineArea, WebGLScrollDelayFrames);
            else
                ScrollToVisibleLineArea();
        }
        private void ScrollToVisibleLineArea()
        {
            if (scrollRect == null)
                return;
            Diag($"scroll BEGIN area={VisibleLineArea} {State()}");
            ScrollToLine(VisibleLineArea.x);
            Diag($"scroll END {State()}");
        }
        public void ScrollToCaretPosition()
            => ScrollToLine(textAdapter.GetLineIndexByPosition(selectionFocusPosition));
        public void ScrollToLine(int lineIndex)
        {
            var lineY = textAdapter.GetLineY(lineIndex, true);
            var v = textAdapter.transform.TransformPoint(0, lineY + 10, 0);
            Diag($"ScrollToLine index={lineIndex} lineY={lineY:0.#}");
            scrollRect.ScrollToPosition(v);
        }

        /// <summary>
        /// TMP 輸入框為了讓游標留在它自己的文字區（viewport）裡，會把文字往上推（文字的 anchoredPosition.y 變正）。
        /// 換行時文字區還沒長高，新的一行在文字區底下，文字就被推上去；文字區之後依文字長高了，TMP 不會把文字推回來，
        /// 結果第一行被切、底下多出空隙，直到游標移出範圍、TMP 重算才恢復。
        /// 輸入框由 ContentSizeFitter 依文字長高，文字區裝得下文字、文字又是靠上對齊時，文字的位移就該是 0，這裡把它拉回來。
        /// 條件不成立（文字比文字區高、非靠上對齊、沒有自己的文字區）時不動，TMP 照常自己捲。
        /// </summary>
        private void RestoreTextOffsetIfFits()
        {
            if (!AutoResizeScrollRect || scrollRect == null || textComponent == null || lineType == 0)
                return;
            var textRt = textComponent.transform as RectTransform;
            var viewport = textRt != null ? textRt.parent as RectTransform : null;
            if (viewport == null || viewport == rectTransform)
                return;
            if (textRt.anchoredPosition.y <= 0.01f)   // 只處理被往上推的情況
                return;
            if (!textAdapter.alignment.HasFlag(Alignment.Top))
                return;
            if (LayoutUtility.GetPreferredHeight(textRt) > viewport.rect.height + 0.5f)
                return;

            var position = textRt.anchoredPosition;
            Diag($"restoreTextOffset textY={position.y:0.#} viewportH={viewport.rect.height:0}");
            position.y = 0f;
            textRt.anchoredPosition = position;
            // TMP 的游標物件（文字的同層，名稱 Caret）要跟文字位置一致，TMP 自己用 AssignPositioningIfNeeded 做同樣的事
            if (viewport.Find("Caret") is RectTransform caret)
                caret.anchoredPosition = position;
        }
        private void LateUpdate() => RestoreTextOffsetIfFits();

        #region 暫時的診斷記錄（WebGL 換行後捲動位置問題），確認原因後整段移除
        private Coroutine diagRoutine;
        private static bool DiagEnabled => Application.platform == RuntimePlatform.WebGLPlayer;
        private void Diag(string message)
        {
            if (DiagEnabled)
                Debug.Log($"[IFA] f{Time.frameCount} {message}");
        }
        private string State()
        {
            if (scrollRect == null)
                return "(no scrollRect)";
            var srt = (RectTransform)scrollRect.transform;
            var content = scrollRect.content;
            var textRt = textComponent != null ? textComponent.transform as RectTransform : null;
            return $"scrollH={srt.sizeDelta.y:0} viewportH={scrollRect.viewport.rect.height:0} contentH={content.rect.height:0} contentY={content.anchoredPosition.y:0.#} " +
                   $"fieldH={rectTransform.rect.height:0} textY={(textRt != null ? textRt.anchoredPosition.y : 0f):0.#} lines={lineCount} caret={caretPosition} area={VisibleLineArea}";
        }
        private void DiagFrames(string reason)
        {
            if (!DiagEnabled || !isActiveAndEnabled)
                return;
            if (diagRoutine != null)
                StopCoroutine(diagRoutine);
            diagRoutine = StartCoroutine(DiagRoutine(reason));
        }
        private IEnumerator DiagRoutine(string reason)
        {
            for (int i = 0; i < 12; i++)
            {
                Diag($"{reason}+{i} {State()}");
                yield return null;
            }
        }
        #endregion

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

            Diag($"textChanged len={txt?.Length}");
            DiagFrames("afterText");
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
            Diag($"lineCountChanged count={count} caretLine={caretlineIndex} before {State()}");
            UpdateVisibleLineArea(caretlineIndex);
            ResizeScrollRect();
            Diag($"lineCountChanged after resize area={VisibleLineArea} {State()}");
            // WebGL（桌面瀏覽器）：換行後行數與高度晚一幀才更新，上面的 UpdateVisibleLineArea 已把游標那一行算進可見區，
            // 之後 OnCaretPositionChanged 看到游標在可見區內就不捲，捲動位置會停在改高度之前的狀態，直到游標移出可見區才被修正。
            // 所以改高度之後在這裡捲一次（等版面更新，晚一幀）。其他平台游標的檢查比行數更新早，已經捲過了。
            if (Application.platform == RuntimePlatform.WebGLPlayer && !Application.isMobilePlatform)
                this.DelayInvoke(ScrollToVisibleLineArea, WebGLScrollDelayFrames);
        }
        private void ResizeScrollRect()
        {
            var dynamicLineCount = Mathf.Clamp(Mathf.Min(visibleLineCount, lineCount), 1, visibleLineCount);
            if (lastLineCount != dynamicLineCount)
            {
                var rt = (RectTransform)scrollRect.transform;
                var sizeDelta = rt.sizeDelta;
                // 輸入框會依文字長高（ContentSizeFitter），可見區就取它實際要的高度（含最小高度），最多 visibleLineCount 行。
                // 不用「行高 × 行數」去估：估的值只要和實際差一點，內容就比可見區高，多出來的那段會被捲動或切掉。
                var maxHeight = (visibleLineCount * textAdapter.GetBaseLineHeight()) + GetTextAreaVerticalInset();
                var wanted = LayoutUtility.GetPreferredHeight(rectTransform);
                var height = wanted > 0f ? Mathf.Min(wanted, maxHeight) : (dynamicLineCount * textAdapter.GetBaseLineHeight()) + GetTextAreaVerticalInset();
                sizeDelta.y = Mathf.Max(height, LayoutUtility.GetMinHeight(rectTransform));
                rt.sizeDelta = sizeDelta;
            }
        }

        /// <summary>
        /// 文字區（輸入框底下那個放文字的 viewport）上下內縮的總和，從它的 RectTransform 讀，不寫死數字。
        /// 沒有獨立的文字區時（文字直接在輸入框底下）沿用原本的 20。
        /// </summary>
        private float GetTextAreaVerticalInset()
        {
            const float fallback = 20f;
            var viewport = textAdapter != null ? textAdapter.transform.parent as RectTransform : null;
            if (viewport == null || viewport == rectTransform)
                return fallback;
            // 上下拉伸（anchorMin.y=0、anchorMax.y=1）時，sizeDelta.y 就是 -(上內縮 + 下內縮)
            if (!Mathf.Approximately(viewport.anchorMin.y, 0f) || !Mathf.Approximately(viewport.anchorMax.y, 1f))
                return fallback;
            return Mathf.Max(0f, -viewport.sizeDelta.y);
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

        public bool TryGetTipComponent(out TextAddon tip)
        {
            tip = null;
            if (transform.TryFind("Text Area/tip", out Transform t))
                return t.TryGetComponent(out tip);
            return false;
        }

 

    }
}