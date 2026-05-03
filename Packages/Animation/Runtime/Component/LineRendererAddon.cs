using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib.Animation
{
    public class LineRendererAddon : ComponentController<LineRenderer>
    {
        public LineRenderer lineRenderer => component;
        [SerializeField] private List<Transform> _points;
        public List<Transform> points { get => _points; set => _points = value; }

        [SerializeField] private int _smoothing = 0;

        private Vector3[] _positionCache = System.Array.Empty<Vector3>();
        private Vector3[] _smoothCache = System.Array.Empty<Vector3>();

        private void LateUpdate()
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
            lineRenderer.positionCount = positions.Length;
            lineRenderer.SetPositions(positions);
        }
    }
}
