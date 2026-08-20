using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TrackEditor
{
    public partial class TrackEditorWindow
    {
        private delegate void MinMaxScrollerDelegate(
            Rect position,
            int id,
            ref float value,
            ref float size,
            float visualStart,
            float visualEnd,
            float startLimit,
            float endLimit,
            GUIStyle slider,
            GUIStyle thumb,
            GUIStyle leftButton,
            GUIStyle rightButton,
            bool horizontal
        );

        private static readonly int timeSliderHash = "TrackEditorTimeSlider".GetHashCode();
        private static readonly MinMaxScrollerDelegate minMaxScroller = CreateMinMaxScroller();

        public void DrawBottomGUI()
        {
            var sliderRect = new Rect(G.CenterRect.x, G.ScreenHeight - G.BottomHeight, G.CenterRect.width, Styles.BOTTOM_HEIGHT);
            ShowTimeSlider(sliderRect);
        }

        /// <summary>
        /// 显示底部滚动信息
        /// </summary>
        void ShowTimeSlider(Rect rect)
        {
            GUILayout.BeginArea(rect);
            var sliderRect = new Rect(2, 0, rect.width - 4, 18);
            var timeMin = asset.ViewTimeMin;
            var timeMax = asset.ViewTimeMax;

            var currentEvent = Event.current;
            if (
                currentEvent.type == EventType.MouseDown
                && currentEvent.button == 0
                && currentEvent.clickCount == 2
                && sliderRect.Contains(currentEvent.mousePosition)
            )
            {
                timeMin = 0;
                timeMax = asset.Length;
                currentEvent.Use();
            }
            else
            {
                DrawTimeRangeScroller(sliderRect, ref timeMin, ref timeMax);
            }

            if (!Mathf.Approximately(timeMin, asset.ViewTimeMin) || !Mathf.Approximately(timeMax, asset.ViewTimeMax))
            {
                var isDraggingTimeMin = !Mathf.Approximately(timeMin, asset.ViewTimeMin) && Mathf.Approximately(timeMax, asset.ViewTimeMax);
                SetViewTimeRange(timeMin, timeMax, isDraggingTimeMin);
            }

            GUI.color = Color.white.WithAlpha(0.1f);
            GUI.DrawTexture(Rect.MinMaxRect(0, Styles.TOP_MARGIN - 1, G.TopMiddleRect.xMax, Styles.TOP_MARGIN), Styles.whiteTexture);
            GUI.color = Color.white;

            GUILayout.EndArea();

            GUI.contentColor = Color.white;
        }

        private void DrawTimeRangeScroller(Rect sliderRect, ref float timeMin, ref float timeMax)
        {
            if (minMaxScroller == null)
            {
                EditorGUI.MinMaxSlider(sliderRect, ref timeMin, ref timeMax, 0, Mathf.Max(asset.Length, timeMax));
                return;
            }

            var viewTime = timeMax - timeMin;
            var controlId = GUIUtility.GetControlID(timeSliderHash, FocusType.Passive, sliderRect);
            var skin = GUI.skin;
            var thumbStyle = skin.FindStyle("MinMaxHorizontalSliderThumb") ?? skin.horizontalScrollbarThumb;

            minMaxScroller(
                sliderRect,
                controlId,
                ref timeMin,
                ref viewTime,
                0,
                asset.Length,
                0,
                float.PositiveInfinity,
                skin.horizontalScrollbar,
                thumbStyle,
                skin.horizontalScrollbarLeftButton,
                skin.horizontalScrollbarRightButton,
                true
            );

            timeMax = timeMin + viewTime;
        }

        private static MinMaxScrollerDelegate CreateMinMaxScroller()
        {
            try
            {
                var editorGuiExt = typeof(EditorGUI).Assembly.GetType("UnityEditor.EditorGUIExt");
                var method = editorGuiExt?.GetMethod("MinMaxScroller", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                return method == null ? null : (MinMaxScrollerDelegate)Delegate.CreateDelegate(typeof(MinMaxScrollerDelegate), method);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void SetViewTimeRange(float timeMin, float timeMax, bool preserveTimeMax = false)
        {
            const float minViewTime = 0.25f;

            timeMin = Mathf.Max(0, timeMin);
            timeMax = Mathf.Max(0, timeMax);

            if (timeMax - timeMin < minViewTime)
            {
                if (preserveTimeMax)
                {
                    timeMin = Mathf.Max(0, timeMax - minViewTime);
                }
                else
                {
                    timeMax = timeMin + minViewTime;
                }
            }

            if (timeMin > asset.ViewTimeMin)
            {
                asset.ViewTimeMax = timeMax;
                asset.ViewTimeMin = timeMin;
                return;
            }

            asset.ViewTimeMin = timeMin;
            asset.ViewTimeMax = timeMax;
        }
    }
}
