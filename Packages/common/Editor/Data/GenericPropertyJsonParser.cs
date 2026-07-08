using System;
using System.Globalization;
using System.Text;

namespace Yu5h1Lib.EditorExtension
{
    public static class GenericPropertyJsonParser
    {
        public const string Prefix = "GenericPropertyJSON:";

        public static bool TryGetValue(string text, string propertyName, out string value)
        {
            value = null;

            if (string.IsNullOrEmpty(propertyName) || !TryGetJson(text, out var json))
                return false;

            for (var index = 0; index < json.Length; index++)
            {
                if (json[index] == '"')
                {
                    ReadString(json, ref index);
                    index--;
                    continue;
                }

                if (json[index] != '{')
                    continue;

                var objectEnd = FindObjectEnd(json, index);
                if (objectEnd < 0)
                    return false;

                if (TryGetDirectPropertyValue(json, index, objectEnd, "name", out var name) &&
                    string.Equals(name, propertyName, StringComparison.Ordinal) &&
                    TryGetDirectPropertyValue(json, index, objectEnd, "val", out value))
                    return true;
            }

            return false;
        }

        public static bool TryGetJson(string text, out string json)
        {
            json = null;

            if (string.IsNullOrEmpty(text))
                return false;

            json = text.StartsWith(Prefix, StringComparison.Ordinal)
                ? text.Substring(Prefix.Length)
                : text;

            if (string.IsNullOrWhiteSpace(json))
            {
                json = null;
                return false;
            }

            return true;
        }

        private static bool TryGetDirectPropertyValue(
            string json,
            int objectStart,
            int objectEnd,
            string propertyName,
            out string value)
        {
            value = null;
            var index = objectStart + 1;

            while (index < objectEnd)
            {
                index = SkipWhiteSpace(json, index);

                if (index >= objectEnd)
                    return false;

                if (json[index] == ',')
                {
                    index++;
                    continue;
                }

                if (json[index] != '"')
                {
                    if (!SkipJsonValue(json, ref index))
                        return false;

                    continue;
                }

                var name = ReadString(json, ref index);
                index = SkipWhiteSpace(json, index);

                if (index >= objectEnd || json[index] != ':')
                    return false;

                index++;
                index = SkipWhiteSpace(json, index);

                if (string.Equals(name, propertyName, StringComparison.Ordinal))
                    return TryReadValue(json, ref index, out value);

                if (!SkipJsonValue(json, ref index))
                    return false;
            }

            return false;
        }

        private static bool TryReadValue(string json, ref int index, out string value)
        {
            value = null;

            if (index >= json.Length)
                return false;

            if (json[index] == '"')
            {
                value = ReadString(json, ref index);
                return true;
            }

            var tokenStart = index;
            while (index < json.Length && json[index] != ',' && json[index] != '}' && json[index] != ']')
                index++;

            value = json.Substring(tokenStart, index - tokenStart).Trim();
            return value.Length > 0;
        }

        private static bool SkipJsonValue(string json, ref int index)
        {
            if (index >= json.Length)
                return false;

            switch (json[index])
            {
                case '"':
                    ReadString(json, ref index);
                    return true;

                case '{':
                    return TrySkipBalanced(json, ref index, '{', '}');

                case '[':
                    return TrySkipBalanced(json, ref index, '[', ']');

                default:
                    while (index < json.Length && json[index] != ',' && json[index] != '}' && json[index] != ']')
                        index++;
                    return true;
            }
        }

        private static bool TrySkipBalanced(string json, ref int index, char open, char close)
        {
            var depth = 0;

            while (index < json.Length)
            {
                if (json[index] == '"')
                {
                    ReadString(json, ref index);
                    continue;
                }

                if (json[index] == open)
                {
                    depth++;
                }
                else if (json[index] == close)
                {
                    depth--;
                    if (depth == 0)
                    {
                        index++;
                        return true;
                    }
                }

                index++;
            }

            return false;
        }

        private static int FindObjectEnd(string json, int objectStart)
        {
            var index = objectStart;
            var depth = 0;

            while (index < json.Length)
            {
                if (json[index] == '"')
                {
                    ReadString(json, ref index);
                    continue;
                }

                if (json[index] == '{')
                {
                    depth++;
                }
                else if (json[index] == '}')
                {
                    depth--;
                    if (depth == 0)
                        return index;
                }

                index++;
            }

            return -1;
        }

        private static string ReadString(string json, ref int index)
        {
            index++;

            var builder = new StringBuilder();
            while (index < json.Length)
            {
                var current = json[index++];
                if (current == '"')
                    break;

                if (current != '\\' || index >= json.Length)
                {
                    builder.Append(current);
                    continue;
                }

                var escaped = json[index++];
                switch (escaped)
                {
                    case '"':
                    case '\\':
                    case '/':
                        builder.Append(escaped);
                        break;

                    case 'b':
                        builder.Append('\b');
                        break;

                    case 'f':
                        builder.Append('\f');
                        break;

                    case 'n':
                        builder.Append('\n');
                        break;

                    case 'r':
                        builder.Append('\r');
                        break;

                    case 't':
                        builder.Append('\t');
                        break;

                    case 'u':
                        if (index + 4 <= json.Length &&
                            ushort.TryParse(json.Substring(index, 4), NumberStyles.HexNumber, null, out var unicode))
                        {
                            builder.Append((char)unicode);
                            index += 4;
                        }
                        break;

                    default:
                        builder.Append(escaped);
                        break;
                }
            }

            return builder.ToString();
        }

        private static int SkipWhiteSpace(string text, int index)
        {
            while (index < text.Length && char.IsWhiteSpace(text[index]))
                index++;

            return index;
        }
    }
}
