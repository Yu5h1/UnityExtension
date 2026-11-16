using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    ///   A temporary orthographic camera that renders into a RenderTexture, with helpers to sample
    ///   the result at world positions, for the pixel checks of ParticleRendering.
    /// </summary>
    internal sealed class ParticleRenderRig : IDisposable
    {
        public static readonly Color Background = new Color(0f, 0f, 1f, 1f);

        readonly GameObject _object;
        readonly RenderTexture _target;
        readonly Texture2D _readback;
        readonly int _size;

        public ParticleRenderRig(Vector3 position, Quaternion rotation, float orthographicSize, int size = 256)
        {
            _size = size;
            _object = new GameObject("RenderRig") { hideFlags = HideFlags.HideAndDontSave };
            Camera = _object.AddComponent<Camera>();
            Camera.enabled = false;
            Camera.orthographic = true;
            Camera.orthographicSize = orthographicSize;
            Camera.nearClipPlane = 0.1f;
            Camera.farClipPlane = 100f;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = Background;
            Camera.allowMSAA = false;
            Camera.allowHDR = false;
            _object.transform.SetPositionAndRotation(position, rotation);
            _target = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
            Camera.targetTexture = _target;
            _readback = new Texture2D(size, size, TextureFormat.RGBA32, false);
        }

        public Camera Camera { get; }

        public void Render()
        {
            Camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _target;
            _readback.ReadPixels(new Rect(0, 0, _size, _size), 0, 0);
            RenderTexture.active = previous;
        }

        public Vector2Int Pixel(Vector3 world)
        {
            Vector3 viewport = Camera.WorldToViewportPoint(world);
            return new Vector2Int(Mathf.FloorToInt(viewport.x * _size), Mathf.FloorToInt(viewport.y * _size));
        }

        public Color Sample(Vector3 world)
        {
            Vector2Int pixel = Pixel(world);
            return _readback.GetPixel(Mathf.Clamp(pixel.x, 0, _size - 1), Mathf.Clamp(pixel.y, 0, _size - 1));
        }

        public Color Sample(int x, int y) => _readback.GetPixel(x, y);

        public static bool IsBackground(Color color) =>
            color.b > 0.9f && color.r < 0.1f && color.g < 0.1f;

        public bool IsDrawn(Vector3 world) => !IsBackground(Sample(world));

        public int CountDrawnInRow(int y)
        {
            int count = 0;
            for (int x = 0; x < _size; x++)
                if (!IsBackground(_readback.GetPixel(x, y)))
                    count++;
            return count;
        }

        public int CountDrawnInColumn(int x)
        {
            int count = 0;
            for (int y = 0; y < _size; y++)
                if (!IsBackground(_readback.GetPixel(x, y)))
                    count++;
            return count;
        }

        public int CountDrawn()
        {
            int count = 0;
            Color[] pixels = _readback.GetPixels();
            foreach (Color pixel in pixels)
                if (!IsBackground(pixel))
                    count++;
            return count;
        }

        public void Dispose()
        {
            Camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(_object);
            UnityEngine.Object.DestroyImmediate(_target);
            UnityEngine.Object.DestroyImmediate(_readback);
        }
    }
}
