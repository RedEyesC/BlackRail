using UnityEditor;
using UnityEngine;

namespace TrackEditor
{
    public abstract class ClipInspector<T> : ClipInspector
        where T : Clip
    {
        protected T action => (T)target;
    }

    [CustomInspectors(typeof(Clip), true)]
    public class ClipInspector : InspectorsBase
    {
        private Clip action => (Clip)target;

        protected virtual bool ShowClipType => true;

        public override void OnInspectorGUI()
        {
            ShowCommonInspector();
        }

        protected void ShowCommonInspector(bool showBaseInspector = true)
        {
            if (ShowClipType)
            {
                EditorGUI.BeginChangeCheck();
                var clipType = (ClipType)EditorGUILayout.EnumPopup("触发方式", action.Type);
                if (EditorGUI.EndChangeCheck())
                {
                    action.Type = clipType;
                }
            }

            ShowErrors();
            ShowInOutControls();
            if (showBaseInspector)
            {
                base.OnInspectorGUI();
            }
        }

        void ShowErrors()
        {
            if (action.isValid)
                return;
            EditorGUILayout.HelpBox("该剪辑无效。 请确保设置了所需的参数。", MessageType.Error);
            GUILayout.Space(5);
        }

        void ShowInOutControls()
        {
            var previousClip = action.IsSignal ? null : action.GetPreviousSibling();
            var previousTime = previousClip != null ? previousClip.EndTime : action.Parent.StartTime;
            var nextClip = action.IsSignal ? null : action.GetNextSibling();
            var nextTime = nextClip != null ? nextClip.StartTime : action.Parent.EndTime;
            var canScale = action.CanScale();
            var playableStart = TimelineTime.RoundForComparison(action.Parent.StartTime);
            var playableEnd = TimelineTime.RoundForComparison(action.Parent.EndTime);

            EditorGUI.BeginChangeCheck();
            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();

            var _in = action.StartTime;
            var _length = action.Length;
            var _out = action.EndTime;

            if (canScale)
            {
                GUILayout.Label("IN", GUILayout.Width(30));
                _in = EditorTools.DelayedTimeField(GUIContent.none, _in, GUILayout.Width(80));

                GUILayout.FlexibleSpace();
                GUILayout.Label("◄");
                _length = EditorTools.DelayedTimeField(GUIContent.none, _length, GUILayout.Width(80));

                GUILayout.Label("►");
                GUILayout.FlexibleSpace();

                GUILayout.Label("OUT", GUILayout.Width(30));
                _out = EditorTools.DelayedTimeField(GUIContent.none, _out, GUILayout.Width(80));
            }

            GUILayout.EndHorizontal();

            if (canScale)
            {
                if (TimelineTime.RoundForComparison(_in) >= playableStart && TimelineTime.RoundForComparison(_out) <= playableEnd)
                {
                    if (TimelineTime.IsAfter(_out, _in))
                    {
                        EditorGUILayout.MinMaxSlider(ref _in, ref _out, previousTime, nextTime);
                    }
                    else
                    {
                        _in = SliderOnly(_in, previousTime, nextTime);
                        _out = _in;
                    }
                }
            }
            else
            {
                GUILayout.Label("IN", GUILayout.Width(30));
                _in = SliderOnly(_in, 0, action.Parent.EndTime);
                _out = _in;
            }

            if (EditorGUI.EndChangeCheck()
                && (_in != action.StartTime || _out != action.EndTime || _length != action.Length))
            {
                if (_length != action.Length)
                {
                    _out = _in + _length;
                }

                if (_in != action.StartTime) _in = Prefs.SnapTime(_in);
                if (_out != action.EndTime) _out = Prefs.SnapTime(_out);

                _in = Mathf.Clamp(_in, previousTime, _out);
                _out = Mathf.Clamp(_out, _in, nextClip != null ? nextTime : float.PositiveInfinity);

                action.StartTime = _in;
                action.EndTime = _out;
            }

            var clipStart = TimelineTime.RoundForComparison(action.StartTime);
            var clipEnd = TimelineTime.RoundForComparison(action.EndTime);
            if (clipStart > playableEnd)
            {
                EditorGUILayout.HelpBox(Lan.OverflowInvalid, MessageType.Warning);
            }
            else if (clipEnd > playableEnd)
            {
                EditorGUILayout.HelpBox(Lan.EndTimeOverflowInvalid, MessageType.Warning);
            }

            if (clipEnd < playableStart)
            {
                EditorGUILayout.HelpBox(Lan.OverflowInvalid, MessageType.Warning);
            }
            else if (clipStart < playableStart)
            {
                EditorGUILayout.HelpBox(Lan.StartTimeOverflowInvalid, MessageType.Warning);
            }

            GUILayout.EndVertical();
        }

        static float SliderOnly(float value, float min, float max, params GUILayoutOption[] options)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, options);

            rect = EditorGUI.IndentedRect(rect);
            rect.y += 2f;
            rect.height = 16f;

            return GUI.HorizontalSlider(rect, value, min, max);
        }
    }
}
