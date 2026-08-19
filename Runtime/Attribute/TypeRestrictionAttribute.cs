using UnityEngine;
using Type = System.Type;

namespace Yu5h1Lib
{
    /// <summary>
    /// Restricts what an object-reference field accepts, and clears anything that does not qualify.
    /// A restriction type may be an interface: <see cref="Mode.Include"/> and <see cref="Mode.Exclude"/>
    /// test with <c>IsAssignableFrom</c>, and a dragged GameObject is resolved through <c>GetComponent</c>.
    /// </summary>
    public class TypeRestrictionAttribute : PropertyAttribute
    {
        /// <summary>
        /// How <see cref="types"/> is applied. <see cref="Include"/> keeps assignable objects,
        /// <see cref="Exclude"/> rejects them.
        /// <para><see cref="Exact"/> compares the dragged object's concrete type for equality, so it is
        /// meaningful only for classes. Pairing it with an interface rejects everything — an interface type
        /// never equals a concrete type — which looks like a broken field rather than a misconfigured one.
        /// Use <see cref="Include"/> for an interface.</para>
        /// </summary>
        public enum Mode { Include, Exclude, Exact }
        public readonly Mode mode;
        public readonly Type[] types;
        public readonly string[] assemblies;

        public TypeRestrictionAttribute(Mode mode, params Type[] types)
        {
            this.mode = mode;
            this.types = types;
            this.assemblies = null;
        }

        public TypeRestrictionAttribute(params Type[] types)
            : this(Mode.Include, types) { }

        public TypeRestrictionAttribute(Mode mode, string[] assemblies, params Type[] types)
        {
            this.mode = mode;
            this.types = types;
            this.assemblies = assemblies;
        }
    }
}