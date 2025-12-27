using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    public interface IDropDownOps : ISelectableOps
    {
        string currentItem { get; set; }
        string this[int index] { get; set; }
        event UnityAction<int> valueChanged;
        
        void Add(IList<string> options);
        void Sync(IList<string> options);
        void InvokeChangedCallback();
    }
    public abstract class DropDownOps<TDropDown, TOptionData> :
        SelectableOps<TDropDown>, IDropDownOps, IList<string> where TDropDown : Selectable
    {
        protected DropDownOps(TDropDown component) : base(component) { }

        #region abstract members
        public abstract List<TOptionData> options { get; }
        public abstract int current { get; set; }
        public abstract event UnityAction<int> valueChanged;
        public abstract string GetText(TOptionData data);        
        public abstract void SetText(TOptionData data,string value);
        public abstract TOptionData CreateData(string text);
        public abstract void InvokeChangedCallback();
        protected abstract void Refresh();
        #endregion
        public string this[int index]
        {
            get => options.IsValid(index) ? GetText(options[index]) : null;
            set
            {
                if (!options.IsValid(index))
                    return;
                SetText(options[index], value);
                Refresh();
            }
        }
        public int Count => options.Count;
        public bool TryGetCurrentText(out string value)
        {
            var result = options.TryGet(current, out TOptionData data);
            value = result ? GetText(data) : null;
            return result;
        }
        public void Add(string item) => options.Add(CreateData(item));
        public void Insert(int index, string item) => options.Insert(index,CreateData(item));
        public void RemoveAt(int index) => options.RemoveAt(index);
        public void Clear() => options.Clear();
        public int IndexOf(string item) => options.FindIndex(d => GetText(d) == item);

        public string currentItem
        {
            get => TryGetCurrentText(out string text) ? text : null;
            set
            {
                if (currentItem == value)
                    return;
                TrySetValue(value);
            }
        }

        public bool IsReadOnly => false;

        public bool Contains(string item) => IndexOf(item) >= 0;

        public bool Remove(string item)
        {
            var index = IndexOf(item);
            if (index < 0) return false;
            RemoveAt(index);
            return true;
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            for (int i = 0; i < Count; i++)
                array[arrayIndex + i] = this[i];
        }

        public IEnumerator<string> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        
   
        
        public bool TrySetValue(string text)
        {
            if (currentItem == text)
                return true;
            
            

            if (this.TryFindIndex(text, out int index))
            {
                current = index;
                Refresh();
                return true;
            }
            return false;
        }
        public void Add(IList<string> options)
        {
            foreach (var opt in options)
                Add(opt);
        }
        public void Sync(IList<string> options)
        {
            var curText = currentItem;
            Clear();
            foreach (var opt in options)
                Add(opt);
            currentItem = curText;
            Refresh();
        }
        
    }
}
