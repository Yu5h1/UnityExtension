using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    [DisallowMultipleComponent,RequireComponent(typeof(CanvasGroup))]
    public class PopupPanel : SingletonBehaviour<PopupPanel>
    {
        #region Nested Structures
        public interface ILogic
        {
            string message { get; }
            InputFieldSetting[] settings { get; }
            bool showButtons { get; }
            IEnumerator waitResult { get; set; }
            IEnumerator BuildRoutine();
            Result GetResult(object sender);
            void Init(PopupPanel panel,InputFieldAdapter input, int index);
            void Report(Result result);
            public void Close(bool canceled);
        }
        [System.Serializable]
        public class Logic : ILogic
        {
            [SerializeField] private string _message;
            public string message { get => _message; private set => _message = value; }
            [SerializeField] private InputFieldSetting[] _inputFieldAdapterSettings;
            [SerializeField] private BinaryEvent<string> _reported;

            public InputFieldSetting[] settings => _inputFieldAdapterSettings;
            [SerializeField] private bool _showButtons;
            public bool showButtons => _showButtons;

            public IEnumerator waitResult { get; set; }
            public virtual IEnumerator BuildRoutine() => null;
            public virtual Result GetResult(object sender) => new PopupPanel.Result();

            public void Report(Result result)
               => _reported?.Invoke(result ?? false, result == null ? "The result is a null value" : result.Content);

            public virtual void Init(PopupPanel panel, InputFieldAdapter input, int index)
            {
                input.gameObject.SetActive(true);
                input.text = "";
                input.placeholder = settings[index].placeHolder;
                input.characterLimit = settings[index].characterLimit;
                input.showPasswordMaskToggle = settings[index].usePasswordMask;
                input.MaskPassword = settings[index].usePasswordMask;
                if (input.TryGetTipComponent(out TextAdapter tip))
                    tip.text = "";
                settings[index].init?.Invoke(input);
            }

            public void Close(bool canceled)
            {
                
            }
        }
        //public abstract class LogicObject<T> : LogicObject , ILogic where T : Logic
        //{
        //    [SerializeField] private T data;
        //    public override InputFieldSetting[] settings => data.settings;
        //    public override bool showButtons => data.showButtons;
        //    public override IEnumerator waitResult { get => data.waitResult; set => data.waitResult = value; }
        //    public override IEnumerator BuildRoutine() => data.BuildRoutine();
        //    public override Result GetResult(object sender) => data.GetResult(sender);
        //    public override void Report(Result result) => data.Report(result);
        //    public override void Init(InputFieldAdapter input, int index) => data.Init(input, index);
        //}
        public abstract class LogicObject : ScriptableObject , ILogic
        {
            [SerializeField] private string _message;
            public string message { get => _message; private set => _message = value; }
            [SerializeField] private InputFieldSetting[] _inputFieldAdapterSettings;
            [SerializeField] private BinaryEvent<string> _reported;

            public virtual InputFieldSetting[] settings => _inputFieldAdapterSettings;
            [SerializeField] private bool _showButtons;
            public virtual bool showButtons => _showButtons;

            public virtual IEnumerator waitResult { get; set; }
            public virtual IEnumerator BuildRoutine() => null;
            public virtual Result GetResult(object sender) => new PopupPanel.Result();

            public virtual void Report(Result result)
               => _reported?.Invoke(result ?? false, result == null ? "The result is a null value" : result.Content);


            public virtual void Init(PopupPanel panel,InputFieldAdapter input,int index)
            {
                input.gameObject.SetActive(true);
                input.text = "";
                input.placeholder = settings[index].placeHolder;
                input.characterLimit = settings[index].characterLimit;
                input.showPasswordMaskToggle = settings[index].usePasswordMask;
                input.MaskPassword = settings[index].usePasswordMask;
                if (input.TryGetTipComponent(out TextAdapter tip))
                    tip.text = "";
                settings[index].init?.Invoke(input);      
                
            }

            public void Close(bool canceled) {}
        }
        public delegate Result ResultHandler(object sender);
        public class Result
        {
            public bool Succeeded;
            public string Content = "Not Implemented ! ";
            public static implicit operator bool(Result r) => r != null && r.Succeeded;

        }
        #endregion

        #region Fields

        [SerializeField] private InputFieldAdapter[] _fields; 
        [SerializeField] private string[] ignoredMessages;
        #endregion
        public InputFieldAdapter[] fields { get => _fields; private set => _fields = value; }

        public ILogic rule { get; private set; }
        public InputFieldSetting[] settings => rule == null ? null : rule.settings;

        public Button confirmButton;
        public Button cancelButton;

        [FormerlySerializedAs("_taskEvent")]
        [SerializeField] private TaskEvent<string> _callbacks;

        public Result result { get; private set; }

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image frameBackground;
        [SerializeField] private Transform fieldsRoot;
        [SerializeField] private Transform buttonsRoot;
        [SerializeField] private Transform messageRoot;
        [SerializeField] private TextAdapter messageText;

        public Transform[] groups => new Transform[] { frameBackground.transform, fieldsRoot, buttonsRoot, messageRoot };


        public bool visible 
        { 
            get => isActiveAndEnabled && canvasGroup.alpha > 0 && canvasGroup.blocksRaycasts;
            set
            {
                if (visible == value)
                    return;
                if (value)
                {
                    if (!isActiveAndEnabled)
                        gameObject.SetActive(true);
                    canvasGroup.alpha = 1;
                    canvasGroup.blocksRaycasts = true;
                }
                else
                {
                    EventSystem.current.SetSelectedGameObject(null);
                    canvasGroup.alpha = 0;
                    canvasGroup.blocksRaycasts = false;
                }
            }
        }
        public bool IsVisible() => visible;

        Coroutine WaitResultRoutine;

        #region caches
        public ResultHandler Validator;
        private System.Func<IEnumerator> BuildWaitResult;
        private InputFieldAdapter verifedInputField;
        #endregion

        protected override void OnInstantiated() {}
        protected override void OnInitializing()
        {
            for (int i = 0; i < fields.Length; i++)
            {
                int index = i; // closure-safe
                fields[i].textChanged += txt =>
                {
                    if (instance.rule.settings.IsValid(index))
                        settings[index].textChanged?.Invoke(fields[index]);
                };
                fields[i].submit += txt =>
                {
                    if (settings.IsValid(index))
                        settings[index].submit?.Invoke(fields[index]);
                };
                fields[i].endEdit += txt =>
                {
                    if (settings.IsValid(index) && settings[index].verifyOnEndEdit)
                    { 
                        Verify();
                    }
                };
            }
            visible = false;
        }

        private void Start()
        {
            
            TryGetComponent(out canvasGroup);

            confirmButton.onClick.AddListener(Verify);
            cancelButton.onClick.AddListener(Hide);
            
        }

        public void Verify() => this.StartCoroutine(ref WaitResultRoutine, BuildWaitResultRoutine());
        
        public void Verify(InputFieldAdapter inputField)
        {
            if (inputField.characterLimit > 0 && inputField.text.Length != inputField.characterLimit)
                return;
            verifedInputField = inputField;
            Verify();
        }

        IEnumerator BuildWaitResultRoutine()
        {
            if (BuildWaitResult == null)
            {
                "No fields need to be verified.".printWarning();
                Hide();
                yield break;
            }
            _callbacks.Begin("");
            yield return BuildWaitResult();
            _callbacks.End("");
            result = Validator?.Invoke(this);
            yield return null;
            rule.Report(result);
            _callbacks.Invoke(result, result.Content);
            if (result)
                Hide();
            else if (verifedInputField != null && verifedInputField.TryGetTipComponent(out TextAdapter tip))
                tip.text = result.Content;

#if UNITY_EDITOR
            $"{result} {result.Content}".print();
#endif
            verifedInputField = null;
        }

        public void Show(LogicObject logic) => Show((ILogic)logic);

        public void Show(ILogic logic)
        {
            Prepare(logic.settings.Length > 0 ? fieldsRoot : null, frameBackground.transform, logic.showButtons ? buttonsRoot : null
                , logic.message.IsEmpty() ? null : messageRoot);


            if (messageText != null)
                messageText.text = logic.message;


            if ("Faild to Show Popup with null Ilogic".printWarningIf(logic == null))
                return;
            if (!isActiveAndEnabled)
                gameObject.SetActive(true);
            rule = logic;
            for (int i = 0; i < Mathf.Min(fields.Length, settings.Length); i++)
                rule.Init(this,fields[i], i);
            for (int i = settings.Length; i < fields.Length; i++)
                fields[i].gameObject.SetActive(false);
            
            BuildWaitResult = logic.BuildRoutine;
            Validator = logic.GetResult;
            visible = true;
        }
        public void Show(string message)
        {
            if (ignoredMessages.Any( m => !m.IsEmpty() && message.Contains(m)))
                return;
            Prepare(messageRoot,frameBackground.transform);
            messageText.text = message;
            if (!isActiveAndEnabled)
                gameObject.SetActive(true);
            visible = true;
        }
        public void Hide()
        {
            if (!visible)
                return;
            visible = false;
        }
        private void Prepare(params Transform[] ignores)
        {
            foreach (var g in groups)
                g.gameObject.SetActive(ignores.Contains(g));
        }
        private void OnDisable()
        {
            Validator = null;
            BuildWaitResult = null;
            rule = null;
            if (WaitResultRoutine != null)
                StopCoroutine(WaitResultRoutine);
            
        }
        [ContextMenu(nameof(Test))]
        public void Test()
            => Show("This is a test message.");

    }
}