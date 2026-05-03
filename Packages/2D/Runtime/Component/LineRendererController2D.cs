using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    [RequireComponent(typeof(LineRenderer))]
    public class LineRendererController2D : ComponentController<LineRenderer>
    {
        public enum FadeStyle
        {
            Shrink = -1,
            Extend = 1,
        }
        public LineRenderer lineRenderer => component;

        [SerializeField,ReadOnly]
        private EdgeCollider2D _edgeCollider;
        public EdgeCollider2D edgeCollider => _edgeCollider;
        public List<Vector2> points { get; private set; } = new List<Vector2>();

        [SerializeField, ReadOnly]
        private bool _IsConnecting;
        public bool IsConnecting
        {
            get => _IsConnecting;
            set
            {
                if (_IsConnecting == value)
                    return;
                _IsConnecting = value;
                if (value)
                    Connect();
                else
                    Disconnect(3);
            }
        }

        //public Vector3[] positionsCache { get; private set; }

        private void Start()
        {
            Connect();
        }

        public virtual void Refresh() {}

        public void SetPositions(Vector3[] positions)
        {
            lineRenderer.positionCount = positions.Length;
            lineRenderer.SetPositions(positions);
            UpdateColliderPoints(positions);
        }
        public void UpdateColliderPoints(Vector3[] positions)
        {
            if (_edgeCollider)
            {
                points.Clear();
                for (int i = 0; i < positions.Length; i++)
                {
                    points.Add(positions[i]);
                }
                if (lineRenderer.positionCount > 2 && lineRenderer.loop)
                    points.Add(positions.First());
                _edgeCollider.offset = lineRenderer.useWorldSpace ? -transform.position : Vector2.zero; ;
                _edgeCollider.SetPoints(points);
            }
        }
        [ContextMenu(nameof(UpdateColliderPoints))]
        public void UpdateColliderPoints()
        {
            Vector3[] results = new Vector3[lineRenderer.positionCount];
            lineRenderer.GetPositions(results);
            UpdateColliderPoints(results);
        }

        #region Fields

        [SerializeField]
        private float ConnectDelay = 0.5f;
        [SerializeField]
        private float ConnectDuration = 0.5f;

        #endregion

        #region Events

        [SerializeField]
        private UnityEvent _connected;
        public event UnityAction connected
        {
            add => _connected.AddListener(value);
            remove => _connected.RemoveListener(value);
        }
        private void InvokeConnected() => _connected?.Invoke();
        [SerializeField]
        private UnityEvent _disconnected;
        public event UnityAction disconnected
        {
            add => _disconnected.AddListener(value);
            remove => _disconnected.RemoveListener(value);
        }
        private void InvokeDisconnected() => _disconnected?.Invoke();
        #endregion



        #region Coroutine

        #region Cache
        [SerializeField, ReadOnly]
        private bool _IsPerforming;
        public bool IsPerforming { get => _IsPerforming; private set => _IsPerforming = value; }
        protected Coroutine performCoroutine;
        //private float performingTimer;
        #endregion


        [ContextMenu(nameof(Connect))]
        public void Connect()
        {
            _IsConnecting = true;
            this.StartCoroutine(ref performCoroutine,
                FadeProcess(ConnectDelay, ConnectDuration, 1, defaultColor: new Color(1, 1, 1, 0), performEnd: InvokeConnected));
        }
        [ContextMenu(nameof(Disconnect))]
        public void Disconnect() => IsConnecting = false;

        public void Disconnect(int index, FadeStyle style = FadeStyle.Extend)
        {
            _IsConnecting = false;
            var duration = 0.3f;
            this.StartCoroutine(ref performCoroutine, FadeProcess(0, duration, 0, index, style, performEnd: InvokeDisconnected));
        }

        private IEnumerator FadeProcess(float delay, float duration, float alpha, int startIndex = 0,
                FadeStyle style = FadeStyle.Extend, Color? defaultColor = null,
                UnityAction performBegin = null, UnityAction performEnd = null)
        {
            lineRenderer.PrepareGradient(defaultColor);
            if (delay > 0)
                yield return new WaitForSeconds(delay);
            //performingTimer = Time.time;
            IsPerforming = true;
            performBegin?.Invoke();

            var colorGradient = lineRenderer.colorGradient;
            var durationInterval = duration / 8;

            var max = Mathf.Max(startIndex, 8 - startIndex);
            var backward_i = style == FadeStyle.Shrink ? 7 : startIndex;
            var forward_i = style == FadeStyle.Shrink ? 0 : startIndex;

            for (int i = 0; i < max; i++)
            {
                SetColorKey(forward_i, alpha, durationInterval);
                SetColorKey(backward_i, alpha, durationInterval);
                forward_i++;
                backward_i--;
                yield return new WaitForSeconds(durationInterval);
            }

            IsPerforming = false;
            performEnd?.Invoke();

        }
        private void SetColorKey(int index, float alpha, float duration)
        {
            if (index < 0 || index > 7)
                return;
            var g = lineRenderer.colorGradient;
            var alphaKeys = g.alphaKeys;
            var colorKeys = g.colorKeys;
            alphaKeys[index].alpha = alpha;
            g.SetKeys(colorKeys, alphaKeys);
            lineRenderer.colorGradient = g;
        }
        private IEnumerator DelaySetColorKey(int index, float alpha, float duration)
        {
            SetColorKey(index, alpha, duration);
            yield return new WaitForSeconds(duration);
        }
        #endregion
    }
}

