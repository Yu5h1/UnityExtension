using Enum = System.Enum;
using UnityEditor;
using UnityEngine;
using Yu5h1Lib.Runtime;

namespace Yu5h1Lib.EditorExtension
{
    [CustomPropertyDrawer(typeof(FlagsOptionAttribute))]
    public class FlagsOptionDrawer : PropertyDrawer
    {
        public FlagsOptionAttribute target { get => (FlagsOptionAttribute)attribute; }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            FlagsOptionPopup(position,property,label,target.optionStyle,ref target.alloptions,option => target.optionStyle = option);
        }
        public static void FlagsOptionPopup(Rect position, SerializedProperty property, GUIContent label,
                                            FlagsOptionStyle style,ref string[] alloptions,
                                            System.Action<FlagsOptionStyle> clickitem = null )
        {
            Event e = Event.current;
            if (clickitem != null) {
                var labelRect = position;
                labelRect.width = EditorGUIUtility.labelWidth;
                if (e.type == EventType.MouseDown && e.button == 1 && labelRect.Contains(e.mousePosition))
                {
                    GenericMenu contextMenu = new GenericMenu();
                    contextMenu.AddItems(style, typeof(FlagsOptionStyle), option => clickitem((FlagsOptionStyle)option));
                    contextMenu.ShowAsContext();
                }
            }
            var targetEnumType = property.GetEnumType();
            switch (style)
            {
                case FlagsOptionStyle.Mix:
                    EditorGUI.PropertyField(position, property);
                    break;
                case FlagsOptionStyle.Option:
                    property.intValue = (int)Enum.ToObject(targetEnumType, EditorGUI.EnumPopup(position, label, (Enum)Enum.ToObject(targetEnumType, property.intValue)));
                    break;
                case FlagsOptionStyle.All:
                    if (alloptions == null || alloptions.Length == 0) {
                        alloptions = EnumUtils.GetAllCombinations(
                                            targetEnumType).ConvertAll(
                                                new System.Converter<object, string>(
                                                obj => Enum.ToObject(targetEnumType, obj).ToString()
                                            )).ToArray();
                    }
                    int index = 0;
                    string selectedName = Enum.ToObject(targetEnumType, property.intValue).ToString();
                    for (int i = 0; i < alloptions.Length; i++)
                    {
                        if (alloptions[i] == selectedName)
                        {
                            index = i;
                            break;
                        }
                    }
                    using (var scope = new EditorGUI.ChangeCheckScope())
                    {
                        index = EditorGUI.Popup(position, property.name, index, alloptions);
                        if (scope.changed)
                        {
                            property.intValue = (int)Enum.Parse(targetEnumType, alloptions[index]);
                        }
                    }
                    break;
            }
        }
    }
}