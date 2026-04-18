using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    public abstract class UI_Adapter<TOps> : UIControl<UIBehaviour>, IUIControl, IOps where TOps : class, IOps
    {
        public Component RawComponent => ui;
        private TOps _Ops;
        public TOps Ops => _Ops ??= OpsFactory.Create<TOps>(RawComponent);

        public override void Get_UIComponent()
        {
            this.TryGetRawComponent<TOps,UIBehaviour>(ref _ui);
        }
    }
    public abstract class UI_Adapter<TOps,TValue> : UIControl<UIBehaviour,TValue>, IValuePort where TOps : class, IOps
    {
        public Component RawComponent => ui;
        private TOps _Ops;
        public TOps Ops => _Ops ??= OpsFactory.Create<TOps>(RawComponent);

        public override void Get_UIComponent()
        {
            this.TryGetRawComponent<TOps, UIBehaviour>(ref _ui);
        }
    }    
}
