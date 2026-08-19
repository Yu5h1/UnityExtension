using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Supplies one <see cref="Renderer"/>'s materials as an <see cref="IReadOnlyList{T}"/>, so a
    /// <see cref="MaterialController"/> can drive them without knowing where they came from.
    /// <para>With <see cref="useSharedMaterial"/> off, this addon builds its own copies and writes them
    /// back through <see cref="Renderer.sharedMaterials"/> instead of reading <see cref="Renderer.materials"/>.
    /// Both do the same thing to the renderer, but a copy made here cannot be reached by anyone else, so
    /// ownership needs no flag to record it: a non-null instance array is the record. That matters because
    /// nothing reclaims these automatically — Unity leaves materials from <see cref="Renderer.materials"/>
    /// to the caller, and the C# GC never collects a <see cref="Object"/>.</para>
    /// <para>The authored assets are kept aside and put back before the copies are destroyed, so removing
    /// this addon leaves the renderer as it was found rather than pointing at destroyed materials.</para>
    /// <para>One addon governs one Renderer. Add another for another Renderer; a
    /// <see cref="MaterialController"/> takes several sources at once.</para>
    /// <para>A subclass adding its own <c>OnDestroy</c> must override this one and call base: Unity
    /// dispatches a message to the most derived declaration only, and a hidden <see cref="OnDestroy"/>
    /// silently leaks every copy this addon made.</para>
    /// </summary>
    public abstract class RendererAddon<TRenderer> : ComponentController<TRenderer>, IReadOnlyList<Material> where TRenderer : Renderer
    {
        [Tooltip("Share the renderer's materials instead of copying them. Anything written then reaches the project assets.")]
        [SerializeField] private bool useSharedMaterial;

        [Tooltip("Expose only materials whose shader name contains one of the filters below.")]
        [SerializeField] private bool enableShaderFilter;

        [SerializeField] private List<string> shaderNameFilters = new List<string>();

        private Material[] sources;
        private Material[] instances;
        private Material[] materials = System.Array.Empty<Material>();

        public int Count => materials.Length;

        public Material this[int index] => materials[index];

        public IEnumerator<Material> GetEnumerator() => ((IEnumerable<Material>)materials).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        protected override void OnInitializing()
        {
            base.OnInitializing();
            RefreshMaterials();
        }

        /// <summary>
        /// Rebuild what this addon exposes. Runs at Awake so the copies are in place before anything else
        /// on this GameObject reaches for a material.
        /// <para>Outside play mode nothing is copied: creating a material there would leave it in the scene
        /// permanently, and it could not be destroyed afterwards.</para>
        /// </summary>
        [ContextMenu(nameof(RefreshMaterials))]
        public virtual void RefreshMaterials()
        {
            Release();
            if (component == null)
                return;

            sources ??= component.sharedMaterials;

            if (useSharedMaterial || !Application.isPlaying)
            {
                materials = BuildView(sources);
                return;
            }

            instances = new Material[sources.Length];
            for (int i = 0; i < sources.Length; i++)
                instances[i] = sources[i] == null ? null : new Material(sources[i]);

            component.sharedMaterials = instances;
            materials = BuildView(instances);
        }

        /// <summary>
        /// Restore the authored assets and destroy every copy this addon made. Iterates the owned array and
        /// never the renderer's, so a material this addon did not create can never reach <c>Destroy</c>.
        /// </summary>
        private void Release()
        {
            materials = System.Array.Empty<Material>();
            if (instances == null)
                return;

            if (component != null)
                component.sharedMaterials = sources;

            foreach (var instance in instances)
                if (instance != null)
                    Destroy(instance);

            instances = null;
        }

        protected virtual void OnDestroy() => Release();

        /// <summary>
        /// Compact <paramref name="candidates"/> into what callers see. The shader filter applies here alone:
        /// the array written back to the renderer keeps every slot, because its length is the submesh count
        /// and a missing slot stops that submesh being drawn.
        /// </summary>
        private Material[] BuildView(Material[] candidates)
        {
            var view = new List<Material>(candidates.Length);
            foreach (var material in candidates)
                if (material != null && PassesShaderFilter(material))
                    view.Add(material);
            return view.ToArray();
        }

        private bool PassesShaderFilter(Material material)
        {
            if (!enableShaderFilter || shaderNameFilters.IsEmpty())
                return true;

            foreach (var filter in shaderNameFilters)
                if (material.shader.name.Contains(filter))
                    return true;
            return false;
        }
    }

    /// <summary>
    /// The <see cref="RendererAddon{TRenderer}"/> to attach when the Renderer needs nothing beyond its
    /// materials. A Renderer with its own concerns gets a subclass instead — see <c>LineRendererAddon</c>.
    /// </summary>
    public class RendererAddon : RendererAddon<Renderer> { }
}
