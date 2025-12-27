using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace Yu5h1Lib
{
    [AddComponentMenu("")] // Hide from menu
    public class DebugRenderer : SingletonBehaviour<DebugRenderer> , SingletonBehaviour.IAllowEditMode
    {
        struct Vertex
        { 
            public Vector3 position;
            public Color color;
        }
        struct LineData
        {
            public Vertex start;
            public Vertex end;
            public Color color
            { 
                get => start.color;
                set
                {
                    start.color = value;
                    end.color = value;
                }
            }
        }

        readonly List<LineData> _lines = new List<LineData>(256);

        Mesh _mesh;
        Material _material;

        readonly List<Vector3> _vertices = new List<Vector3>(512);
        readonly List<Color> _colors = new List<Color>(512);
        readonly List<int> _indices = new List<int>(512);

        protected override void OnInstantiated()
        {
            name = "[DebugDraw]"; // { hideFlags = HideFlags.HideAndDontSave };
            gameObject.hideFlags = HideFlags.DontSave;
            if (Application.isPlaying)
                Object.DontDestroyOnLoad(gameObject);
        }
        protected override void OnInitializing()
        {
            _mesh = new Mesh { name = "DebugDraw Mesh", hideFlags = HideFlags.HideAndDontSave };
            _mesh.MarkDynamic();

            // Use built-in Sprites/Default shader - works across all SRPs
            _material = new Material(Shader.Find("Hidden/Yu5h1Lib/DebugLine"))
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            // Subscribe to render callbacks for SRP compatibility
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;

#if UNITY_EDITOR
            // Editor Scene View (確保 Edit Mode 也能畫)
            UnityEditor.SceneView.duringSceneGui += OnSceneGui;
#endif
        }
        void Render()
        {
            if (_lines.Count == 0) return;

            BuildMesh();
            _material.SetPass(0);
            Graphics.DrawMeshNow(_mesh, Matrix4x4.identity);

            _lines.Clear();
        }

        // For Built-in Render Pipeline
        void OnPostRender() => Render();
        void OnCameraPostRender(Camera cam) => Render();
        // For SRP (URP/HDRP)
        void OnEndCameraRendering(ScriptableRenderContext context, Camera camera) => Render();

#if UNITY_EDITOR
        void OnSceneGui(UnityEditor.SceneView sceneView)
        {
            // Scene View 在 Edit Mode 時用 Handles 或 GL 畫
            // 但其實 SRP callback 已經涵蓋 Scene View 了
            // 這個主要是 BiRP Edit Mode 的 fallback
            if (UnityEditor.EditorApplication.isPlaying) return;
            Render();
        }
#endif


        void OnDestroy()
        {
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            Camera.onPostRender -= OnCameraPostRender;

#if UNITY_EDITOR
            UnityEditor.SceneView.duringSceneGui -= OnSceneGui;
#endif

            if (_mesh != null) DestroyImmediate(_mesh);
            if (_material != null) DestroyImmediate(_material);
        }

        void BuildMesh()
        {
            _vertices.Clear();
            _colors.Clear();
            _indices.Clear();

            int index = 0;
            foreach (var line in _lines)
            {
                _vertices.Add(line.start.position);
                _vertices.Add(line.end.position);
                _colors.Add(line.start.color);
                _colors.Add(line.end.color);
                _indices.Add(index++);
                _indices.Add(index++);
            }

            _mesh.Clear();
            _mesh.SetVertices(_vertices);
            _mesh.SetColors(_colors);
            _mesh.SetIndices(_indices, MeshTopology.Lines, 0);
        }

        private void AddLine(Vertex start, Vertex end)
            => _lines.Add(new LineData { start = start, end = end });

        public void AddLine(Vector3 start, Vector3 end, Color colorStart, Color colorEnd)
            => AddLine(new Vertex() { position = start, color = colorStart }  , 
                       new Vertex() { position = end, color = colorEnd });
        public void AddLine(Vector3 start, Vector3 end, Color color)
            => AddLine(start, end, color, color);


        #region Static Methods
        /// <summary>
        /// Draw a line from start to end.
        /// </summary>
        public static void DrawLine(Vector3 start, Vector3 end, Color color)
        {
#if UNITY_EDITOR
            Debug.DrawLine(start, end, color);
#endif
            instance.AddLine(start, end, color);
        }

        /// <summary>
        /// Draw a line from start to end with default white color.
        /// </summary>
        public static void DrawLine(Vector3 start, Vector3 end)
            => DrawLine(start, end, Color.white);

        /// <summary>
        /// Draw a ray from start in direction.
        /// </summary>
        public static void DrawRay(Vector3 start, Vector3 direction, Color color)
            => DrawLine(start, start + direction, color);

        /// <summary>
        /// Draw a ray from start in direction with default white color.
        /// </summary>
        public static void DrawRay(Vector3 start, Vector3 direction)
            => DrawRay(start, direction, Color.white);
        #endregion
    }
}