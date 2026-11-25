using System.Collections.Generic;
using TrackEditor;

namespace GameEditor.ActionEditor
{
    public static class ActionEventConfig
    {
        public sealed class Definition
        {
            public int EventType { get; }
            public string Name { get; }
            public ClipType ClipType { get; }
            public IReadOnlyList<string> ParameterNames { get; }
            public int ParameterCount => ParameterNames.Count;

            public Definition(int eventType, string name, ClipType clipType, params string[] parameterNames)
            {
                EventType = eventType;
                Name = name;
                ClipType = clipType;
                ParameterNames = parameterNames;
            }
        }

        // Event IDs are persisted in timeline JSON. Keep IDs stable when editing this list.
        public static IReadOnlyList<Definition> Definitions { get; } = new[]
        {
            new Definition(0, "通用事件", ClipType.Signal, "参数 1", "参数 2", "参数 3"),
            new Definition(1, "脚步音效", ClipType.Signal, "音效 ID"),
            new Definition(2, "状态窗口", ClipType.Range, "状态 ID"),
        };

        public static Definition Find(int eventType)
        {
            foreach (var definition in Definitions)
            {
                if (definition.EventType == eventType)
                {
                    return definition;
                }
            }

            return null;
        }
    }
}
