using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yu5h1Lib;

public class MaterialSequence : AssetSequence<Renderer, Material>
{
    public override Material GetValue(Renderer renderer) => renderer.sharedMaterial ?? renderer.material;
    public override void SetValue(Renderer renderer, Material material) => renderer.sharedMaterial = material;
}