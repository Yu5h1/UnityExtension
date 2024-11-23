using UnityEngine;

public class DropDownTagAttribute : PropertyAttribute {
    public bool AllowMultiple;
    public DropDownTagAttribute() {}
    public DropDownTagAttribute(bool allowMultiple) => AllowMultiple = allowMultiple; 
}
