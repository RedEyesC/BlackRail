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
            var sliderRect = Rect.MinMaxRect(view.CenterRect.xMin, TimelineBottom, view.ScreenWidth, view.ScreenHeight);
            var timeMin = ViewTimeMin;
            var timeMax = ViewTimeMax;

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

            var minChanged = !TimelineTime.AreEqual(timeMin, ViewTimeMin);
            var maxChanged = !TimelineTime.AreEqual(timeMax, ViewTimeMax);
            if (minChanged || maxChanged)
            {
                SetViewTimeRange(timeMin, timeMax, minChanged && !maxChanged);
            }

            if (currentEvent.type == EventType.Repaint)
            {
                var skin = GUI.skin;
                var left = minMaxScroller == null ? 0 : skin.horizontalScrollbarLeftButton.fixedWidth;
                var right = minMaxScroller == null ? 0 : skin.horizontalScrollbarRightButton.fixedWidth;
                var fraction = player.CurrentTime / Mathf.Max(asset.Length, ViewTimeMax);
                var x = Mathf.Lerp(sliderRect.xMin + left, sliderRect.xMax - right - 1, fraction);
                EditorGUI.DrawRect(new Rect(x, sliderRect.y + 2, 1, sliderRect.height - 4), player.GetScriberColor());
            }
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
            var thumbStyle = skin.FindStyle("horizontalMinMaxScrollbarThumb") ?? skin.horizontalScrollbarThumb;

            minMaxScroller(
                sliderRect,
                controlId,
                ref timeMin,
                ref viewTime,
                0,
                Mathf.Max(asset.Length, timeMax),
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

            timeMin = preserveTimeMax
                ? Mathf.Clamp(timeMin, 0, Mathf.Max(0, timeMax - minViewTime))
                : Mathf.Max(0, timeMin);

            viewTimeRange = new Vector2(timeMin, Mathf.Max(timeMax, timeMin + minViewTime));
            if (assetSource != null)
            {
                viewTimeByAsset[assetSource] = viewTimeRange;
            }
            WillRepaint = true;
        }
    }
}
