using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using UnityEditor;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// One effective <c>binding-path</c> in a UXML tree, and where its value is written: the element's
    /// own attribute, or the <c>AttributeOverrides</c> that replaces it from a parent file.
    /// </summary>
    public sealed class UxmlBinding
    {
        public string BindingPath;
        public string ElementType;
        public string DisplayName;
        public string DefinedIn;
        public int Line;
        public int Column;
        public bool FromOverride;

        /// <summary>Identifies the attribute holding the value. Two bindings with the same key come from
        /// one template element instanced more than once, so renaming one renames all of them.</summary>
        public string SourceKey => $"{DefinedIn}:{Line}:{Column}";
    }

    /// <summary>
    /// Lists every <c>binding-path</c> a UXML asset produces by reading the files themselves, following
    /// <c>&lt;Template src&gt;</c> into <c>&lt;Instance&gt;</c> and applying <c>AttributeOverrides</c>.
    /// Reading the XML rather than <c>CloneTree()</c> keeps each binding tied to the attribute that
    /// defines it, which is what a rename has to write.
    /// </summary>
    public static class UxmlBindingScanner
    {
        public const string BindingPathAttribute = "binding-path";
        private const string ProjectDatabasePrefix = "project://database/";

        private sealed class Override
        {
            public string Value;
            public string File;
            public int Line;
            public int Column;
        }

        public static List<UxmlBinding> Scan(string assetPath, ICollection<string> scannedFiles = null)
        {
            var result = new List<UxmlBinding>();
            ScanFile(assetPath, new List<Dictionary<string, Override>>(), result, new HashSet<string>(), scannedFiles);
            return result;
        }

        private static void ScanFile(string assetPath, List<Dictionary<string, Override>> overrides,
            List<UxmlBinding> result, HashSet<string> visiting, ICollection<string> scannedFiles)
        {
            if (string.IsNullOrEmpty(assetPath) || !visiting.Add(assetPath))
                return;
            scannedFiles?.Add(assetPath);
            XDocument doc;
            try
            {
                doc = XDocument.Load(FileUtil.GetPhysicalPath(assetPath), LoadOptions.SetLineInfo);
            }
            catch (Exception e)
            {
                $"UxmlBindingScanner: cannot read {assetPath}: {e.Message}".printWarning();
                visiting.Remove(assetPath);
                return;
            }
            var templates = new Dictionary<string, string>();
            foreach (var template in doc.Root.Elements().Where(e => e.Name.LocalName == "Template"))
            {
                var name = (string)template.Attribute("name");
                var src = ResolveSource((string)template.Attribute("src"), assetPath);
                if (!string.IsNullOrEmpty(name) && src != null)
                    templates[name] = src;
            }
            Walk(doc.Root, assetPath, templates, overrides, result, visiting, scannedFiles);
            visiting.Remove(assetPath);
        }

        private static void Walk(XElement parent, string assetPath, Dictionary<string, string> templates,
            List<Dictionary<string, Override>> overrides, List<UxmlBinding> result, HashSet<string> visiting,
            ICollection<string> scannedFiles)
        {
            foreach (var element in parent.Elements())
            {
                switch (element.Name.LocalName)
                {
                    case "Template":
                    case "Style":
                    case "AttributeOverrides":
                        continue;
                    case "Instance":
                        var templateName = (string)element.Attribute("template");
                        if (templateName != null && templates.TryGetValue(templateName, out var templatePath))
                        {
                            var layers = new List<Dictionary<string, Override>>(overrides) { ReadOverrides(element, assetPath) };
                            ScanFile(templatePath, layers, result, visiting, scannedFiles);
                        }
                        Walk(element, assetPath, templates, overrides, result, visiting, scannedFiles);
                        continue;
                }

                var name = (string)element.Attribute("name");
                var own = element.Attribute(BindingPathAttribute);
                // Unity lets the outermost template instance's override win.
                var hit = name == null ? null : overrides.Select(layer => layer.TryGetValue(name, out var o) ? o : null).FirstOrDefault(o => o != null);
                if (hit != null)
                    result.Add(Create(element, hit.Value, hit.File, hit.Line, hit.Column, true));
                else if (own != null && !string.IsNullOrEmpty(own.Value))
                {
                    var info = (IXmlLineInfo)own;
                    result.Add(Create(element, own.Value, assetPath, info.LineNumber, info.LinePosition, false));
                }
                Walk(element, assetPath, templates, overrides, result, visiting, scannedFiles);
            }
        }

        private static Dictionary<string, Override> ReadOverrides(XElement instance, string assetPath)
        {
            var layer = new Dictionary<string, Override>();
            foreach (var o in instance.Elements().Where(e => e.Name.LocalName == "AttributeOverrides"))
            {
                var target = (string)o.Attribute("element-name");
                var attr = o.Attribute(BindingPathAttribute);
                if (string.IsNullOrEmpty(target) || attr == null)
                    continue;
                var info = (IXmlLineInfo)attr;
                layer[target] = new Override { Value = attr.Value, File = assetPath, Line = info.LineNumber, Column = info.LinePosition };
            }
            return layer;
        }

        private static UxmlBinding Create(XElement element, string value, string file, int line, int column, bool fromOverride)
        {
            var type = element.Name.LocalName;
            var display = (string)element.Attribute("label");
            if (string.IsNullOrEmpty(display)) display = (string)element.Attribute("name");
            if (string.IsNullOrEmpty(display)) display = type;
            return new UxmlBinding
            {
                BindingPath = value,
                ElementType = type,
                DisplayName = display,
                DefinedIn = file,
                Line = line,
                Column = column,
                FromOverride = fromOverride,
            };
        }

        /// <summary>Resolves a <c>Template src</c> to an asset path: <c>project://database/...</c> (UI Builder),
        /// <c>/Assets/...</c> (project-relative), or a path relative to the referencing file.</summary>
        private static string ResolveSource(string src, string fromAssetPath)
        {
            if (string.IsNullOrEmpty(src))
                return null;
            if (src.StartsWith(ProjectDatabasePrefix, StringComparison.Ordinal))
            {
                src = src.Substring(ProjectDatabasePrefix.Length);
                var cut = src.IndexOfAny(new[] { '?', '#' });
                return Uri.UnescapeDataString(cut < 0 ? src : src.Substring(0, cut));
            }
            if (src.StartsWith("/", StringComparison.Ordinal))
                return src.Substring(1);
            var directory = fromAssetPath.Substring(0, Math.Max(0, fromAssetPath.LastIndexOf('/')));
            var segments = new List<string>(directory.Split('/'));
            foreach (var part in src.Replace('\\', '/').Split('/'))
            {
                if (part == "..") { if (segments.Count > 0) segments.RemoveAt(segments.Count - 1); }
                else if (part != "." && part.Length > 0) segments.Add(part);
            }
            return string.Join("/", segments);
        }
    }
}
