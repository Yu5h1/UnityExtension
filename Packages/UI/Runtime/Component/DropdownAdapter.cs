using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib.UI
{
    public class DropdownAdapter : UI_Adapter<IDropDownOps>, IDropDownOps
    {
        #region Ops
        public string currentItem { get => adapter.currentItem; set => adapter.currentItem = value; }
        public void Add(IList<string> options) => adapter.Add(options);
        public void Sync(IList<string> options) => adapter.Sync(options);
        public void InvokeChangedCallback() => adapter.InvokeChangedCallback();
        public event UnityAction<int> valueChanged
        {
            add => adapter.valueChanged += value;
            remove => adapter.valueChanged -= value;
        }
        #endregion

        [SerializeField] private UnityEvent<string> _valueTextChanged = new UnityEvent<string>();

        

        public string this[int index] { get => adapter[index]; set => adapter[index] = value; }


        public event UnityAction<string> valueTextChanged
        {
            add => _valueTextChanged.AddListener(value);
            remove => _valueTextChanged.RemoveListener(new UnityAction<string>(value));
        }



        private void Start()
        {
            adapter.valueChanged += OnValueChanged;
        }

        private void OnValueChanged(int index) => _valueTextChanged?.Invoke(this[index]);
        

        public void SelectOption(string ItemName) => currentItem = ItemName;
        //test method contextmenu
        [ContextMenu(nameof(Test))]
        public void Test() => Add(Enumerable.Range(1,6).Select(d=>$"{d}").ToList());
 
    }

}