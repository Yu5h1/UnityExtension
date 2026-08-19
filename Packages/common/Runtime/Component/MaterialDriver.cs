using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// ScriptableObject contract for anything that writes into a set of materials every frame.
    /// Concrete subclasses supply the strategy — see <see cref="TextureSheetDriver"/>,
    /// <see cref="TextureSequenceDriver"/> and <see cref="TextureScrollDriver"/>.
    /// <para>Takes materials rather than a <see cref="Renderer"/>, so one asset serves a renderer's
    /// copies, a <see cref="MaterialArrayObject"/>, or anything else that can present an
    /// <see cref="IReadOnlyList{T}"/> of them.</para>
    /// </summary>
    public abstract class MaterialDriver : ScriptableObject
    {
        [Tooltip("Material property to write. e.g. _BaseMap (URP Lit), _MainTex (Built-in).")]
        [SerializeField] private string _propertyName = "_BaseMap";

        /// <summary>
        /// Material property this driver writes. A material property can be a float, color, vector or
        /// texture, so the name says which property rather than which kind of value.
        /// <para>Assigning refreshes <see cref="propertyId"/>, which is why the two cannot fall out of step.</para>
        /// </summary>
        public string propertyName
        {
            get => _propertyName;
            set
            {
                _propertyName = value;
                propertyId = string.IsNullOrEmpty(value) ? 0 : Shader.PropertyToID(value);
            }
        }

        /// <summary>Cached <see cref="Shader.PropertyToID"/> of <see cref="propertyName"/>.</summary>
        public int propertyId { get; private set; }

        protected virtual void OnEnable() => propertyName = _propertyName;

        protected virtual void OnValidate() => propertyName = _propertyName;

        /// <summary>Apply this driver's logic to every material in <paramref name="materials"/> for this frame.</summary>
        public abstract void Drive(IReadOnlyList<Material> materials);
    }
}
