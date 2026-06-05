using UnityEditor;
using UnityEngine;

namespace TrackEditor
{
    public partial class TrackEditorWindow
    {
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
            var sliderRect = new Rect(2, 0, G.TopMiddleRect.width - 4, 18);
            var timeMin = asset.ViewTimeMin;
            var timeMax = asset.ViewTimeMax;
            var maxTime = Mathf.Max(asset.Length, timeMax);

            EditorGUI.MinMaxSlider(sliderRect, ref timeMin, ref timeMax, 0, maxTime);

            if (!Mathf.Approximately(timeMin, asset.ViewTimeMin) || !Mathf.Approximately(timeMax, asset.ViewTimeMax))
            {
                SetViewTimeRange(timeMin, timeMax);
            }

            if (sliderRect.Contains(Event.current.mousePosition) && Event.current.clickCount == 2)
            {
                SetViewTimeRange(0, asset.Length);
            }

            GUI.color = Color.white.WithAlpha(0.1f);
            GUI.DrawTexture(Rect.MinMaxRect(0, Styles.TOP_MARGIN - 1, G.TopMiddleRect.xMax, Styles.TOP_MARGIN), Styles.whiteTexture);
            GUI.color = Color.white;

            GUILayout.EndArea();

            GUI.contentColor = Color.white;
        }

        private void SetViewTimeRange(float timeMin, float timeMax)
        {
            timeMin = Mathf.Max(0, timeMin);
            timeMax = Mathf.Max(timeMax, timeMin + 0.25f);

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
