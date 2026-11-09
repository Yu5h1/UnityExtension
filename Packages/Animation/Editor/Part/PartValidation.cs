using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    public sealed class PartDescription
    {
        public string format;
        public string formatVersion;
        public string id;
        public string version;
        public string kind;
        public string glb;
        public string targetBone;
        public Vector3 offsetPosition;
        public Vector3 offsetRotation;
        public Vector3 rawOffsetPosition;
        public Vector3 rawOffsetRotation;
    }

    public static class PartValidation
    {
        public const string ExpectedFormat = "yu5h1.part.draft";
        public const string ExpectedFormatVersion = "0.1.0";

        static readonly Regex IdPattern = new Regex(@"^[a-z][a-z0-9]*(?:[._-][a-z0-9]+)*$", RegexOptions.Compiled);
        static readonly Regex VersionPattern = new Regex(@"^\d+\.\d+\.\d+$", RegexOptions.Compiled);
        static readonly Regex KindPattern = new Regex(@"^[a-z][a-z0-9]*(?:\.[a-z][a-z0-9-]*)*$", RegexOptions.Compiled);
        static readonly Regex BonePattern = new Regex(@"^[A-Za-z0-9_.-]{1,64}$", RegexOptions.Compiled);
        static readonly Regex GlbPattern = new Regex(@"^[A-Za-z0-9_.-]+\.glb$", RegexOptions.Compiled);

        static readonly HashSet<string> ValidKinds = new HashSet<string>
        {
            "weapon", "helmet", "hair", "clothing", "accessory"
        };

        public static bool Validate(string jsonText, out PartDescription description, out string errorMessage)
        {
            description = null;
            errorMessage = null;

            object parsed;
            try
            {
                parsed = MiniJson.Deserialize(jsonText);
            }
            catch (Exception ex)
            {
                errorMessage = $"[INVALID_JSON] /: {ex.Message}";
                return false;
            }

            if (!(parsed is Dictionary<string, object> root))
            {
                errorMessage = "[INVALID_DATA] /: must be an object";
                return false;
            }

            var allowedRoot = new HashSet<string> { "format", "formatVersion", "id", "version", "kind", "glb", "attach", "source" };
            foreach (var key in root.Keys)
            {
                if (!allowedRoot.Contains(key))
                {
                    errorMessage = $"[INVALID_DATA] /{key}: unknown field \"{key}\"";
                    return false;
                }
            }

            string[] requiredRoot = { "format", "formatVersion", "id", "version", "kind", "glb", "attach" };
            foreach (var req in requiredRoot)
            {
                if (!root.ContainsKey(req))
                {
                    errorMessage = $"[INVALID_DATA] /{req}: is required";
                    return false;
                }
            }

            string format = root["format"] as string;
            if (format != ExpectedFormat)
            {
                errorMessage = $"[INVALID_DATA] /format: must be \"{ExpectedFormat}\"";
                return false;
            }

            string formatVersion = root["formatVersion"] as string;
            if (formatVersion != ExpectedFormatVersion)
            {
                errorMessage = $"[UNSUPPORTED_VERSION] /formatVersion: must be \"{ExpectedFormatVersion}\"";
                return false;
            }

            string id = root["id"] as string;
            if (string.IsNullOrEmpty(id) || id.Length > 64 || !IdPattern.IsMatch(id))
            {
                errorMessage = "[INVALID_DATA] /id: must be lowercase words joined by \".\", \"-\" or \"_\", starting with a letter (at most 64 characters)";
                return false;
            }

            string version = root["version"] as string;
            if (string.IsNullOrEmpty(version) || !VersionPattern.IsMatch(version))
            {
                errorMessage = "[INVALID_DATA] /version: must be a version such as \"1.0.0\"";
                return false;
            }

            string kind = root["kind"] as string;
            if (string.IsNullOrEmpty(kind) || !KindPattern.IsMatch(kind))
            {
                errorMessage = "[INVALID_DATA] /kind: must start with one of weapon, helmet, hair, clothing, accessory, optionally followed by \".subtype\"";
                return false;
            }
            string firstKind = kind.Split('.')[0];
            if (!ValidKinds.Contains(firstKind))
            {
                errorMessage = "[INVALID_DATA] /kind: must start with one of weapon, helmet, hair, clothing, accessory, optionally followed by \".subtype\"";
                return false;
            }

            string glb = root["glb"] as string;
            if (string.IsNullOrEmpty(glb) || !GlbPattern.IsMatch(glb))
            {
                errorMessage = "[INVALID_DATA] /glb: must be a file name ending in \".glb\" with no folder";
                return false;
            }

            if (!(root["attach"] is Dictionary<string, object> attach))
            {
                errorMessage = "[INVALID_DATA] /attach: must be an object";
                return false;
            }

            var allowedAttach = new HashSet<string> { "target", "offset", "sway" };
            foreach (var key in attach.Keys)
            {
                if (!allowedAttach.Contains(key))
                {
                    errorMessage = $"[INVALID_DATA] /attach/{key}: unknown field \"{key}\"";
                    return false;
                }
            }

            if (!attach.ContainsKey("target"))
            {
                errorMessage = "[INVALID_DATA] /attach/target: is required";
                return false;
            }
            string target = attach["target"] as string;
            if (string.IsNullOrEmpty(target) || !BonePattern.IsMatch(target))
            {
                errorMessage = "[INVALID_DATA] /attach/target: must be a bone name";
                return false;
            }

            Vector3 rawOffsetPos = Vector3.zero;
            Vector3 rawOffsetRot = Vector3.zero;

            if (attach.TryGetValue("offset", out var offsetObj) && offsetObj != null)
            {
                if (!(offsetObj is Dictionary<string, object> offset))
                {
                    errorMessage = "[INVALID_DATA] /attach/offset: must be an object";
                    return false;
                }

                var allowedOffset = new HashSet<string> { "position", "rotation" };
                foreach (var key in offset.Keys)
                {
                    if (!allowedOffset.Contains(key))
                    {
                        errorMessage = $"[INVALID_DATA] /attach/offset/{key}: unknown field \"{key}\"";
                        return false;
                    }
                }

                if (offset.TryGetValue("position", out var posObj) && posObj != null)
                {
                    if (!TryReadTriple(posObj, out rawOffsetPos))
                    {
                        errorMessage = "[INVALID_DATA] /attach/offset/position: must be an array of 3 finite numbers";
                        return false;
                    }
                }

                if (offset.TryGetValue("rotation", out var rotObj) && rotObj != null)
                {
                    if (!TryReadTriple(rotObj, out rawOffsetRot))
                    {
                        errorMessage = "[INVALID_DATA] /attach/offset/rotation: must be an array of 3 finite numbers";
                        return false;
                    }
                }
            }

            description = new PartDescription
            {
                format = format,
                formatVersion = formatVersion,
                id = id,
                version = version,
                kind = kind,
                glb = glb,
                targetBone = target,
                rawOffsetPosition = rawOffsetPos,
                rawOffsetRotation = rawOffsetRot,
                offsetPosition = new Vector3(-rawOffsetPos.x, rawOffsetPos.y, rawOffsetPos.z),
                offsetRotation = new Vector3(rawOffsetRot.x, -rawOffsetRot.y, -rawOffsetRot.z)
            };
            return true;
        }

        static bool TryReadTriple(object obj, out Vector3 v)
        {
            v = Vector3.zero;
            if (!(obj is List<object> list) || list.Count != 3)
                return false;
            float[] values = new float[3];
            for (int i = 0; i < 3; i++)
            {
                if (list[i] is double d && !double.IsNaN(d) && !double.IsInfinity(d))
                    values[i] = (float)d;
                else if (list[i] is float f && !float.IsNaN(f) && !float.IsInfinity(f))
                    values[i] = f;
                else if (list[i] is long l)
                    values[i] = (float)l;
                else if (list[i] is int val)
                    values[i] = val;
                else
                    return false;
            }
            v = new Vector3(values[0], values[1], values[2]);
            return true;
        }
    }
}
