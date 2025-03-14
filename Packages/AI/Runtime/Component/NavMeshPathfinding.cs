using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Yu5h1Lib;

public class NavMeshPathfinding : MonoBehaviour
{
    [System.Flags]
    public enum UpdateMode
    {
        None        = 0,
        Complete    = 1 << 0,
        Partial     = 1 << 1,
        Invalid     = 1 << 2,
        All = Complete | Partial | Invalid
    }
    [SerializeField]
    private Transform target;
    public NavMeshPath NavPath { get; private set; }
    private Vector3[] lastCorners;
    public UnityEvent<Vector3[]> changed;

    public bool IsChangedEventsEmpty { get; private set; }

    [SerializeField]
    private UnityEvent found;
    [SerializeField]
    private UnityEvent lost;

    [SerializeField,ReadOnly]
    private bool _Exists;
    public bool Exists
    {
        get => _Exists;
        set
        {  
            if (_Exists == value)
                return;
            _Exists = value;
            if (value)
                found?.Invoke();
            else
                lost?.Invoke();
        }
    }
    [SerializeField]
    private UpdateMode updateMode = UpdateMode.All;

    private void Start()
    {
        NavPath = new NavMeshPath();
        IsChangedEventsEmpty = changed.IsEmpty();
    }
    public void FixedUpdate()
    {
        CalculatePath();
    }
    public void CalculatePath()
    {
        if (this.IsAvailable() && target && NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, NavPath))
        {
            var allowDraw = false;
            switch (NavPath.status)
            {
                case NavMeshPathStatus.PathComplete:
                    allowDraw = updateMode.HasFlag(UpdateMode.Complete);
                    Exists = true;
                    break;
                case NavMeshPathStatus.PathPartial:
                    allowDraw = updateMode.HasFlag(UpdateMode.Partial);
                    Exists = false;
                    break;
                case NavMeshPathStatus.PathInvalid:
                    allowDraw = updateMode.HasFlag(UpdateMode.Invalid);
                    Exists = false;
                    break;
            }
            if (updateMode == UpdateMode.All)
                allowDraw = true;
            else if (updateMode == UpdateMode.None)
                allowDraw = false;

            if (allowDraw)
            {
                if (!lastCorners.SequenceEqual(NavPath.corners) && !IsChangedEventsEmpty)
                    changed.Invoke(NavPath.corners);
            }

        } else Exists = false;

    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
            return;
        if (NavPath == null) NavPath = new NavMeshPath();
        CalculatePath();
    }

    public static NavMeshPathStatus Convert(UpdateMode mode)
    {
        switch (mode)
        {
            case UpdateMode.Complete:
                return NavMeshPathStatus.PathComplete;
            case UpdateMode.Partial:
                return NavMeshPathStatus.PathPartial;
            case UpdateMode.Invalid:
                return NavMeshPathStatus.PathInvalid;
            default:
                return NavMeshPathStatus.PathInvalid;
        }
    }
}
