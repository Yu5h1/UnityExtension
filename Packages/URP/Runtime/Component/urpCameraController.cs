using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Yu5h1Lib;
using Yu5h1Lib.Runtime;

public class urpCameraController : BaseMonoBehaviour
{
    [SerializeField, ReadOnly]
    private Camera _camera;
#pragma warning disable 0109
    public new Camera camera => _camera;
#pragma warning restore 0109


    protected override void OnInitializing()
    {
        this.GetComponent(ref _camera);
    }
    void Start() {}

}
