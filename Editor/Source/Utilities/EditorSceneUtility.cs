using UnityEditor;
using System.ComponentModel;

namespace Yu5h1Lib.EditorExtension
{
    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public static class EditorSceneUtility
    {
        public static DrawCameraMode drawMode
        { 
            get => SceneView.lastActiveSceneView.cameraMode.drawMode;
            set {
                var mode = SceneView.lastActiveSceneView.cameraMode;
                mode.drawMode = value;
                SceneView.lastActiveSceneView.cameraMode = mode;
            }
        }
        public static void SwitchMode(DrawCameraMode a, DrawCameraMode b)
        {            
            drawMode = drawMode == a ? b : a;
            SceneView.lastActiveSceneView.Repaint();
        }
        //[MenuItem("Tools/SceneView/Wireframe _F3")]
        //public static void Wireframe()
        //    => SwitchMode(DrawCameraMode.Wireframe, DrawCameraMode.Textured);
        //[MenuItem("Tools/SceneView/ShadedWireframe _F4")]
        //public static void ShadedWireframe()
        //    => SwitchMode(DrawCameraMode.TexturedWire, DrawCameraMode.Textured);

        public static void SetDont ()
        {
            //GameObjectEx
        }
    } 
}
