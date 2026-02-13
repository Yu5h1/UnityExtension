using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// Central registry for Decorable pattern.
    /// Drawer types register their static draw methods at editor load.
    /// InlineDrawer registers per-instance drawer type at draw time.
    /// DecoratableDrawer queries: instanceID → Type → drawMethod.
    /// </summary>
    public static class DecoratorProvider
    {
        public delegate void DecoratorDraw(
            PropertyDrawer drawer,
            Rect position,
            SerializedProperty property,
            GUIContent label
        );

        // Static registry: attribute type → draw method (registered once at editor load)
        private static readonly Dictionary<Type, DecoratorDraw> _drawMethods = new();

        // Dynamic registry: SO instanceID → drawer attribute type (registered per-frame by InlineDrawer)
        private static readonly Dictionary<int, Type> _drawerTypes = new();

        /// <summary>
        /// Register a draw method for a specific attribute type.
        /// Called by each drawer in [InitializeOnLoadMethod].
        /// </summary>
        public static void RegisterDrawMethod(Type attributeType, DecoratorDraw method)
        {
            if (attributeType == null || method == null) return;
            _drawMethods[attributeType] = method;
        }

        /// <summary>
        /// Set the drawer type for a specific SO instance.
        /// Called by InlineDrawer when it reads [Decorator] from the field.
        /// </summary>
        public static void SetDrawerType(int instanceID, Type attributeType)
        {
            if (attributeType == null) return;
            _drawerTypes[instanceID] = attributeType;
        }

        /// <summary>
        /// Query: instanceID → Type → drawMethod (two-step lookup)
        /// </summary>
        public static bool TryGetDrawMethod(int instanceID, out DecoratorDraw method)
        {
            method = null;
            return _drawerTypes.TryGetValue(instanceID, out var type)
                && _drawMethods.TryGetValue(type, out method);
        }

        /// <summary>
        /// Query directly by attribute type
        /// </summary>
        public static bool TryGetDrawMethod(Type attributeType, out DecoratorDraw method)
            => _drawMethods.TryGetValue(attributeType, out method);

        public static void ClearInstance(int instanceID)
            => _drawerTypes.Remove(instanceID);
    }
}
