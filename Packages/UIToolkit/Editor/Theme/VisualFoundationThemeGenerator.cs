using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.UIToolkit.Editor
{
    /// <summary>
    /// Produces <c>Yu5h1VisualFoundation.uss</c> from the pinned Yu5h1Lib.UX VisualFoundation design data
    /// (<c>Tests/Editor/UX~/VisualFoundation/visual-design.json</c>). The output is deterministic: the same input
    /// always yields the same bytes, so importing again leaves no diff.
    /// </summary>
    public static class VisualFoundationThemeGenerator
    {
        public const string DesignRelativePath = "Tests/Editor/UX~/VisualFoundation/visual-design.json";
        public const string OutputRelativePath = "Runtime/Resources/Yu5h1Lib.UIToolkit/Yu5h1VisualFoundation.uss";
        public const string VariablePrefix = "--yu5h1-color-";
        public const string FocusWidthVariable = "--yu5h1-focus-min-width";

        private static readonly Regex ColorToken = new Regex(
            "\"color\\.(light|dark)\\.([A-Za-z]+)\"\\s*:\\s*\\{[^}]*?\"value\"\\s*:\\s*\"#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})\"",
            RegexOptions.CultureInvariant);

        private static readonly Regex FocusToken = new Regex(
            "\"focusMinWidth\"\\s*:\\s*\\{[^}]*?\"value\"\\s*:\\s*([0-9.]+)",
            RegexOptions.CultureInvariant);

        /// <summary>The roles of one scheme in design-data order, with their <c>#rrggbb</c> or <c>#rrggbbaa</c> values.</summary>
        public static List<KeyValuePair<string, string>> ReadRoles(string designJson, string scheme)
        {
            var roles = new List<KeyValuePair<string, string>>();
            foreach (Match match in ColorToken.Matches(designJson))
            {
                if (match.Groups[1].Value != scheme) continue;
                roles.Add(new KeyValuePair<string, string>(match.Groups[2].Value, "#" + match.Groups[3].Value.ToLowerInvariant()));
            }
            return roles;
        }

        public static float ReadFocusMinWidth(string designJson)
        {
            var match = FocusToken.Match(designJson);
            if (!match.Success) throw new InvalidDataException("focusMinWidth is missing from visual-design.json");
            return float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        /// <summary>The USS variable name of a role, for example <c>pageBackground</c> to <c>--yu5h1-color-page-background</c>.</summary>
        public static string VariableName(string role)
        {
            var name = new StringBuilder(VariablePrefix);
            foreach (var character in role)
            {
                if (char.IsUpper(character)) name.Append('-').Append(char.ToLowerInvariant(character));
                else name.Append(character);
            }
            return name.ToString();
        }

        public static string Generate(string designJson, string designSha256)
        {
            var focusWidth = ReadFocusMinWidth(designJson);
            var text = new StringBuilder();
            text.Append("/* Generated from the pinned Yu5h1Lib.UX VisualFoundation design data (visual-design.json, SHA-256 ")
                .Append(designSha256)
                .Append(").\n   Do not edit: run Tools > Yu5h1Lib > UIToolkit > Regenerate VisualFoundation Theme. */\n");
            foreach (var scheme in new[] { "light", "dark" })
            {
                var roles = ReadRoles(designJson, scheme);
                if (roles.Count == 0) throw new InvalidDataException("No " + scheme + " colour roles in visual-design.json");
                text.Append("\n.yu5h1-theme--").Append(scheme).Append(" {\n");
                foreach (var role in roles)
                {
                    text.Append("    ").Append(VariableName(role.Key)).Append(": ").Append(role.Value).Append(";\n");
                }
                text.Append("    ").Append(FocusWidthVariable).Append(": ")
                    .Append(focusWidth.ToString("0.##", CultureInfo.InvariantCulture)).Append("px;\n}\n");
            }
            return text.ToString();
        }

        public static string Sha256(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        public static string PackagePath()
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(VisualFoundationThemeGenerator).Assembly);
            if (package == null) throw new InvalidOperationException("Yu5h1Lib.UIToolkit package not found");
            return package.resolvedPath;
        }

        public static string GenerateFromPinnedDesign()
        {
            var bytes = File.ReadAllBytes(Path.Combine(PackagePath(), DesignRelativePath));
            return Generate(Encoding.UTF8.GetString(bytes), Sha256(bytes));
        }

        [MenuItem("Tools/Yu5h1Lib/UIToolkit/Regenerate VisualFoundation Theme")]
        public static void Regenerate()
        {
            var output = Path.Combine(PackagePath(), OutputRelativePath);
            File.WriteAllText(output, GenerateFromPinnedDesign(), new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log("VisualFoundation theme regenerated: " + output);
        }
    }
}
