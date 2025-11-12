using System;
using System.Collections.Generic;
using System.Text;

namespace Yu5h1Lib
{
    [Serializable]
    public class SeparatedValues
    {
        public string Separator = ",";
        public string[] Items;

        public SeparatedValues(string content, string separator = ",")
        {
            Separator = separator;
            Parse(content);
        }

        public void Parse(string content)
        {
            Items = content.Split(new[] { Separator }, StringSplitOptions.None);
        }
        public bool TryGetValue(int index, out string val)
        {
            val = null;
            if (Items.IsEmpty() || !Items.IsValid(index))
                return false;
            val = Items[index];
            return true;
        }

        public override string ToString() => Items.Join(Separator);

    }
}
