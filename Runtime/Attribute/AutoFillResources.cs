using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Yu5h1Lib;
using static System.IO.PathInfo;

public class AutoFillResources : AutoFillAttribute.Items
{
    public override string[] Get(params object[] args)
    {
        string subpath = args.IsEmpty() ? "" : $"{args.First()}";
        string searchPattern = args.IsValid(1) ? (string)args[1] : "";
        bool AllDirectories = args.IsValid(2) ? (bool)args[2] : false;
        return GetFiles(Combine("Assets/Resources", subpath), "*.prefab", false).Select(d=> PathInfo.GetName(d)).ToArray();
    }
}
