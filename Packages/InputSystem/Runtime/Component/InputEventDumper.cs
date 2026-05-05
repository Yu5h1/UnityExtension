using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class InputEventDumper : MonoBehaviour
{
    public string[] ignoreTexts;
    void OnEnable()
    {
        InputSystem.onEvent += OnEvent;
    }
    void OnDisable()
    {
        InputSystem.onEvent -= OnEvent;
    }
    void OnEvent(InputEventPtr evt, InputDevice device)
    {
        if (evt.type != StateEvent.Type) return;
        foreach (var control in device.allControls)
        {
            if (!control.HasValueChangeInEvent(evt))
                continue;
            var msg = $"{device.name} ¡÷ {control.path} = {control.ReadValueAsObject()}";
            if (ignoreTexts.Any(text => msg.Contains(text)))
                continue;
            msg.print();
        }
    }
}