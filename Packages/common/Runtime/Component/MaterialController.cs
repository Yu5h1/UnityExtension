using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Binds one or more material sources and works on them: writes properties on demand, and runs a
    /// <see cref="MaterialDriver"/> over them every frame.
    /// <para>A source is anything presenting an <see cref="IReadOnlyList{T}"/> of materials — a
    /// <see cref="RendererAddon"/>, a <see cref="MaterialArrayObject"/>, or a future one. Because the
    /// supplier owns whatever it supplies, this component never creates or destroys a material and does not
    /// need to know where any of them came from.</para>
    /// <para>Sources are flattened and exposed as a list rather than mirrored through per-property getters:
    /// a caller that wants to read a value, or toggle one, holds the material and does it directly.</para>
    /// <para>An empty source list does nothing. It does not fall back to a renderer on this GameObject —
    /// an implicit binding is harder to account for than a visibly empty field.</para>
    /// </summary>
    public class MaterialController : MonoBehaviour, IReadOnlyList<Material>
    {
        [Tooltip("Material suppliers. Each entry must implement IReadOnlyList<Material>.")]
        [SerializeField, TypeRestriction(typeof(IReadOnlyList<Material>))]
        private Object[] sources;

        [Tooltip("Runs over every source each frame. Swap this asset to change the behavior.")]
        [SerializeField, Inline] private MaterialDriver driver;

        public int Count
        {
            get
            {
                int count = 0;
                if (sources != null)
                    foreach (var source in sources)
                        if (source is IReadOnlyList<Material> materials)
                            count += materials.Count;
                return count;
            }
        }

        public Material this[int index]
        {
            get
            {
                if (sources != null)
                    foreach (var source in sources)
                    {
                        if (source is not IReadOnlyList<Material> materials)
                            continue;
                        if (index < materials.Count)
                            return materials[index];
                        index -= materials.Count;
                    }
                throw new System.ArgumentOutOfRangeException(nameof(index));
            }
        }

        public IEnumerator<Material> GetEnumerator()
        {
            if (sources == null)
                yield break;

            foreach (var source in sources)
            {
                if (source is not IReadOnlyList<Material> materials)
                    continue;
                for (int i = 0; i < materials.Count; i++)
                    yield return materials[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void Update()
        {
            if (driver == null || sources == null)
                return;

            foreach (var source in sources)
                if (source is IReadOnlyList<Material> materials)
                    driver.Drive(materials);
        }

        /// <summary>Swap the active driver at runtime.</summary>
        public void SetDriver(MaterialDriver value) => driver = value;

        public void SetFloat(string propertyName, float value)
        {
            int id = Shader.PropertyToID(propertyName);
            foreach (var material in this)
                if (material != null && material.HasProperty(id))
                    material.SetFloat(id, value);
        }

        public void SetInt(string propertyName, int value)
        {
            int id = Shader.PropertyToID(propertyName);
            foreach (var material in this)
                if (material != null && material.HasProperty(id))
                    material.SetInt(id, value);
        }

        public void SetBool(string propertyName, bool value) => SetFloat(propertyName, value ? 1f : 0f);

        public void SetColor(string propertyName, Color value)
        {
            int id = Shader.PropertyToID(propertyName);
            foreach (var material in this)
                if (material != null && material.HasProperty(id))
                    material.SetColor(id, value);
        }

        public void SetVector(string propertyName, Vector4 value)
        {
            int id = Shader.PropertyToID(propertyName);
            foreach (var material in this)
                if (material != null && material.HasProperty(id))
                    material.SetVector(id, value);
        }

        public void SetTexture(string propertyName, Texture value)
        {
            int id = Shader.PropertyToID(propertyName);
            foreach (var material in this)
                if (material != null && material.HasProperty(id))
                    material.SetTexture(id, value);
        }
    }
}
