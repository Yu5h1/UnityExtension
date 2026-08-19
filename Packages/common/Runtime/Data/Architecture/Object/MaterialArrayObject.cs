using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// A reusable set of materials held as ordinary <see cref="ParameterCollection{T}"/> data, so it
    /// inherits that family's authoring, <see cref="ParameterCollection{T}.Random"/> and
    /// <see cref="ParameterCollection{T}.GetRandomElement"/> behavior instead of restating them.
    /// <para>Serves two jobs from one set: <see cref="MoveNext"/> steps a <see cref="Renderer"/> through it,
    /// and <see cref="IReadOnlyList{T}"/> hands the same materials to a <see cref="MaterialController"/>
    /// for batch edits.</para>
    /// <para>What it holds are authored assets, not instances. Supplying them is the whole contract —
    /// this object never creates or destroys a material, so writing through it reaches the project assets.</para>
    /// </summary>
    public class MaterialArrayObject : ParameterCollection<Material>, IReadOnlyList<Material>
    {
        public int Count => value == null ? 0 : value.Length;

        public Material this[int index] => value[index];

        public IEnumerator<Material> GetEnumerator() => ((IEnumerable<Material>)value).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Advance <paramref name="renderer"/> to the material after the one it currently shows, wrapping
        /// past the end. The cursor is derived from <see cref="Renderer.sharedMaterial"/> rather than stored,
        /// so any number of renderers can share this object without sharing a position.
        /// <para>A renderer showing something outside this set starts from index 0; that is defined behavior,
        /// not a failure. <c>false</c> is reserved for a switch that could not happen at all — no renderer,
        /// or nothing to switch to.</para>
        /// </summary>
        public bool MoveNext(Renderer renderer)
        {
            if (renderer == null || value.IsEmpty())
                return false;

            renderer.sharedMaterial = value[value.Repeat(value.IndexOf(renderer.sharedMaterial) + 1)];
            return true;
        }
    }
}
