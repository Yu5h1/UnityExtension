using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Yu5h1Lib.MVVM;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>A binding that reads and writes one field of a Preferences save.</summary>
    public readonly struct PreferencesConsumer
    {
        public readonly string Key;
        public readonly string Source;

        public PreferencesConsumer(string key, string source)
        {
            Key = key;
            Source = source;
        }
    }

    /// <summary>
    /// Collects every binding in the loaded scenes that consumes a given Preferences save (identified by
    /// its <c>KEY</c>). uGUI <c>_bindings</c> are built in; a package whose bindings live elsewhere (UI
    /// Toolkit's UXML) registers its own provider, since this assembly cannot reference it.
    /// </summary>
    public static class PreferencesConsumerRegistry
    {
        private const string BindingsPropertyName = "_bindings";

        private static readonly List<Func<string, IEnumerable<PreferencesConsumer>>> providers =
            new List<Func<string, IEnumerable<PreferencesConsumer>>> { CollectUguiBindings };

        public static void Register(Func<string, IEnumerable<PreferencesConsumer>> provider)
        {
            if (!providers.Contains(provider))
                providers.Add(provider);
        }

        public static List<PreferencesConsumer> Collect(string storageKey)
        {
            var result = new List<PreferencesConsumer>();
            if (string.IsNullOrEmpty(storageKey))
                return result;
            foreach (var provider in providers)
            {
                try
                {
                    result.AddRange(provider(storageKey));
                }
                catch (Exception e)
                {
                    $"PreferencesConsumerRegistry: a provider failed: {e.Message}".printWarning();
                }
            }
            return result;
        }

        private static IEnumerable<PreferencesConsumer> CollectUguiBindings(string storageKey)
        {
            foreach (var behaviour in ObjectUtility.FindObjects<MonoBehaviour>())
            {
                if (!(behaviour is IPreferences host) || host.KEY != storageKey)
                    continue;
                var bindings = new SerializedObject(behaviour).FindProperty(BindingsPropertyName);
                if (bindings == null)
                    continue;
                for (int i = 0; i < bindings.arraySize; i++)
                    if (bindings.GetArrayElementAtIndex(i).objectReferenceValue is Component control)
                        yield return new PreferencesConsumer(
                            control is IValuePort port ? port.GetFieldName() : control.gameObject.name,
                            $"uGUI '{control.name}' on '{behaviour.name}'");
            }
        }
    }
}
