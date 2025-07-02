using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Yu5h1Lib.EditorExtension;
using Yu5h1Lib.UI;

[CustomEditor(typeof(EnvelopeLines))]
public class UI_EnvelopelinesEditor : Editor<EnvelopeLines> {
   public override void OnInspectorGUI()
   {
      DrawDefaultInspector();
   }
}