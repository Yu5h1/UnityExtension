using UnityEditor;
using UnityEngine;
using Yu5h1.UnifiedSolver;

// Draws the emitter's hidden companions as modules, the way Unity's own
// ParticleSystem inspector draws ParticleSystemRenderer.
//
// The companions are separate components because they run in different phases,
// but assembling them was never the user's job. They are hidden by the emitter
// and drawn here, so the object reads as one thing with modules rather than
// three components that have to be kept in agreement.
[CustomEditor(typeof(SolverParticleEmitter))]
[CanEditMultipleObjects]
public sealed class SolverParticleEmitterEditor : Editor
{
    // Static so a module stays open as the selection moves between emitters,
    // which is how Unity's own module foldouts behave.
    static bool _rendererOpen = true;
    static bool _runnerOpen;

    UnityEditor.Editor _rendererEditor;
    UnityEditor.Editor _runnerEditor;

    void OnEnable()
    {
        // Objects created before the emitter owned its companions still need
        // them, and OnEnable is a safe context to add components from, unlike
        // OnValidate. Also re-applies the hide flags, so a companion that was
        // added by hand shows up as a module instead of a second header.
        foreach (Object each in targets)
        {
            if (each is SolverParticleEmitter emitter)
                emitter.EnsureCompanions();
        }
    }

    void OnDisable()
    {
        DestroyEditor(ref _rendererEditor);
        DestroyEditor(ref _runnerEditor);
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (targets.Length > 1)
        {
            EditorGUILayout.HelpBox(
                "Modules are hidden while multiple emitters " +
                "are selected.",
                MessageType.None);
            return;
        }

        var emitter = (SolverParticleEmitter)target;
        DrawModule<SolverMeshRenderer>(
            emitter,
            "Renderer",
            ref _rendererEditor,
            ref _rendererOpen);
        DrawModule<SolverParticleModifierRunner>(
            emitter,
            "Modifier Runner",
            ref _runnerEditor,
            ref _runnerOpen);
    }

    void DrawModule<T>(
        SolverParticleEmitter emitter,
        string title,
        ref UnityEditor.Editor moduleEditor,
        ref bool open)
        where T : Component
    {
        T module = emitter.GetComponent<T>();
        if (module == null)
        {
            // Not an error worth throwing: EnsureCompanions runs on the next
            // OnEnable. Say nothing and draw nothing.
            DestroyEditor(ref moduleEditor);
            return;
        }

        if (moduleEditor == null ||
            moduleEditor.target != module)
        {
            DestroyEditor(ref moduleEditor);
            moduleEditor = CreateEditor(module);
        }

        EditorGUILayout.Space();
        open = EditorGUILayout.BeginFoldoutHeaderGroup(
            open,
            title);
        if (open)
        {
            EditorGUI.indentLevel++;
            moduleEditor.OnInspectorGUI();
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    static void DestroyEditor(
        ref UnityEditor.Editor moduleEditor)
    {
        if (moduleEditor == null)
            return;
        DestroyImmediate(moduleEditor);
        moduleEditor = null;
    }
}
