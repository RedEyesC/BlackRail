using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TrackEditor
{
    public static class TimelineJson
    {
        private const int Version = 1;
        private const string Format = "TrackEditor";

        public static string Serialize(Asset asset)
        {
            asset.Validate();
            var json = new StringBuilder();
            WriteNode(json, asset, 0, document: true);
            return json.ToString();
        }

        public static Asset Deserialize(string json, Type assetType = null)
        {
            if (string.IsNullOrWhiteSpace(json) || json.TrimStart()[0] != '{')
            {
                return null;
            }

            var document = new Reader(json).ReadObject();
            if (!document.TryGetValue("format", out var format) || format[0] != '"' || new Reader(format).ReadString() != Format)
            {
                return null;
            }

            if (Required(document, "version") != Version.ToString(CultureInfo.InvariantCulture))
            {
                throw new FormatException("Unsupported timeline JSON version.");
            }

            var expectedType = assetType ?? typeof(Asset);
            var type = ReadType(document);
            if (!typeof(Asset).IsAssignableFrom(type) || !expectedType.IsAssignableFrom(type))
            {
                return null;
            }

            var asset = (Asset)ReadNode(document, type, null);
            asset.Validate();
            return asset;
        }

        public static T Clone<T>(T node) where T : DirectableAsset
        {
            var json = new StringBuilder();
            WriteNode(json, node, 0);
            return (T)ReadNode(new Reader(json.ToString()).ReadObject(), node.GetType(), null);
        }

        private static void WriteNode(StringBuilder json, DirectableAsset node, int depth, bool document = false)
        {
            var indent = new string(' ', depth * 2);
            var fieldIndent = indent + "  ";
            json.AppendLine("{");
            if (document)
            {
                json.Append(fieldIndent).Append("\"format\": \"").Append(Format).AppendLine("\",");
                json.Append(fieldIndent).Append("\"version\": ").Append(Version).AppendLine(",");
            }

            var typeName = node.GetType().FullName.Replace("\\", "\\\\").Replace("\"", "\\\"");
            json.Append(fieldIndent).Append("\"type\": \"").Append(typeName).AppendLine("\",");
            json.Append(fieldIndent).Append("\"data\": ").Append(JsonUtility.ToJson(node));

            switch (node)
            {
                case Asset asset:
                    WriteChildren(json, "groups", asset.groups, depth);
                    break;
                case Group group:
                    WriteChildren(json, "tracks", group.Tracks, depth);
                    break;
                case Track track:
                    WriteChildren(json, "clips", track.Clips, depth);
                    break;
            }

            json.AppendLine().Append(indent).Append('}');
        }

        private static void WriteChildren(StringBuilder json, string key, IReadOnlyList<DirectableAsset> children, int depth)
        {
            var indent = new string(' ', (depth + 1) * 2);
            json.AppendLine(",").Append(indent).Append('"').Append(key).Append("\": [");
            for (var i = 0; i < children.Count; i++)
            {
                if (i > 0)
                {
                    json.Append(',');
                }

                json.AppendLine().Append(indent).Append("  ");
                WriteNode(json, children[i], depth + 2);
            }

            if (children.Count > 0)
            {
                json.AppendLine().Append(indent);
            }

            json.Append(']');
        }

        private static DirectableAsset ReadNode(Dictionary<string, string> fields, Type type, DirectableAsset parent)
        {
            var data = Required(fields, "data");
            if (data[0] != '{')
            {
                throw new FormatException("Timeline node data must be a JSON object.");
            }

            var node = (DirectableAsset)Activator.CreateInstance(type);
            JsonUtility.FromJsonOverwrite(data, node);
            node._parent = parent;

            switch (node)
            {
                case Asset asset:
                    ReadChildren(fields, "groups", asset.groups, asset);
                    break;
                case Group group:
                    ReadChildren(fields, "tracks", group.Tracks, group);
                    break;
                case Track track:
                    ReadChildren(fields, "clips", track.Clips, track);
                    break;
            }

            return node;
        }

        private static void ReadChildren<T>(
            Dictionary<string, string> fields, string key, List<T> children, DirectableAsset parent) where T : DirectableAsset
        {
            foreach (var value in new Reader(Required(fields, key)).ReadArray())
            {
                var child = new Reader(value).ReadObject();
                var type = ReadType(child);
                if (!typeof(T).IsAssignableFrom(type))
                {
                    throw new FormatException($"Invalid timeline child type '{type.Name}' under '{parent.name}'.");
                }

                children.Add((T)ReadNode(child, type, parent));
            }
        }

        private static Type ReadType(Dictionary<string, string> fields)
        {
            var name = new Reader(Required(fields, "type")).ReadString();
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(name, false))
                .FirstOrDefault(found => found != null);
            if (type == null || type.IsAbstract || type.ContainsGenericParameters || !typeof(DirectableAsset).IsAssignableFrom(type))
            {
                throw new FormatException($"Invalid timeline type '{name}'.");
            }

            return type;
        }

        private static string Required(Dictionary<string, string> fields, string key) =>
            fields.TryGetValue(key, out var value) ? value : throw new FormatException($"Missing timeline field '{key}'.");

        // Reads the timeline structure. Unity handles the serialized fields of each node.
        private sealed class Reader
        {
            private readonly string text;
            private int position;

            public Reader(string text) => this.text = text;

            public Dictionary<string, string> ReadObject()
            {
                Expect('{');
                var fields = new Dictionary<string, string>(StringComparer.Ordinal);
                if (!Take('}'))
                {
                    do
                    {
                        var key = ReadString();
                        Expect(':');
                        fields[key] = ReadValue();
                    }
                    while (Take(','));

                    Expect('}');
                }

                End();
                return fields;
            }

            public IEnumerable<string> ReadArray()
            {
                Expect('[');
                if (!Take(']'))
                {
                    do
                    {
                        yield return ReadValue();
                    }
                    while (Take(','));

                    Expect(']');
                }

                End();
            }

            private string ReadValue()
            {
                SkipSpace();
                var start = position;
                SkipValue(0);
                if (position == start)
                {
                    throw InvalidJson();
                }

                return text.Substring(start, position - start);
            }

            private void SkipValue(int depth)
            {
                if (depth >= 64 || position >= text.Length)
                {
                    throw InvalidJson();
                }

                var first = text[position];
                if (first == '"')
                {
                    ReadString(decode: false);
                    return;
                }

                if (first == '{' || first == '[')
                {
                    var close = first == '{' ? '}' : ']';
                    position++;
                    while (position < text.Length)
                    {
                        var next = text[position];
                        if (next == close)
                        {
                            position++;
                            return;
                        }

                        if (next == '{' || next == '[' || next == '"')
                        {
                            SkipValue(depth + 1);
                        }
                        else if (next == '}' || next == ']')
                        {
                            throw InvalidJson();
                        }
                        else
                        {
                            position++;
                        }
                    }

                    throw InvalidJson();
                }

                while (position < text.Length && !char.IsWhiteSpace(text[position])
                       && text[position] != ',' && text[position] != '}' && text[position] != ']')
                {
                    position++;
                }
            }

            public string ReadString(bool decode = true)
            {
                Expect('"');
                var result = decode ? new StringBuilder() : null;
                while (position < text.Length)
                {
                    var character = text[position++];
                    if (character == '"')
                    {
                        return result?.ToString();
                    }

                    if (character < ' ')
                    {
                        throw InvalidJson();
                    }

                    if (character == '\\')
                    {
                        character = ReadEscape();
                    }

                    result?.Append(character);
                }

                throw InvalidJson();
            }

            private char ReadEscape()
            {
                if (position >= text.Length)
                {
                    throw InvalidJson();
                }

                switch (text[position++])
                {
                    case '"': return '"';
                    case '\\': return '\\';
                    case '/': return '/';
                    case 'b': return '\b';
                    case 'f': return '\f';
                    case 'n': return '\n';
                    case 'r': return '\r';
                    case 't': return '\t';
                    case 'u':
                        if (position + 4 <= text.Length && ushort.TryParse(text.Substring(position, 4),
                                NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var code))
                        {
                            position += 4;
                            return (char)code;
                        }

                        break;
                }

                throw InvalidJson();
            }

            private bool Take(char expected)
            {
                SkipSpace();
                if (position >= text.Length || text[position] != expected)
                {
                    return false;
                }

                position++;
                return true;
            }

            private void Expect(char character)
            {
                if (!Take(character))
                {
                    throw InvalidJson();
                }
            }

            private void SkipSpace()
            {
                while (position < text.Length && char.IsWhiteSpace(text[position]))
                {
                    position++;
                }
            }

            private void End()
            {
                SkipSpace();
                if (position != text.Length)
                {
                    throw InvalidJson();
                }
            }

            private FormatException InvalidJson() => new FormatException($"Invalid timeline JSON at character {position + 1}.");
        }
    }

    [Serializable]
    public struct TimelineAssetReference
    {
        public string guid;
        public long fileId;
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TimelineAssetReferenceAttribute : Attribute
    {
        public TimelineAssetReferenceAttribute(Type type)
        {
            Type = type;
        }

        public Type Type { get; }
    }
}
