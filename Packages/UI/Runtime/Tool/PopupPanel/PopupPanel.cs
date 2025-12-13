using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    [DisallowMultipleComponent,RequireComponent(typeof(CanvasGroup))]
    public class PopupPanel : SingletonBehaviour<PopupPanel>
    {
        public static string tipComponentPath => "Text Area/tip";
        #region Nested Structures
        public interface ILogic
        {
            InputFieldSetting[] settings { get; }
            bool showButtons { get; }
            IEnumerator waitResult { get; set; }
            IEnumerator BuildRoutine();
            Result GetResult(object sender);
            void Init(InputFieldAdapter input, int index);
            void Report(Result result);
            public void Close(bool canceled);
        }
        [System.Serializable]
        public class Logic : ILogic
        {
            [SerializeField] private InputFieldSetting[] _inputFieldAdapterSettings;
            [SerializeField] private ResultEvent<string> _reported;

            public InputFieldSetting[] settings => _inputFieldAdapterSettings;
            [SerializeField] private bool _showButtons;
            public bool showButtons => _showButtons;

            public IEnumerator waitResult { get; set; }
            public virtual IEnumerator BuildRoutine() => null;
            public virtual Result GetResult(object sender) => new PopupPanel.Result();

            public void Report(Result result)
               => _reported?.Invoke(result ?? false, result == null ? "The result is a null value" : result.Content);

            public virtual void Init(InputFieldAdapter input, int index)
            {
      
                input.gameObject.SetActive(true);
                input.text = "";
                input.placeholder = settings[index].placeHolder;
                input.characterLimit = settings[index].characterLimit;
                input.showPasswordMaskToggle = settings[index].usePasswordMask;
                input.MaskPassword = settings[index].usePasswordMask;
                if (input.transform.TryFind(tipComponentPath, out Transform c))
                    c.gameObject.SetActive(false);
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
            [SerializeField] private InputFieldSetting[] _inputFieldAdapterSettings;
            [SerializeField] private ResultEvent<string> _reported;

            public virtual InputFieldSetting[] settings => _inputFieldAdapterSettings;
            [SerializeField] private bool _showButtons;
            public virtual bool showButtons => _showButtons;

            public virtual IEnumerator waitResult { get; set; }
            public virtual IEnumerator BuildRoutine() => null;
            public virtual Result GetResult(object sender) => new PopupPanel.Result();

            public virtual void Report(Result result)
               => _reported?.Invoke(result ?? false, result == null ? "The result is a null value" : result.Content);


            public virtual void Init(InputFieldAdapter input,int index)
            {
       
                input.gameObject.SetActive(true);
                input.text = "";
                input.placeholder = settings[index].placeHolder;
                input.characterLimit = settings[index].characterLimit;
                input.showPasswordMaskToggle = settings[index].usePasswordMask;
                input.MaskPassword = settings[index].usePasswordMask;
                if (input.transform.TryFind(tipComponentPath, out Transform c))
                    c.gameObject.SetActive(false);
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
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private InputFieldAdapter[] _fields;
        public InputFieldAdapter[] fields { get => _fields; private set => _fields = value; }

        public ILogic rule { get; private set; }
        public InputFieldSetting[] settings => rule == null ? null : rule.settings;

        public Selectable[] buttons;

        [SerializeField] private TaskEvent<string> _taskEvent;

        public Result result { get; private set; }

        public Image bg;
        public Transform controlsRoot;
        public TextAdapter messageText;


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

        public ResultHandler Validator;
        System.Func<IEnumerator> BuildWaitResult;

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
        }

        private void Start()
        {
            TryGetComponent(out canvasGroup);
            
        }

        public void Verify() => this.StartCoroutine(ref WaitResultRoutine, BuildWaitResultRoutine());
        
        public void Verify(InputFieldAdapter inputField)
        {
            if (inputField.characterLimit > 0 && inputField.text.Length != inputField.characterLimit)
                return;
            Verify();
        }

        IEnumerator BuildWaitResultRoutine(){
            _taskEvent.Begin("");
            yield return BuildWaitResult();
            _taskEvent.End("");
            result = Validator?.Invoke(this);
            yield return null;
            rule.Report(result);
            _taskEvent.Report(result, result.Content);
        }

        public void Show(LogicObject logic) => Show((ILogic)logic);

        public void Show(ILogic Ilogic)
        {
            controlsRoot.gameObject.SetActive(true);
            messageText.transform.parent.gameObject.SetActive(false);
            if ("Faild to Show Popup with null Ilogic".printWarningIf(Ilogic == null))
                return;
            if (!isActiveAndEnabled)
                gameObject.SetActive(true);
            rule = Ilogic;
            for (int i = 0; i < Mathf.Min(fields.Length, settings.Length); i++)
                rule.Init(fields[i], i);
            for (int i = settings.Length; i < fields.Length; i++)
                fields[i].gameObject.SetActive(false);
            foreach (var btn in buttons) btn.gameObject.SetActive(Ilogic.showButtons);
            BuildWaitResult = Ilogic.BuildRoutine;
            Validator = Ilogic.GetResult;
            visible = true;
        }
        public void Show(string message)
        {
            controlsRoot.gameObject.SetActive(false);
            messageText.transform.parent.gameObject.SetActive(true);
            messageText.text = message;
            if (!isActiveAndEnabled)
                gameObject.SetActive(true);
            visible = true;
        }
        public bool Hide()
        {
            if (!visible)
                return false;
            visible = false;
            return true;
        }
        private void OnDisable()
        {
            Validator = null;
            BuildWaitResult = null;
            rule = null;
            if (WaitResultRoutine != null)
                StopCoroutine(WaitResultRoutine);
            
        }
        
    }
}