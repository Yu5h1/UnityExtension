using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib.Runtime;

namespace Yu5h1Lib.UI
{
    public abstract class TextAdapter : UIControl
    {
        public Component component;
        public RectTransform targetRectTransform => (RectTransform)component.transform;
        public abstract string text { get; set; }
        public abstract Color color { get; set; }
        public abstract float fontSize { get; set; }
        public abstract float lineSpacing { get; set; }
        public abstract CanvasRenderer canvasRenderer { get; }

        public abstract void CrossFadeAlpha(float alpha, float duration, bool ignoreTimeScale);

        public abstract Alignment alignment { get; set; }

        public abstract float GetActualFontSize();

        public abstract float GetWrapDistance();
        public abstract float GetFirstLineOffsetY();

    }
    public abstract class TextAdapter<T0, T1> : TextAdapter
        where T0 : Component 
        where T1 : Component
    {

        [SerializeField] private UnityEvent<string> TextChanged;
        public override string text
        {
            get => component switch
            {
                T0 t0 => GetText(t0),
                T1 t1 => GetText(t1),
                _ => Unhandled<string>()
            };
            set
            {
                if (text == value)
                    return;
                switch (component)
                {
                    case T0 t0: SetText(t0, value); break;
                    case T1 t1: SetText(t1, value); break;
                    default: Unhandled(); break;
                }
                TextChanged?.Invoke(value);
            }
        }
        public abstract string GetText(T0 t0);
        public abstract void SetText(T0 t0,string val); 
        public abstract string GetText(T1 t1);
        public abstract void SetText(T1 t1, string val);

        public override Color color
        {
            get => component switch
            {
                T0 t0 => GetColor(t0),
                T1 t1 => GetColor(t1),
                _ => Unhandled<Color>()
            };
            set
            {
                switch (component)
                {
                    case T0 t0: SetColor(t0, value); break;
                    case T1 t1: SetColor(t1, value); break;
                    default: Unhandled(); break;
                }
            }
        }
        public abstract Color GetColor(T0 t0);
        public abstract void SetColor(T0 t0, Color val);
        public abstract Color GetColor(T1 t1);
        public abstract void SetColor(T1 t1, Color val);

        public override Alignment alignment 
        {
            get => component switch
            {
                T0 t0 => GetAlignment(t0),
                T1 t1 => GetAlignment(t1),
                _ => Unhandled<Alignment>()
            };
            set
            {
                switch (component)
                {
                    case T0 t0: SetAlignment(t0, value); break;
                    case T1 t1: SetAlignment(t1, value); break;
                    default: Unhandled(); break;
                }
            }
        }
        public abstract Alignment GetAlignment(T0 t0);
        public abstract void SetAlignment(T0 t0, Alignment val);
        public abstract Alignment GetAlignment(T1 t0);
        public abstract void SetAlignment(T1 t0, Alignment val);

        public override float fontSize
        {
            get => component switch
            {
                T0 t0 => GetFontSize(t0),
                T1 t1 => GetFontSize(t1),
                _ => Unhandled<int>()
            };
            set
            {
                switch (component)
                {
                    case T0 t0: SetFontSize(t0, value); break;
                    case T1 t1: SetFontSize(t1, value); break;
                    default: Unhandled(); break;
                }
            }
        }
        protected abstract float GetFontSize(T0 t0);
        protected abstract void SetFontSize(T0 t0, float value);
        protected abstract float GetFontSize(T1 t0);
        protected abstract void SetFontSize(T1 t0, float value);


        public override float lineSpacing
        {
            get => component switch
            {
                T0 t0 => GetlineSpacing(t0),
                T1 t1 => GetlineSpacing(t1),
                _ => Unhandled<int>()
            };
            set
            {
                switch (component)
                {
                    case T0 t0: SetlineSpacing(t0, value); break;
                    case T1 t1: SetlineSpacing(t1, value); break;
                    default: Unhandled(); break;
                }
            }
        }
        protected abstract float GetlineSpacing(T0 t0);
        protected abstract void SetlineSpacing(T0 t0, float value);
        protected abstract float GetlineSpacing(T1 t0);
        protected abstract void SetlineSpacing(T1 t0, float value);

        public override CanvasRenderer canvasRenderer 
        { 
            get
            {
                switch (component)
                {
                    case T0 t0: return GetCanvasRenderer(t0); 
                    case T1 t1: return GetCanvasRenderer(t1); 
                    default: Unhandled(); break;
                }
                return null;
            }
        }
        public abstract CanvasRenderer GetCanvasRenderer(T0 t0);
        public abstract CanvasRenderer GetCanvasRenderer(T1 t1);

        public abstract void CrossFadeAlpha(T0 t0,float alpha,float duration,bool ignoreTimeScale);
        public abstract void CrossFadeAlpha(T1 t1, float alpha, float duration, bool ignoreTimeScale);

        public override void CrossFadeAlpha(float alpha, float duration, bool ignoreTimeScale)
        {
            switch (component)
            {
                case T0 t0: CrossFadeAlpha(t0,alpha,duration,ignoreTimeScale); break;
                case T1 t1: CrossFadeAlpha(t1, alpha, duration, ignoreTimeScale); break;
                default: Unhandled<T0>(); break;
            }
        }
        public void Unhandled()
        {
            Debug.LogWarning($"{GetType().Name}: 無法處理 {component.GetType().Name} 類型的 component。");
        }
        public T Unhandled<T>()
        {
            Unhandled();
            return default(T);
        }

 


        protected override void OnInitializing()
        {
            base.OnInitializing();
            if (TryGetComponent(out T0 t0))
                component = t0;
            else if (TryGetComponent(out T1 t1))
                component = t1;
            else if (this.TryGetComponentInChildren(out t0,true))
                component = t0;
            else if (this.TryGetComponentInChildren(out t1, true))
                component = t1;
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
                _ => Unhandled<string>()
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
