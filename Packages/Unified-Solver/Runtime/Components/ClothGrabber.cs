using System.Runtime.InteropServices;
using UnityEngine;
using Yu5h1.UnifiedSolver;

// Dynamically grabs cloth nodes near a hand Transform. Animation Events call
// Grab and Release; all node selection and movement stays on the GPU.
[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
public sealed class ClothGrabber : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Cloth whose nodes may be grabbed.")]
    [SerializeField] ClothGenerator cloth = null;

    [Tooltip("Transform used as the hand pose. When omitted, this component's Transform is used.")]
    [SerializeField] Transform hand;

    [Header("Grab")]
    [Min(0.001f)]
    [Tooltip("World-space radius used to select nearby cloth nodes when Grab() is called.")]
    [SerializeField] float grabRadius = 0.25f;

    const string GrabComputeResource = "ClothGrab";
    const int GrabStateStride = 20;
    const int ThreadsPerGroup = 64;

    [StructLayout(LayoutKind.Sequential)]
    struct GrabStateGPU
    {
        public Vector3 localOffset;
        public float originalInvMass;
        public uint grabbed;
    }

    SolverManager _manager;
    ComputeShader _compute;
    ComputeBuffer _grabStateBuffer;
    ComputeBuffer _grabCountBuffer;
    GrabStateGPU[] _emptyStates;
    readonly uint[] _grabCountReadback = new uint[1];

    int _kernelBegin = -1;
    int _kernelApply = -1;
    int _kernelRelease = -1;
    int _bufferParticleCount;
    int _clothParticleOffset = -1;
    int _clothParticleCount;

    bool _grabRequested;
    bool _releaseRequested;
    bool _hasPreviousHandPose;
    bool _isQuitting;
    Vector3 _previousHandPosition;
    Quaternion _previousHandRotation;
    Vector3 _handLinearVelocity;
    Vector3 _handAngularVelocity;

    public bool IsGrabbing { get; private set; }
    public bool IsGrabRequested { get; private set; }
    public int GrabbedCount { get; private set; }

    Transform HandTransform => hand != null ? hand : transform;

    void Reset()
    {
        hand = transform;
    }

    void OnEnable()
    {
        ResetHandMotion();
    }

    void FixedUpdate()
    {
        UpdateHandMotion();

        if (_releaseRequested)
        {
            _releaseRequested = false;
            _grabRequested = false;

            if (IsGrabbing && CanDispatch())
                DispatchRelease(false);

            IsGrabbing = false;
            GrabbedCount = 0;
            return;
        }

        if (_grabRequested)
        {
            if (!TryInitialize())
                return;

            _grabRequested = false;
            GrabbedCount = DispatchBegin();
            IsGrabbing = GrabbedCount > 0;

            Debug.Log(
                $"ClothGrabber: Grabbed {GrabbedCount} cloth nodes " +
                $"within radius {grabRadius:F3}.",
                this);
        }

        if (IsGrabbing && CanDispatch())
            DispatchApply();
    }

    // Animation Event entry point.
    public void Grab()
    {
        if (IsGrabbing)
            return;

        IsGrabRequested = true;
        _releaseRequested = false;
        _grabRequested = true;
    }

    // Animation Event entry point.
    public void Release()
    {
        IsGrabRequested = false;
        _grabRequested = false;
        _releaseRequested = true;
    }

    void OnDisable()
    {
        _grabRequested = false;
        _releaseRequested = false;
        IsGrabRequested = false;

        if (!_isQuitting && IsGrabbing && CanDispatch())
            DispatchRelease(true);

        IsGrabbing = false;
        GrabbedCount = 0;
        ReleaseBuffers();
    }

    void OnDestroy()
    {
        ReleaseBuffers();
    }

    void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    bool TryInitialize()
    {
        if (cloth == null)
        {
            Debug.LogError("ClothGrabber: No ClothGenerator assigned.", this);
            return false;
        }

        if (_manager == null)
            _manager = SolverManager.Instance;

        if (_manager == null ||
            _manager.ParticleBuffer == null ||
            !_manager.ParticleBuffer.IsValid())
        {
            return false;
        }

        if (!SolverManagerAccess.TryGetClothParticleRange(
                _manager,
                cloth,
                out _clothParticleOffset,
                out _clothParticleCount))
        {
            return false;
        }

        if (_compute == null)
        {
            _compute = Resources.Load<ComputeShader>(GrabComputeResource);
            if (_compute == null)
            {
                Debug.LogError(
                    $"ClothGrabber: Could not load Resources/{GrabComputeResource}.compute.",
                    this);
                return false;
            }

            _kernelBegin = _compute.FindKernel("BeginGrab");
            _kernelApply = _compute.FindKernel("ApplyGrab");
            _kernelRelease = _compute.FindKernel("ReleaseGrab");
        }

        int particleCount = _clothParticleCount;
        if (particleCount <= 0)
            return false;

        if (_grabStateBuffer != null &&
            _grabStateBuffer.IsValid() &&
            _bufferParticleCount == particleCount)
        {
            return true;
        }

        ReleaseBuffers();

        _bufferParticleCount = particleCount;
        _emptyStates = new GrabStateGPU[particleCount];
        _grabStateBuffer = new ComputeBuffer(particleCount, GrabStateStride);
        _grabStateBuffer.SetData(_emptyStates);

        _grabCountBuffer = new ComputeBuffer(1, sizeof(uint));
        _grabCountReadback[0] = 0;
        _grabCountBuffer.SetData(_grabCountReadback);
        return true;
    }

    int DispatchBegin()
    {
        _grabCountReadback[0] = 0;
        _grabCountBuffer.SetData(_grabCountReadback);

        BindKernel(_kernelBegin);
        SetHandParameters();
        _compute.SetFloat("_GrabRadius", grabRadius);
        _compute.Dispatch(_kernelBegin, GroupCount, 1, 1);

        // A single uint readback happens only when an Animation Event grabs.
        // It provides useful validation without reading back all cloth particles.
        _grabCountBuffer.GetData(_grabCountReadback);
        return (int)_grabCountReadback[0];
    }

    void DispatchApply()
    {
        BindKernel(_kernelApply);
        SetHandParameters();
        _compute.Dispatch(_kernelApply, GroupCount, 1, 1);
    }

    void DispatchRelease(bool waitForCompletion)
    {
        BindKernel(_kernelRelease);
        SetHandParameters();
        _compute.Dispatch(_kernelRelease, GroupCount, 1, 1);

        if (waitForCompletion)
            _grabCountBuffer.GetData(_grabCountReadback);
    }

    void BindKernel(int kernel)
    {
        _compute.SetInt(
            "_ParticleOffset",
            _clothParticleOffset);
        _compute.SetInt("_ParticleCount", _bufferParticleCount);
        _compute.SetBuffer(kernel, "_Particles", _manager.ParticleBuffer);
        _compute.SetBuffer(kernel, "_GrabStates", _grabStateBuffer);
        _compute.SetBuffer(kernel, "_GrabCount", _grabCountBuffer);
    }

    void SetHandParameters()
    {
        Transform target = HandTransform;
        Quaternion rotation = target.rotation.normalized;

        _compute.SetVector("_HandPosition", target.position);
        _compute.SetVector(
            "_HandRotation",
            new Vector4(rotation.x, rotation.y, rotation.z, rotation.w));
        _compute.SetVector("_HandLinearVelocity", _handLinearVelocity);
        _compute.SetVector("_HandAngularVelocity", _handAngularVelocity);
    }

    int GroupCount =>
        Mathf.CeilToInt(_bufferParticleCount / (float)ThreadsPerGroup);

    bool CanDispatch()
    {
        return cloth != null &&
               _compute != null &&
               _manager != null &&
               HasValidClothParticleRange() &&
               _manager.ParticleBuffer != null &&
               _manager.ParticleBuffer.IsValid() &&
               _grabStateBuffer != null &&
               _grabStateBuffer.IsValid() &&
               _grabCountBuffer != null &&
               _grabCountBuffer.IsValid();
    }

    bool HasValidClothParticleRange()
    {
        return SolverManagerAccess.TryGetClothParticleRange(
                   _manager,
                   cloth,
                   out int particleOffset,
                   out int particleCount) &&
               particleOffset == _clothParticleOffset &&
               particleCount == _bufferParticleCount;
    }

    void UpdateHandMotion()
    {
        Transform target = HandTransform;
        Vector3 position = target.position;
        Quaternion rotation = target.rotation;

        if (!_hasPreviousHandPose || Time.fixedDeltaTime <= 0f)
        {
            _handLinearVelocity = Vector3.zero;
            _handAngularVelocity = Vector3.zero;
            _hasPreviousHandPose = true;
        }
        else
        {
            float inverseDeltaTime = 1f / Time.fixedDeltaTime;
            _handLinearVelocity =
                (position - _previousHandPosition) * inverseDeltaTime;
            _handAngularVelocity = CalculateAngularVelocity(
                _previousHandRotation,
                rotation,
                inverseDeltaTime);
        }

        _previousHandPosition = position;
        _previousHandRotation = rotation;
    }

    void ResetHandMotion()
    {
        Transform target = HandTransform;
        _previousHandPosition = target.position;
        _previousHandRotation = target.rotation;
        _handLinearVelocity = Vector3.zero;
        _handAngularVelocity = Vector3.zero;
        _hasPreviousHandPose = true;
    }

    static Vector3 CalculateAngularVelocity(
        Quaternion previous,
        Quaternion current,
        float inverseDeltaTime)
    {
        Quaternion delta = current * Quaternion.Inverse(previous);
        if (delta.w < 0f)
        {
            delta.x = -delta.x;
            delta.y = -delta.y;
            delta.z = -delta.z;
            delta.w = -delta.w;
        }

        delta.ToAngleAxis(out float angleDegrees, out Vector3 axis);
        if (angleDegrees > 180f)
            angleDegrees -= 360f;

        if (Mathf.Abs(angleDegrees) < 0.0001f ||
            float.IsNaN(axis.x) ||
            float.IsNaN(axis.y) ||
            float.IsNaN(axis.z))
        {
            return Vector3.zero;
        }

        return axis.normalized *
               (angleDegrees * Mathf.Deg2Rad * inverseDeltaTime);
    }

    void ReleaseBuffers()
    {
        if (_grabStateBuffer != null)
        {
            _grabStateBuffer.Release();
            _grabStateBuffer = null;
        }

        if (_grabCountBuffer != null)
        {
            _grabCountBuffer.Release();
            _grabCountBuffer = null;
        }

        _emptyStates = null;
        _bufferParticleCount = 0;
    }

    void OnValidate()
    {
        grabRadius = Mathf.Max(0.001f, grabRadius);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = IsGrabRequested
            ? new Color(1f, 0.1f, 0.1f, 0.9f)
            : new Color(0.1f, 1f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(HandTransform.position, grabRadius);
    }
}
