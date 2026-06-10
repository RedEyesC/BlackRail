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

        public override void OnInspectorGUI()
        {
            ShowCommonInspector();
        }

        protected void ShowCommonInspector(bool showBaseInspector = true)
        {
            ShowErrors();
            ShowInOutControls();
            ShowBlendingControls();
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
            var previousClip = action.GetPreviousSibling();
            var previousTime = previousClip != null ? previousClip.EndTime : action.Parent.StartTime;
            if (action.CanCrossBlend(previousClip))
            {
                previousTime -= Mathf.Min(action.Length / 2, (previousClip.EndTime - previousClip.StartTime) / 2);
            }

            var nextClip = action.GetNextSibling();
            var nextTime = nextClip != null ? nextClip.StartTime : action.Parent.EndTime;
            if (action.CanCrossBlend(nextClip))
            {
                nextTime += Mathf.Min(action.Length / 2, (nextClip.EndTime - nextClip.StartTime) / 2);
            }

            var canScale = action.CanScale();
            var doFrames = Prefs.timeStepMode == Prefs.TimeStepMode.Frames;

            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();

            var _in = action.StartTime;
            var _length = action.Length;
            var _out = action.EndTime;

            if (canScale)
            {
                GUILayout.Label("IN", GUILayout.Width(30));
                if (doFrames)
                {
                    _in *= Prefs.frameRate;
                    _in = EditorGUILayout.DelayedIntField((int)_in, GUILayout.Width(80));
                    _in *= (1f / Prefs.frameRate);
                }
                else
                {
                    _in = EditorGUILayout.DelayedFloatField(_in, GUILayout.Width(80));
                }

                GUILayout.FlexibleSpace();
                GUILayout.Label("◄");
                if (doFrames)
                {
                    _length *= Prefs.frameRate;
                    _length = EditorGUILayout.DelayedIntField((int)_length, GUILayout.Width(80));
                    _length *= (1f / Prefs.frameRate);
                }
                else
                {
                    _length = EditorGUILayout.DelayedFloatField(_length, GUILayout.Width(80));
                }

                GUILayout.Label("►");
                GUILayout.FlexibleSpace();

                GUILayout.Label("OUT", GUILayout.Width(30));
                if (doFrames)
                {
                    _out *= Prefs.frameRate;
                    _out = EditorGUILayout.DelayedIntField((int)_out, GUILayout.Width(80));
                    _out *= (1f / Prefs.frameRate);
                }
                else
                {
                    _out = EditorGUILayout.DelayedFloatField(_out, GUILayout.Width(80));
                }
            }

            GUILayout.EndHorizontal();

            if (canScale)
            {
                if (_in >= action.Parent.StartTime && _out <= action.Parent.EndTime)
                {
                    if (_out > _in)
                    {
                        EditorGUILayout.MinMaxSlider(ref _in, ref _out, previousTime, nextTime);
                    }
                    else
                    {
                        _in = TimeSlider(_in, previousTime, nextTime, doFrames);
                        _out = _in;
                    }
                }
            }
            else
            {
                GUILayout.Label("IN", GUILayout.Width(30));
                _in = TimeSlider(_in, 0, action.Parent.EndTime, doFrames);
                _out = _in;
            }

            if (GUI.changed)
            {
                if (_length != action.Length)
                {
                    _out = _in + _length;
                }

                _in = Mathf.Round(_in / Prefs.snapInterval) * Prefs.snapInterval;
                _out = Mathf.Round(_out / Prefs.snapInterval) * Prefs.snapInterval;

                _in = Mathf.Clamp(_in, previousTime, _out);
                _out = Mathf.Clamp(_out, _in, nextClip != null ? nextTime : float.PositiveInfinity);

                action.StartTime = _in;
                action.EndTime = _out;
            }

            if (_in > action.Parent.EndTime)
            {
                EditorGUILayout.HelpBox(Lan.OverflowInvalid, MessageType.Warning);
            }
            else
            {
                if (_out > action.Parent.EndTime)
                {
                    EditorGUILayout.HelpBox(Lan.EndTimeOverflowInvalid, MessageType.Warning);
                }
            }

            if (_out < action.Parent.StartTime)
            {
                EditorGUILayout.HelpBox(Lan.OverflowInvalid, MessageType.Warning);
            }
            else
            {
                if (_in < action.Parent.StartTime)
                {
                    EditorGUILayout.HelpBox(Lan.StartTimeOverflowInvalid, MessageType.Warning);
                }
            }

            GUILayout.EndVertical();
        }

        static float TimeSlider(float value, float leftValue, float rightValue, bool doFrames)
        {
            if (!doFrames)
            {
                return EditorGUILayout.Slider(value, leftValue, rightValue);
            }

            var frameRate = Mathf.Max(1, Prefs.frameRate);
            var frame = Mathf.RoundToInt(value * frameRate);
            var leftFrame = Mathf.RoundToInt(leftValue * frameRate);
            var rightFrame = Mathf.RoundToInt(rightValue * frameRate);

            frame = EditorGUILayout.IntSlider(frame, leftFrame, rightFrame);
            return frame * (1f / frameRate);
        }

        /// <summary>
        /// 显示混合输入/输出控件
        /// </summary>
        void ShowBlendingControls()
        {
            var canBlendIn = action.CanBlendIn();
            var canBlendOut = action.CanBlendOut();
            if ((canBlendIn || canBlendOut) && action.Length > 0)
            {
                GUILayout.BeginVertical("box");
                GUILayout.BeginHorizontal();
                if (canBlendIn)
                {
                    GUILayout.BeginVertical();
                    GUILayout.Label("Blend In");
                    var max = action.Length - action.BlendOut;
                    action.BlendIn = EditorGUILayout.Slider(action.BlendIn, 0, max);
                    action.BlendIn = Mathf.Clamp(action.BlendIn, 0, max);
                    GUILayout.EndVertical();
                }

                if (canBlendOut)
                {
                    GUILayout.BeginVertical();
                    GUILayout.Label("Blend Out");
                    var max = action.Length - action.BlendIn;
                    action.BlendOut = EditorGUILayout.Slider(action.BlendOut, 0, max);
                    action.BlendOut = Mathf.Clamp(action.BlendOut, 0, max);
                    GUILayout.EndVertical();
                }

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
        }
    }
}
