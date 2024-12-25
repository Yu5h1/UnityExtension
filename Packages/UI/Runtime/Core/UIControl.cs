using UnityEngine;
using Yu5h1Lib;

[RequireComponent(typeof(RectTransform))]
public abstract class UIControl : MonoBehaviour
{
    [SerializeField,ReadOnly]
    private RectTransform _rectTransform;
    public RectTransform rectTransform => this.GetComponent(ref _rectTransform);

    public void Log(string msg) => msg.print();
}
