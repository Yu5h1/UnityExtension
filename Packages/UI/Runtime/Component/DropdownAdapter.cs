using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Yu5h1Lib.UI
{
    public class DropdownAdapter : SelectableAdapter<IDropDownOps>, IDropDownOps
    {
        #region Ops
        public string currentItem { get => Ops.currentItem; set => Ops.currentItem = value; }
        public void Add(IList<string> options) => Ops.Add(options);
        public void Sync(IList<string> options) => Ops.Sync(options);
        public void InvokeChangedCallback() => Ops.InvokeChangedCallback();
        public event UnityAction<int> valueChanged
        {
            add => Ops.valueChanged += value;
            remove => Ops.valueChanged -= value;
        }
        #endregion

        [SerializeField] private UnityEvent<string> _valueTextChanged = new UnityEvent<string>();

        

        public string this[int index] { get => Ops[index]; set => Ops[index] = value; }


        public event UnityAction<string> valueTextChanged
        {
            add => _valueTextChanged.AddListener(value);
            remove => _valueTextChanged.RemoveListener(new UnityAction<string>(value));
        }



        private void Start()
        {
            Ops.valueChanged += OnValueChanged;
        }

        private void OnValueChanged(int index) => _valueTextChanged?.Invoke(this[index]);
        

        public void SelectOption(string ItemName) => currentItem = ItemName;
        //test method contextmenu
        [ContextMenu(nameof(Test))]
        public void Test() => Add(Enumerable.Range(1,6).Select(d=>$"{d}").ToList());
 
    }

}