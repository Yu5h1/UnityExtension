using System;
using UnityEngine;
using Yu5h1.UnifiedSolver;

// Binds selected ClothGenerator grid particles to scene Transforms.
// The selected grid coordinates are authored by ClothAnchorEditor and are
// converted to global solver particle indices once at runtime.
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(ClothGenerator))]
public sealed class ClothAnchor : MonoBehaviour
{
    [Serializable]
    public sealed class Info
    {
        public Transform transform;

        // Authored visually by ClothAnchorEditor.
        [HideInInspector] public Vector2Int node;
        [HideInInspector] public bool generated;
    }

    public Info[] anchors = Array.Empty<Info>();

    const string AnchorComputeResource = "ClothAnchor";
    const int AnchorStride = 16;
    const int ThreadsPerGroup = 64;

    struct AnchorGPU
    {
        public int particleIndex;
        public Vector3 position;
    }

    ClothGenerator _cloth;
    SolverManager _manager;
    ComputeShader _compute;
    ComputeBuffer _anchorBuffer;
    AnchorGPU[] _anchorData;
    int _kernel = -1;
    int _particleOffset = -1;
    bool _reportedMissingCompute;
    bool _reportedMissingOffset;

    void FixedUpdate()
    {
        if (anchors == null || anchors.Length == 0)
            return;

        if (!TryInitialize())
            return;

        EnsureCapacity(anchors.Length);

        int count = 0;
        for (int i = 0; i < anchors.Length; i++)
        {
            Info info = anchors[i];
            if (info == null || info.transform == null || !IsValidNode(info.node))
                continue;

            _anchorData[count++] = new AnchorGPU
            {
                particleIndex = _particleOffset + ToLocalIndex(info.node),
                position = info.transform.position
            };
        }

        if (count == 0)
            return;

        _anchorBuffer.SetData(_anchorData, 0, 0, count);
        _compute.SetInt("_AnchorCount", count);
        _compute.SetBuffer(_kernel, "_Anchors", _anchorBuffer);
        _compute.SetBuffer(_kernel, "_Particles", _manager.ParticleBuffer);
        _compute.Dispatch(_kernel, Mathf.CeilToInt(count / (float)ThreadsPerGroup), 1, 1);
    }

    void OnDisable()
    {
        ReleaseBuffer();
    }

    void OnDestroy()
    {
        ReleaseBuffer();
    }

    bool TryInitialize()
    {
        if (_cloth == null)
            _cloth = GetComponent<ClothGenerator>();

        if (_manager == null)
            _manager = SolverManager.Instance;

        if (_cloth == null || _manager == null || _manager.ParticleBuffer == null)
            return false;

        if (_compute == null)
        {
            _compute = Resources.Load<ComputeShader>(AnchorComputeResource);
            if (_compute == null)
            {
                if (!_reportedMissingCompute)
                {
                    Debug.LogError(
                        $"ClothAnchor: Could not load Resources/{AnchorComputeResource}.compute.",
                        this);
                    _reportedMissingCompute = true;
                }
                return false;
            }

            _kernel = _compute.FindKernel("ApplyAnchors");
        }

        if (!SolverManagerAccess.TryGetClothParticleRange(
                _manager,
                _cloth,
                out int particleOffset,
                out _))
        {
            _particleOffset = -1;
            if (!_reportedMissingOffset &&
                _manager.ActiveCount >= _cloth.resolutionX * _cloth.resolutionY)
            {
                Debug.LogWarning(
                    "ClothAnchor: Could not identify this cloth in SolverManager. " +
                    "Do not move the ClothGenerator transform after it spawns; move only its anchor Transforms.",
                    this);
                _reportedMissingOffset = true;
            }
            return false;
        }

        _particleOffset = particleOffset;
        _reportedMissingOffset = false;
        return true;
    }

    void EnsureCapacity(int capacity)
    {
        if (_anchorBuffer != null && _anchorBuffer.count >= capacity)
            return;

        ReleaseBuffer();

        int bufferCapacity = Mathf.NextPowerOfTwo(Mathf.Max(1, capacity));
        _anchorData = new AnchorGPU[bufferCapacity];
        _anchorBuffer = new ComputeBuffer(bufferCapacity, AnchorStride);
    }

    void ReleaseBuffer()
    {
        if (_anchorBuffer != null)
        {
            _anchorBuffer.Release();
            _anchorBuffer = null;
        }
    }

    bool IsValidNode(Vector2Int node)
    {
        return node.x >= 0 &&
               node.y >= 0 &&
               node.x < _cloth.resolutionX &&
               node.y < _cloth.resolutionY;
    }

    int ToLocalIndex(Vector2Int node)
    {
        return node.y * _cloth.resolutionX + node.x;
    }

    public Vector3 GetNodeWorldPosition(int x, int y)
    {
        ClothGenerator cloth = _cloth != null ? _cloth : GetComponent<ClothGenerator>();
        if (cloth == null)
            return transform.position;

        Vector3 offset = new Vector3(
            (cloth.resolutionX - 1) * cloth.spacing * 0.5f,
            (cloth.resolutionY - 1) * cloth.spacing * 0.5f,
            0f);

        Vector3 localPosition = new Vector3(x * cloth.spacing, y * cloth.spacing, 0f) - offset;
        return cloth.transform.position + cloth.transform.rotation * localPosition;
    }
}
