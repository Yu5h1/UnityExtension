using UnityEngine;
using Type = System.Type;

namespace Yu5h1Lib
{
    public class TypeRestrictionAttribute : PropertyAttribute
    {
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