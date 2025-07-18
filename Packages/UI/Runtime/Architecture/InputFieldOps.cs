using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib.Common;

public interface IInputFieldOps : ISelectableOps
{
    string text { get; set; }
    string placeholder { get; set; }
    int characterLimit { get; set; }

    bool MaskPassword { get; set; }

    void SetTextWithoutNotify(string value);

    event UnityAction<string> submit;
    event UnityAction<string> textChanged;
    event UnityAction<string> endEdit;
}

public abstract class InputFieldOps<T> : OpsBase<T>, IInputFieldOps where T : Component
{
    protected InputFieldOps(T component) : base(component) {}

    public abstract bool interactable { get; set; }
    public abstract string text { get; set; }
    public abstract string placeholder { get; set; }
    public abstract int characterLimit { get; set; }

    public abstract bool MaskPassword { get; set; }
    

    public abstract void SetTextWithoutNotify(string value);

    public abstract event UnityAction<string> submit;
    public abstract event UnityAction<string> textChanged;
    public abstract event UnityAction<string> endEdit;
}
