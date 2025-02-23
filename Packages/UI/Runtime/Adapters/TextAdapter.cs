using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib.UI
{
    public abstract class TextAdapter : MonoBehaviour
    {
        public Component component;
        public abstract string text { get; set; }
    }
    public abstract class TextAdapter<T0, T1> : TextAdapter
        where T0 : Component 
        where T1 : Component
    {
        public override string text
        {
            get => component switch
            {
                T0 t0 => GetText(t0),
                T1 t1 => GetText(t1),
                _ => Unhandled()
            };
            set
            {
                switch (component)
                {
                    case T0 t0: SetText(t0, value); break;
                    case T1 t1: SetText(t1, value); break;
                    default: Unhandled(); break;
                }
            }
        }
        public abstract string GetText(T0 t0);
        public abstract void SetText(T0 t0,string val); 
        public abstract string GetText(T1 t1);
        public abstract void SetText(T1 t1, string val);
        public string Unhandled()
        {
            Debug.LogWarning($"TextAdapter: 無法處理 {component.GetType().Name} 類型的 component。");
            return string.Empty;
        }
    }
    public abstract class TextAdapter<T0, T1,T2> : TextAdapter<T0,T1>
        where T0 : Component
        where T1 : Component
        where T2 : Component 
    {
        public override string text
        {
            get => component switch
            {
                T0 t0 => GetText(t0),
                T1 t1 => GetText(t1),
                T2 t2 => GetText(t2),
                _ => Unhandled()
            };
            set
            {
                switch (component)
                {
                    case T0 t0: SetText(t0, value); break;
                    case T1 t1: SetText(t1, value); break;
                    case T2 t2: SetText(t2, value); break;
                    default: Unhandled(); break;
                }
            }
        }
        public abstract string GetText(T2 t2);
        public abstract void SetText(T2 t2, string val);
    }

}
