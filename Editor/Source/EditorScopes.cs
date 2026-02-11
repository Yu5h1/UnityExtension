using System;
using UnityEngine;
using UnityEditor;

namespace Yu5h1Lib.EditorExtension
{
    public static class EditorScopes
    {
        public readonly struct HandlesScope : IDisposable
        {
            private readonly Color? _previousColor;
            private readonly Matrix4x4? _previousMatrix;

            public HandlesScope(Color? color = null, Matrix4x4? matrix = null)
            {
                if (color.HasValue)
                {
                    _previousColor = Handles.color;
                    Handles.color = color.Value;
                }
                else
                    _previousColor = null;

                if (matrix.HasValue)
                {
                    _previousMatrix = Handles.matrix;
                    Handles.matrix = matrix.Value;
                }
                else
                    _previousMatrix = null;
            }

            public void Dispose()
            {
                if (_previousColor.HasValue)
                    Handles.color = _previousColor.Value;

                if (_previousMatrix.HasValue)
                    Handles.matrix = _previousMatrix.Value;
            }
        }
        public static TempValueScope<int> IndentLevel(int add = 1) => new TempValueScope<int>(
          () => EditorGUI.indentLevel,
          v => EditorGUI.indentLevel = v,
          EditorGUI.indentLevel + add);
        
        public static TempValueScope<float> FieldWidth(float width) => new TempValueScope<float>(
            () => EditorGUIUtility.fieldWidth,
            v => EditorGUIUtility.fieldWidth = v,
            width);


        public static TempValueScope<float> LabelWidth(float width) => new TempValueScope<float>(
            () => EditorGUIUtility.labelWidth,
            v => EditorGUIUtility.labelWidth = v,
            width);

        public static TempValueScope<Color> HandlesColor(Color color) => new TempValueScope<Color>(
            () => Handles.color,
            v => Handles.color = v,
            color);

        public static TempValueScope<Color> GUIColor(Color color) => new TempValueScope<Color>(
            () => GUI.color,
            v => GUI.color = v,
            color);
    }
}