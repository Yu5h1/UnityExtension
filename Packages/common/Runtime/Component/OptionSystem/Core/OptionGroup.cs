using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yu5h1Lib;
using Yu5h1Lib.Mathematics;
using Yu5h1Lib.Runtime;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib
{
	public class OptionGroup : OptionSet
    {
		[SerializeField] private List<OptionSet> _OptionSets;
		public List<OptionSet> optionSets { get => _OptionSets; set => _OptionSets = value; }

        public int MaxCount => _OptionSets.IsEmpty() ? 0 : optionSets.Max(s => s.Count);
        public int MinCount => _OptionSets.IsEmpty() ? 0 : optionSets.Min(s => s.Count);

        [SerializeField] private MinMax.Option rangeOption;
        public override int Count => rangeOption == MinMax.Option.Min ? MinCount : MaxCount;

        public override bool TryGetItemText(int index, out string text)
        {
            text = "";
            if (optionSets.IsEmpty())
                return false;
            List<string> texts = new List<string>();
            foreach (var set in optionSets)
            {
                if (set != null && set.TryGetItemText(index, out string itemText))
                    texts.Add(itemText);
            }
            if (texts.IsEmpty())
                return false;
            text = texts.Join(", ");
            return true;
        }


        //[SerializeField] private int _currentSet;
        //public int currentSet
        //{
        //    get => _currentSet;
        //    set
        //    {
        //        if (_currentSet == value)
        //            return;
        //        value %= Count;
        //        if (value < 0)
        //            value = Count - 1;
        //        _currentSet = value;
        //    }
        //}

        protected override void OnInitializing() { }

        public override void Select(int index)
        {
            foreach (var set in optionSets)
            {
                if (set != null)
                    set.Select(index);
            }
        }

        public override string GetValue() => "";
        public override void SetValue(string value,System.StringComparison c) { }

    } 
}
