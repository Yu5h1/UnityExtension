using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib.Serialization
{
    [System.Serializable]
    public class DataView : DataView<string, string>
    {
        public DataView() : base() {}
        public DataView(IDictionary<string, string> source) : base(source) { }
        public DataView(IEnumerable<KeyValuePair<string, string>> source) : base(source) { }
 
        public override bool TryGetProperty(Object obj)
        {
            if (!(obj is IBindable binding))
            {
                $"{obj} is unbindable".printWarning();
                return false;
            }
            var fieldName = binding.GetFieldName();
            if ($"Field name with [{fieldName}] does not exist.\n{ToString()}".printWarningIf(!ContainsKey(fieldName)))
                return false;
            binding.SetValue(this[fieldName]);
            return true;
        }

        public override bool TrySetProperty(Object bindable)
        {
            if (!(bindable is IBindable binding))
            {
                $"{bindable} is unbindable".printWarning();
                return false;
            }
            var fieldName = binding.GetFieldName();
            if (ContainsKey(fieldName) && this[fieldName].Equals(binding.GetValue()))
                return false;
            this[fieldName] = binding.GetValue();
            return true;
        }
        public string ToJson()
        {
            var json = JsonUtility.ToJson(this).TrimBefore("[", true).TrimAfter("]", true);
            var items = json.Split("},{");
            if (items.Length <= 0)
                return "";
            var parameters = new List<string>();
            foreach (var item in items)
            {
                if (item.IsEmpty())
                    continue;
                var keyValue = item.Split(",");
                var key = keyValue[0].Trim('{', '}').TrimBefore(":", true).Trim('"');
                var value = keyValue[1].Trim('{', '}').TrimBefore(":", true).Trim('"');
                parameters.Add($"\"{key}\":\"{value}\"");
            }
            return parameters.IsEmpty() ? "" : $"{{{parameters.Join(",")}}}";
        }
        public static bool TryParseFromJson(string json, out DataView result) 
        {
            result = null;
            if ("Empty Json".printWarningIf(json.IsEmpty()))
                return false;
            try
            {
                result = JsonUtility.FromJson<DataView>(json);
            }
            catch (System.Exception e)
            {
                e.Message.printWarning();
                return false;
            }
            if (json.Contains("entries"))
                return true;
            var items = json.TrimBefore("{",true).TrimAfterLast("}", true).Split(",");
            var parameters = new List<KeyValue<string, string>>();
            foreach (var item in items)
            {
                var value = item.TrimBefore(":", true).Trim('"');
                parameters.Add(new KeyValue<string, string>(item.TrimAfter(":", true).Trim('"'),
                    item.TrimBefore(":", true).Trim('"')));
                //result.entries.Add();
            }
            result.CopyFrom(parameters);
            return result.Entries.Count > 0;
        }
        public override string ToString() => ToJson();
    }
    [System.Serializable]
    public abstract class DataView<TKey, TValue> : KeyValues<TKey, TValue>
    {
        public abstract bool TryGetProperty(Object bindable);
        public abstract bool TrySetProperty(Object bindable);
        public void GetProperty(Object bindable) => TryGetProperty(bindable);
        public void SetProperty(Object bindable) => TrySetProperty(bindable);

        public DataView() : base() { }
        public DataView(IDictionary<TKey, TValue> source) : base(source) { }
        public DataView(IEnumerable<KeyValuePair<TKey, TValue>> source) : base(source) { }

    }

}
