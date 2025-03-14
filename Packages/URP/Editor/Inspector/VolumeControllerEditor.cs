using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Yu5h1Lib.EditorExtension;
using Yu5h1Lib;

[CustomEditor(typeof(VolumeController))]
public class VolumeControllerEditor : Editor<VolumeController> {
   public override void OnInspectorGUI()
   {
      DrawDefaultInspector();
   }
}