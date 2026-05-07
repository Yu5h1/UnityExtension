using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Yu5h1Lib.MVVM;

namespace Yu5h1Lib.UI
{
    public abstract class UI_Adapter<TAdapter> : UIControl<UIBehaviour>, IUIControl, IAdapterShell<TAdapter>
        where TAdapter : class, IAdapter<Component>
    {
        public Component Raw => ui;
        private TAdapter _adapter;
        public TAdapter adapter => _adapter ??= AdapterFactory<Component>.Create<TAdapter>(Raw);
        IAdapter IAdapterShell.adapter => adapter;

        public override void Get_UIComponent()
        {
            this.TryGetRawComponent<TAdapter,UIBehaviour>(ref _ui);
        }
    }
}
