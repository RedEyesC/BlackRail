using System.Linq;
using GameEditor.ActionEditor;
using TrackEditor;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Action
{
    [CustomInspectors(typeof(ActionEventClip), true)]
    public sealed class ActionEventClipInspector : ClipInspector<ActionEventClip>
    {
        private static readonly string[] EventNames = ActionEventConfig.Definitions
            .Select(definition => definition.Name)
            .ToArray();
        private static readonly int[] EventTypes = ActionEventConfig.Definitions
            .Select(definition => definition.EventType)
            .ToArray();

        protected override bool ShowClipType => false;

        public override void OnInspectorGUI()
        {
            ShowCommonInspector(showBaseInspector: false);

            EditorGUI.BeginChangeCheck();
            var eventType = EditorGUILayout.IntPopup("事件类型", action.EventType, EventNames, EventTypes);
            var changed = EditorGUI.EndChangeCheck();
            var definition = ActionEventConfig.Find(eventType);
            if (changed && definition != null)
            {
                action.EventType = eventType;
                action.Type = definition.ClipType;
            }

            if (definition == null)
            {
                EditorGUILayout.HelpBox($"事件类型 {action.EventType} 尚未配置。", MessageType.Warning);
                return;
            }

            for (var index = 0; index < definition.ParameterCount; index++)
            {
                var currentValue = action.GetParameter(index);
                var value = EditorGUILayout.IntField(definition.ParameterNames[index], currentValue);
                if (value != currentValue)
                {
                    action.SetParameter(index, value);
                }
            }
        }
    }
}
