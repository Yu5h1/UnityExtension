using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib;

public class LineGroup : MonoBehaviour
{
    
    [SerializeField]
    private LineRendererController2D[] _lineControllers;
    public LineRendererController2D[] lineControllers => _lineControllers;

    public UnityEvent AllConnected;
    public UnityEvent AllDisconnected;
    // Start is called before the first frame update
    void Start()
    {
        if (!lineControllers.IsEmpty())
        for (int i = 0; i < lineControllers.Length; i++)
        {
                lineControllers[i].connected += OnAllConnected;
                lineControllers[i].disconnected += OnAllDisconnected;
        }
    }

    private bool IsConnecting(LineRendererController2D controller) => controller.IsConnecting;

    public bool CheckConntecting(LineRendererController2D l) => !l.IsPerforming && l.IsConnecting;
    public bool allConntecting() => lineControllers.All(CheckConntecting);


    public bool AlreadyInvokeAllConnected { get; private set; }
    private void OnAllConnected()
    {
        if (!isActiveAndEnabled || AlreadyInvokeAllConnected || lineControllers.Any(l => !l.IsConnecting))
            return;
        AllConnected?.Invoke();
        AlreadyInvokeAllConnected = true;
        AlreadyInvokeAllDisconnected = false;
    }
    
    public bool AlreadyInvokeAllDisconnected { get; private set; } 
    private void OnAllDisconnected()
    {
        if (!isActiveAndEnabled || AlreadyInvokeAllDisconnected || lineControllers.Any(l => l.IsConnecting))
            return;
        AllDisconnected?.Invoke();
        AlreadyInvokeAllDisconnected = true;
        AlreadyInvokeAllConnected = false;
    }
    [ContextMenu(nameof(ConnectAll))]
    public void ConnectAll() => SetConnection(true);
    [ContextMenu(nameof(DisconnectAll))]
    public void DisconnectAll() => SetConnection(false);
    public void SetConnection(bool val)
    {
        foreach (var line in lineControllers)
            line.IsConnecting = val;

    }
    public bool IsNotPerforming()
        => lineControllers.All(l => !l.IsPerforming);
}
