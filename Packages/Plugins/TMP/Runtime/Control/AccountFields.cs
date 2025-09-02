using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib;
using Yu5h1Lib.Serialization;


[System.Serializable]
public class AccountFields
{
    public Transform root;
    public TMP_InputField email;
    public TMP_InputField userName;
    public TMP_InputField password;
    [SerializeField]
    private UnityEvent<bool> Validated;

    public bool AllowEmptyPassword;


    public IEnumerable<TMP_InputField> Fields()
    {
        yield return email;
        yield return userName;

        if (!AllowEmptyPassword)
            yield return password;
    }


    public bool IsEmpty() => Fields().All(IsNull) || Fields().All(IsEmpty);
    public bool AnyFieldEmpty() => Fields().All(IsNull) || Fields().Any(IsEmpty);

    public bool IsEmpty(TMP_InputField field)
        => field && field.text.IsEmpty();

    public bool IsNull(TMP_InputField field) => field == null;

    public event UnityAction<string> ValueChanged
    {
        add
        {
            foreach (var item in Fields())
                AddValueChanged(item, value);
        }
        remove
        {
            foreach (var item in Fields())
                RemoveValueChanged(item, value);
        }
    }
    public void CopyFrom(AccountFields other)
    {
        email.text = other.email.text;
        userName.text = other.userName.text;
        password.text = other.password.text;
    }


    public void AddValueChanged(TMP_InputField field, UnityAction<string> call)
    {
        if (field && call != null)
            field.onValueChanged?.AddListener(call);
    }
    public void RemoveValueChanged(TMP_InputField field, UnityAction<string> call)
    {
        if (field && call != null)
            field.onValueChanged?.RemoveListener(call);
    }

    public void Init()
    {
        ValueChanged -= OnTextNotEmptyChanged;
        ValueChanged += OnTextNotEmptyChanged;
        OnTextNotEmptyChanged("");
    }

    public void OnTextNotEmptyChanged(string txt)
      => Validated?.Invoke(!AnyFieldEmpty());

    public void Load(DataView userData)
    {
        if (userData == null)
            return;
        if (email)
            email.text = userData["Email"];
        if (userName)
            userName.text = userData["Name"];
        if (password)
            password.text = userData["Password"];
    }

}