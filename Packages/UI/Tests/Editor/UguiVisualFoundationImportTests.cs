using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Yu5h1Lib.UI.Tests
{
    /// <summary>Verifies the pinned UX source, imported Theme assets and existing uGUI bindings.</summary>
    public sealed class UguiVisualFoundationImportTests
    {
        private const string AssetRelativePath = "Runtime/Resources/Yu5h1Lib.UI/VisualFoundation.yu5h1visualfoundation";
        private const string ResourcePath = "Yu5h1Lib.UI/VisualFoundation";
        private const string SourceSha256 = "ffcfb60703205089157835e6c15348798c43eb4b59b2df4a488b20a4b7e73250";

        [Test]
        public void ResourceThemesContainEveryPinnedSrgbColor()
        {
            var bytes = ReadSource();
            using var sha256 = SHA256.Create();
            var hash = BitConverter.ToString(sha256.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            Assert.That(hash, Is.EqualTo(SourceSha256));

            var themes = Resources.LoadAll<Theme>(ResourcePath);
            Assert.That(themes.Select(theme => theme.name), Is.EquivalentTo(new[] { "Light", "Dark" }));
            var source = Encoding.UTF8.GetString(bytes);
            foreach (var theme in themes)
            {
                var scheme = theme.name.ToLowerInvariant();
                var tokens = Regex.Matches(source, "\"color\\." + scheme + "\\.(?<role>\\w+)\"\\s*:\\s*\\{\\s*\"value\"\\s*:\\s*\"(?<value>#[a-fA-F0-9]+)\"")
                    .Cast<Match>()
                    .ToDictionary(match => match.Groups["role"].Value, match => match.Groups["value"].Value);

                Assert.That(tokens.Count, Is.EqualTo(17), scheme);
                Assert.That(theme.items.Select(parameter => parameter.name), Is.EquivalentTo(tokens.Keys), scheme);
                foreach (var token in tokens)
                {
                    var parameter = theme.items.Single(item => item.name == token.Key);
                    Assert.That(parameter, Is.TypeOf<ColorObject>(), token.Key);
                    Assert.That(theme.TryGet<Color>(token.Key, out var color), Is.True, token.Key);

                    var expected = token.Value.ToLowerInvariant().TrimStart('#');
                    if (expected.Length == 6)
                        expected += "ff";
                    Assert.That(ColorUtility.ToHtmlStringRGBA(color).ToLowerInvariant(), Is.EqualTo(expected), $"{scheme}.{token.Key}");
                }
            }
        }

        [Test]
        public void FreshImportAndReimportProduceStableSubassetIdentifiers()
        {
            var path = AssetDatabase.GenerateUniqueAssetPath("Assets/UguiVisualFoundationImportTest.yu5h1visualfoundation");
            try
            {
                File.WriteAllBytes(path, ReadSource());
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var original = AssetIdentifiers(path);
                Assert.That(original.Count, Is.EqualTo(37));

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                var reimported = AssetIdentifiers(path);
                Assert.That(reimported, Is.EqualTo(original));
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        [Test]
        public void ExistingGraphicBindingFollowsExplicitKeyAndThemeSchema()
        {
            var light = Resources.LoadAll<Theme>(ResourcePath).Single(theme => theme.name == "Light");
            var overlay = light.items.Single(parameter => parameter.name == "overlay");
            var local = ScriptableObject.CreateInstance<Theme>();
            var binding = ScriptableObject.CreateInstance<GraphicColorBinding>();
            var target = new GameObject("Bound graphic", typeof(RectTransform), typeof(Image));
            try
            {
                using (var serializedLocal = new SerializedObject(local))
                {
                    var items = serializedLocal.FindProperty("_items");
                    items.arraySize = 1;
                    items.GetArrayElementAtIndex(0).objectReferenceValue =
                        light.items.Single(parameter => parameter.name == "accent");
                    serializedLocal.FindProperty("_schema").objectReferenceValue = light;
                    serializedLocal.ApplyModifiedPropertiesWithoutUndo();
                }
                using (var serializedBinding = new SerializedObject(binding))
                {
                    serializedBinding.FindProperty("_keyRef").objectReferenceValue = overlay;
                    var targets = serializedBinding.FindProperty("_targets");
                    targets.arraySize = 1;
                    targets.GetArrayElementAtIndex(0).objectReferenceValue = target.GetComponent<Image>();
                    serializedBinding.ApplyModifiedPropertiesWithoutUndo();
                }

                binding.name = "Application Binding";
                binding.Apply(light);
                Assert.That((Color32)target.GetComponent<Image>().color, Is.EqualTo(new Color32(0x17, 0x23, 0x32, 0x66)));
                target.GetComponent<Image>().color = Color.white;
                binding.Apply(local);
                Assert.That((Color32)target.GetComponent<Image>().color, Is.EqualTo(new Color32(0x17, 0x23, 0x32, 0x66)));
            }
            finally
            {
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(binding);
                Object.DestroyImmediate(local);
            }
        }

        private static byte[] ReadSource()
        {
            var package = PackageInfo.FindForAssetPath("Packages/com.yu5h1.ui/package.json");
            return File.ReadAllBytes(Path.Combine(package.resolvedPath, AssetRelativePath));
        }

        private static Dictionary<string, long> AssetIdentifiers(string path)
        {
            var result = new Dictionary<string, long>(StringComparer.Ordinal);
            var marker = AssetDatabase.LoadMainAssetAtPath(path);
            Assert.That(marker, Is.TypeOf<TextAsset>());
            result.Add("source", LocalIdentifier(marker));
            var themes = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Theme>().ToArray();
            Assert.That(themes.Select(theme => theme.name), Is.EquivalentTo(new[] { "Light", "Dark" }));
            foreach (var theme in themes)
            {
                result.Add(theme.name, LocalIdentifier(theme));
                foreach (var parameter in theme.items)
                    result.Add(theme.name + "." + parameter.name, LocalIdentifier(parameter));
            }

            Assert.That(result.Values.Distinct().Count(), Is.EqualTo(result.Count));
            return result;
        }

        private static long LocalIdentifier(Object asset)
        {
            Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out _, out long id), Is.True, asset.name);
            return id;
        }
    }
}
