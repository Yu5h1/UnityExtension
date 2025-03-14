using UnityEngine;

namespace Yu5h1Lib
{
    public class TypeRestrictionAttribute : PropertyAttribute
    {
        public System.Type[] allowedTypes;
        public bool allowSceneObjects;
        public TypeRestrictionAttribute(bool allowSceneObjects, params System.Type[] types)
        {
            this.allowSceneObjects = allowSceneObjects;
            allowedTypes = types;
        }
        public TypeRestrictionAttribute(params System.Type[] types)
        {
            allowSceneObjects = true;
            allowedTypes = types;
        }
    }
}