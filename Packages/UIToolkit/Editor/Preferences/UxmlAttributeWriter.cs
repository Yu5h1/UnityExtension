using System.IO;
using System.Net;
using System.Security;
using System.Text;
using UnityEditor;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// Rewrites one attribute value in a UXML file in place. Only the characters between the quotes
    /// change: re-saving the whole document through an XML API would normalize line endings, quotes and
    /// spacing, and turn an escaped <c>&amp;#10;</c> in another attribute into a literal newline.
    /// </summary>
    public static class UxmlAttributeWriter
    {
        /// <param name="line">1-based line of the attribute name, as <c>IXmlLineInfo</c> reports it.</param>
        /// <param name="column">1-based column of the attribute name.</param>
        /// <param name="expectedValue">The value scanned earlier; the write is refused if the file no longer has it.</param>
        public static bool TryReplace(string assetPath, int line, int column, string attributeName,
            string expectedValue, string newValue, out string error)
        {
            error = null;
            var physicalPath = FileUtil.GetPhysicalPath(assetPath);
            var bytes = File.ReadAllBytes(physicalPath);
            var hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            var offset = hasBom ? 3 : 0;
            var text = Encoding.UTF8.GetString(bytes, offset, bytes.Length - offset);

            var index = 0;
            for (var l = 1; l < line; l++)
            {
                index = text.IndexOf('\n', index);
                if (index < 0)
                {
                    error = $"{assetPath} has fewer than {line} lines; rescan and try again.";
                    return false;
                }
                index++;
            }
            index += column - 1;
            if (index < 0 || index + attributeName.Length > text.Length
                || string.CompareOrdinal(text, index, attributeName, 0, attributeName.Length) != 0)
            {
                error = $"{assetPath}:{line}:{column} is no longer '{attributeName}'; the file changed since it was scanned.";
                return false;
            }

            var quote = index + attributeName.Length;
            while (quote < text.Length && text[quote] != '"' && text[quote] != '\'')
                quote++;
            var end = quote < text.Length ? text.IndexOf(text[quote], quote + 1) : -1;
            if (end < 0)
            {
                error = $"{assetPath}:{line}:{column}: cannot find the value of '{attributeName}'.";
                return false;
            }
            var current = WebUtility.HtmlDecode(text.Substring(quote + 1, end - quote - 1));
            if (current != expectedValue)
            {
                error = $"{assetPath}:{line}:{column} now holds '{current}', not '{expectedValue}'; the file changed since it was scanned.";
                return false;
            }

            text = text.Substring(0, quote + 1) + SecurityElement.Escape(newValue) + text.Substring(end);
            var encoded = Encoding.UTF8.GetBytes(text);
            using (var stream = File.Create(physicalPath))
            {
                if (hasBom)
                    stream.Write(bytes, 0, 3);
                stream.Write(encoded, 0, encoded.Length);
            }
            AssetDatabase.ImportAsset(assetPath);
            return true;
        }
    }
}
