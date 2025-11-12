using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Scripting;

namespace Yu5h1Lib
{
    public static class Scopes
    {

        public readonly struct GizmosScope : IDisposable
        {
            private readonly Color? _previousColor;

            public GizmosScope(Color color)
            {
                _previousColor = Gizmos.color;
                Gizmos.color = color;
            }

            public void Dispose()
            {
                if (_previousColor.HasValue)
                    Gizmos.color = _previousColor.Value;
            }
        }


        public readonly struct GUIScope : IDisposable
        {
            private readonly bool _hasColor, _hasMatrix;
            private readonly Color _previousColor;
            private readonly Matrix4x4 _previousMatrix;

            public float MatrixScale { get; }

            public GUIScope(Color? color = null, Matrix4x4? matrix = null, float matrixScale = 1)
            {
                if (color.HasValue)
                {
                    _hasColor = true;
                    _previousColor = GUI.color;
                    GUI.color = color.Value;
                }
                else
                {
                    _hasColor = false;
                    _previousColor = default;
                }

                if (matrix.HasValue)
                {
                    _hasMatrix = true;
                    _previousMatrix = GUI.matrix;
                    GUI.matrix = matrix.Value;
                }
                else
                {
                    _hasMatrix = false;
                    _previousMatrix = default;
                }

                MatrixScale = matrixScale;
            }

            public GUIScope(Vector2 resolution, float scaleMultiplier, Color? color = null)
                : this(color, CreateCenteredScaleMatrix(resolution, scaleMultiplier, out float scale), scale)
            { }

            public static Matrix4x4 CreateCenteredScaleMatrix(Vector2 resolution, float scaleMultiplier, out float matrixScale)
            {
                matrixScale = Mathf.Min(Screen.width / resolution.x, Screen.height / resolution.y) * scaleMultiplier;
                float scaledWidth = resolution.x * matrixScale;
                float scaledHeight = resolution.y * matrixScale;

                float offsetX = (Screen.width - scaledWidth) / 2f;
                float offsetY = (Screen.height - scaledHeight) / 2f;

                return Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0), Quaternion.identity, new Vector3(matrixScale, matrixScale, 1));
            }

            public void Dispose()
            {
                if (_hasColor) GUI.color = _previousColor;
                if (_hasMatrix) GUI.matrix = _previousMatrix;
            }
        }

    }
}
