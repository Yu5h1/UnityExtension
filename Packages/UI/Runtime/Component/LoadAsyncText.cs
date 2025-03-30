
using UnityEngine;
using UnityEngine.Rendering;
using Yu5h1Lib;
using Yu5h1Lib.UI;

/// <summary>
///  component.text = $"{component.name}{percentage * 100:0.0}%";
/// </summary>
public class LoadAsyncText : LoadAsyncBehaviour<TextAdapter>
{
    public override void OnProcessing(float percentage)
        => component.text = $"{component.name}{percentage * 100:0.0}%";
}
