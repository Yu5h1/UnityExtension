using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeAnimation : MonoBehaviour
{
    private AnimationClip clip;
    public AnimationCurve curve;
    public string relativePath;

    // Start is called before the first frame update
    void Start()
    {
        clip = new AnimationClip();
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
