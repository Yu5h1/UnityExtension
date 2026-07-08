using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib.Animation
{
    public class LineRendererAddon : ComponentController<LineRenderer>, IColor
    {
        public LineRenderer lineRenderer => component;
        [SerializeField] private List<Transform> _points;
        public List<Transform> points { get => _points; set => _points = value; }

        public Color color { get => GetColor(); set => SetColor(value); }
        public float alpha { get => GetAlpha(); set => SetAlpha(value); }

        [SerializeField] private int _smoothing = 0;

        private Vector3[] _positionCache = System.Array.Empty<Vector3>();
        private Vector3[] _smoothCache = System.Array.Empty<Vector3>();
        private GradientAlphaKey[] _alphaKeyCache = System.Array.Empty<GradientAlphaKey>();
        private GradientAlphaKey[] _alphaKeyApplyCache = System.Array.Empty<GradientAlphaKey>();

        private void LateUpdate() => Apply();

        public virtual void CacheAlphaKeys()
        {
            var lr = GetLineRenderer();
            if (lr == null)
            {
                _alphaKeyCache = System.Array.Empty<GradientAlphaKey>();
                _alphaKeyApplyCache = System.Array.Empty<GradientAlphaKey>();
                return;
            }

            CacheAlphaKeys(lr.colorGradient.alphaKeys);
        }

        public virtual void SetAlpha(float alpha)
        {
            var lr = GetLineRenderer();
            if (lr == null)
                return;

            var gradient = lr.colorGradient;
            var alphaKeys = gradient.alphaKeys;
            if (alphaKeys == null || alphaKeys.Length == 0)
                return;

            if (!CanApplyFromCache(alphaKeys))
                CacheAlphaKeys(alphaKeys);

            alpha = Mathf.Clamp01(alpha);
            for (int i = 0; i < _alphaKeyCache.Length; i++)
                _alphaKeyApplyCache[i] = new GradientAlphaKey(_alphaKeyCache[i].alpha * alpha, _alphaKeyCache[i].time);

#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.Undo.RecordObject(lr, nameof(SetAlpha));
#endif
            gradient.SetKeys(gradient.colorKeys, _alphaKeyApplyCache);
            lr.colorGradient = gradient;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.EditorUtility.SetDirty(lr);
#endif
        }

        public virtual Color GetColor()
        {
            var lr = GetLineRenderer();
            if (lr == null)
                return Color.white;

            var colorKeys = lr.colorGradient.colorKeys;
            var c = colorKeys != null && colorKeys.Length > 0 ? colorKeys[0].color : Color.white;
            c.a = GetAlpha();
            return c;
        }

        public virtual void SetColor(Color color)
        {
            var lr = GetLineRenderer();
            if (lr == null)
                return;

            var gradient = lr.colorGradient;
            var colorKeys = gradient.colorKeys;
            if (colorKeys == null || colorKeys.Length == 0)
                colorKeys = new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) };
            else
            {
                for (int i = 0; i < colorKeys.Length; i++)
                    colorKeys[i].color = color;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.Undo.RecordObject(lr, nameof(SetColor));
#endif
            gradient.SetKeys(colorKeys, gradient.alphaKeys);
            lr.colorGradient = gradient;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.EditorUtility.SetDirty(lr);
#endif
            SetAlpha(color.a);
        }

        public virtual float GetAlpha()
        {
            var lr = GetLineRenderer();
            if (lr == null)
                return 0f;

            var alphaKeys = lr.colorGradient.alphaKeys;
            if (alphaKeys == null || alphaKeys.Length == 0)
                return 1f;

            if (!CanApplyFromCache(alphaKeys))
                CacheAlphaKeys(alphaKeys);

            for (int i = 0; i < alphaKeys.Length; i++)
            {
                if (_alphaKeyCache[i].alpha > 0f)
                    return Mathf.Clamp01(alphaKeys[i].alpha / _alphaKeyCache[i].alpha);
            }
            return 0f;
        }

        [ContextMenu(nameof(Apply))]
        public virtual void Apply()
        {
            if (_points == null || _points.Count == 0)
                return;
            if (_positionCache.Length != _points.Count)
                _positionCache = new Vector3[_points.Count];
            for (int i = 0; i < _points.Count; i++)
                _positionCache[i] = _points[i] != null ? _points[i].position : Vector3.zero;

            if (_smoothing > 0)
                SetPositions(CatmullRom(_positionCache, _smoothing));
            else
                SetPositions(_positionCache);
        }

        private Vector3[] CatmullRom(Vector3[] pts, int subdivisions)
        {
            int segments = pts.Length - 1;
            int totalPoints = segments * subdivisions + 1;
            if (_smoothCache.Length != totalPoints)
                _smoothCache = new Vector3[totalPoints];

            int idx = 0;
            for (int i = 0; i < segments; i++)
            {
                Vector3 p0 = pts[Mathf.Max(i - 1, 0)];
                Vector3 p1 = pts[i];
                Vector3 p2 = pts[i + 1];
                Vector3 p3 = pts[Mathf.Min(i + 2, pts.Length - 1)];
                for (int j = 0; j < subdivisions; j++)
                {
                    float t = j / (float)subdivisions;
                    _smoothCache[idx++] = CatmullRomPoint(p0, p1, p2, p3, t);
                }
            }
            _smoothCache[idx] = pts[pts.Length - 1];
            return _smoothCache;
        }

        private static Vector3 CatmullRomPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (
                2f * p1 +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        public virtual void SetPositions(Vector3[] positions)
        {
            var lr = GetLineRenderer();
            if (lr == null)
                return;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.Undo.RecordObject(lr, nameof(Apply));
#endif
            lr.positionCount = positions.Length;
            lr.SetPositions(positions);
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.EditorUtility.SetDirty(lr);
#endif
        }

        private void CacheAlphaKeys(GradientAlphaKey[] alphaKeys)
        {
            if (alphaKeys == null || alphaKeys.Length == 0)
            {
                _alphaKeyCache = System.Array.Empty<GradientAlphaKey>();
                _alphaKeyApplyCache = System.Array.Empty<GradientAlphaKey>();
                return;
            }

            if (_alphaKeyCache.Length != alphaKeys.Length)
                _alphaKeyCache = new GradientAlphaKey[alphaKeys.Length];
            if (_alphaKeyApplyCache.Length != alphaKeys.Length)
                _alphaKeyApplyCache = new GradientAlphaKey[alphaKeys.Length];

            System.Array.Copy(alphaKeys, _alphaKeyCache, alphaKeys.Length);
        }

        private bool CanApplyFromCache(GradientAlphaKey[] alphaKeys)
        {
            if (alphaKeys == null || _alphaKeyCache.Length != alphaKeys.Length || _alphaKeyApplyCache.Length != alphaKeys.Length)
                return false;

            for (int i = 0; i < alphaKeys.Length; i++)
            {
                if (!Mathf.Approximately(_alphaKeyCache[i].time, alphaKeys[i].time))
                    return false;
            }
            return true;
        }

        private LineRenderer GetLineRenderer() => lineRenderer != null ? lineRenderer : GetComponent<LineRenderer>();
    }
}
