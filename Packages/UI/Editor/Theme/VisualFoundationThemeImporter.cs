using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>Imports the pinned UX VisualFoundation color data as reusable Theme profiles.</summary>
    [ScriptedImporter(1, "yu5h1visualfoundation")]
    internal sealed class VisualFoundationThemeImporter : ScriptedImporter
    {
        private const string ContractVersion = "1.3.0";
        private const string DesignSha256 = "ffcfb60703205089157835e6c15348798c43eb4b59b2df4a488b20a4b7e73250";

        private static readonly string[] Roles =
        {
            "pageBackground", "panelBackground", "heading", "panelHeading", "itemText", "secondaryText",
            "border", "accent", "hover", "focus", "onAccent", "disabled", "overlay", "danger",
            "controlBorder", "viewportSelection", "viewportActive"
        };

        private static readonly Regex ColorToken = new Regex(
            @"""color\.(?<scheme>light|dark)\.(?<role>[A-Za-z]+)""\s*:\s*\{\s*""value""\s*:\s*""(?<hex>#[0-9A-Fa-f]{6}(?:[0-9A-Fa-f]{2})?)""\s*,\s*""unit""\s*:\s*""color""",
            RegexOptions.CultureInvariant);

        public override void OnImportAsset(AssetImportContext context)
        {
            try
            {
                var colors = ReadPinnedColors(context.assetPath);
                AddTheme(context, "light", "Light", colors);
                AddTheme(context, "dark", "Dark", colors);

                var source = new TextAsset($"UX VisualFoundation {ContractVersion}\nSHA-256 {DesignSha256}")
                {
                    name = "VisualFoundation"
                };
                context.AddObjectToAsset("source", source);
                context.SetMainObject(source);
            }
            catch (InvalidDataException error)
            {
                context.LogImportError(error.Message);
            }
        }

        private static Dictionary<string, Color> ReadPinnedColors(string path)
        {
            var bytes = File.ReadAllBytes(path);
            using var sha256 = SHA256.Create();
            var hash = BitConverter.ToString(sha256.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            if (hash != DesignSha256)
                throw new InvalidDataException($"VisualFoundation source SHA-256 is {hash}, expected {DesignSha256}.");

            var source = Encoding.UTF8.GetString(bytes);
            if (!Regex.IsMatch(source, @"""schemaVersion""\s*:\s*1\s*,", RegexOptions.CultureInvariant)
                || !Regex.IsMatch(source, @"""contractVersion""\s*:\s*""" + Regex.Escape(ContractVersion) + @"""", RegexOptions.CultureInvariant))
                throw new InvalidDataException($"Expected VisualFoundation schema 1 and design version {ContractVersion}.");

            var colors = new Dictionary<string, Color>(StringComparer.Ordinal);
            foreach (Match match in ColorToken.Matches(source))
            {
                var key = match.Groups["scheme"].Value + "." + match.Groups["role"].Value;
                if (!ColorUtility.TryParseHtmlString(match.Groups["hex"].Value, out var color) || !colors.TryAdd(key, color))
                    throw new InvalidDataException($"Invalid or duplicate VisualFoundation color {key}.");
            }

            foreach (var scheme in new[] { "light", "dark" })
                foreach (var role in Roles)
                    if (!colors.ContainsKey(scheme + "." + role))
                        throw new InvalidDataException($"Missing VisualFoundation color {scheme}.{role}.");

            if (colors.Count != Roles.Length * 2)
                throw new InvalidDataException("Unexpected VisualFoundation color role.");

            return colors;
        }

        private static void AddTheme(AssetImportContext context, string scheme, string name, Dictionary<string, Color> colors)
        {
            var theme = ScriptableObject.CreateInstance<Theme>();
            theme.name = name;
            context.AddObjectToAsset(scheme, theme);

            using var serializedTheme = new SerializedObject(theme);
            var items = serializedTheme.FindProperty("_items");
            items.arraySize = Roles.Length;
            for (int index = 0; index < Roles.Length; index++)
            {
                var role = Roles[index];
                var key = scheme + "." + role;
                var parameter = ScriptableObject.CreateInstance<ColorObject>();
                parameter.name = role;
                parameter.value = colors[key];
                context.AddObjectToAsset(key, parameter);
                items.GetArrayElementAtIndex(index).objectReferenceValue = parameter;
            }
            serializedTheme.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
