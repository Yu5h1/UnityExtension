using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using ContentType = TMPro.TMP_InputField.ContentType;

[DisallowMultipleComponent, RequireComponent(typeof(TMP_InputField))]
public class TMP_InputField_Aide : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField _TMP_InputField;
    public TMP_InputField inputField_TMP => _TMP_InputField;

    [SerializeField]
    private UnityEvent<string> _Summit;
    public event UnityAction<string> Summit
    { 
        add => _Summit.AddListener(value);
        remove => _Summit.RemoveListener(value);
    }

    private void Reset()
    {
        _TMP_InputField = GetComponent<TMP_InputField>();
    }

    private void Start()
    {
        if (!inputField_TMP)
            return;
        inputField_TMP.onSubmit.AddListener(OnSummit);
    }
    private void OnSummit(string content) => _Summit?.Invoke(content);

    public void TogglePasswordVisible() => _TMP_InputField.SwapContentType(ContentType.Password,ContentType.Standard);
}

