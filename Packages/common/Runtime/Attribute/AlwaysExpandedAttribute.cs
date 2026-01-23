using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
public sealed class AlwaysExpandedAttribute : PropertyAttribute
{    
    public readonly bool ShowLabel;    
    public readonly float LabelSpacing;

    public AlwaysExpandedAttribute(bool showLabel = true, float labelSpacing = 2f)
    {
        ShowLabel = showLabel;
        LabelSpacing = labelSpacing;
    }
}