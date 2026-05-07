using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib;
using Yu5h1Lib.Mathematics;
using Yu5h1Lib.MVVM;
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

        public override event UnityAction ChangedCallback;

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
        protected override void OnInitializing() { }

        public override bool CanSelect(int index)
        {
            bool result = false;
            foreach (var set in optionSets)
            {
                if (set != null)
                    result |= set.TrySelect(index);
            }
            return result;
        }
        protected override void OnSelected(int index)
        {
            
        }

        public override string GetValue()
        {
            throw new NotImplementedException();
        }

        public override void SetValue(string value, StringComparison comparision)
        {
            throw new NotImplementedException();
        }
    } 
}
