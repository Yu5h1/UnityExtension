using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib.MVC
{
    public class Control<Data> : MonoBehaviour
    {
        [SerializeField] private Data _data;
        public Data data => _data;

        public UnityEvent _propertyChanged;
        public event UnityAction propertyChanged
        { 
            add => _propertyChanged.AddListener(value);
            remove => _propertyChanged.RemoveListener(value);   
        }
        public void UpdateProperty(View view)
        {
            var fieldName = view.GetFieldName();
            var field = data.GetType().GetField(fieldName);
            if ($"Field name with [{fieldName}] does not exist.".printWarningIf(field == null))
                return;
            field.SetValue(data, view.GetValue());
        }
    }
}