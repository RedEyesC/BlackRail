using UnityEditor;
using UnityEngine;

namespace TrackEditor
{
    public partial class TrackEditorWindow
    {
        private static readonly float[] TickIntervals = { 0.1f, 0.5f, 1, 5, 10, 50, 100, 500, 1000, 5000, 10000, 50000, 100000, 250000, 500000 };

        private static Color TickColor => EditorGUIUtility.isProSkin ? Color.white.WithAlpha(0.55f) : Color.black.WithAlpha(0.55f);

        public void DrawHeaderTimeInfoGUI()
        {
            if (asset == null)
                return;
            var topMiddleRect = view.TopMiddleRect;
            GUI.color = Color.white.WithAlpha(0.2f);
            GUI.Box(topMiddleRect, string.Empty, EditorStyles.toolbarButton);
            GUI.color = Color.black.WithAlpha(0.2f);
            GUI.Box(topMiddleRect, string.Empty, Styles.timeBoxStyle);
            GUI.color = Color.white;

            var doFrames = Prefs.timeStepMode == Prefs.TimeStepMode.Frames;
            var frameRate = Mathf.Max(1, Prefs.frameRate);
            var intervalIndex = 0;
            while (intervalIndex < TickIntervals.Length - 1 && TickIntervals[intervalIndex] / ViewTime * topMiddleRect.width <= 50)
            {
                intervalIndex++;
            }

            var interval = TickIntervals[intervalIndex];
            var timeStep = intervalIndex > 0 ? TickIntervals[intervalIndex - 1] : 0.01f;
            if (doFrames)
            {
                interval = Mathf.Max(1, Mathf.RoundToInt(interval * frameRate)) / (float)frameRate;
                timeStep = 1f / frameRate;
            }

            view.TickInterval = interval;
            view.FirstTick = Mathf.FloorToInt(ViewTimeMin / interval);
            view.LastTick = Mathf.CeilToInt(ViewTimeMax / interval);

            GUI.BeginGroup(topMiddleRect);
            {
                //步长间隔
                var tickColor = TickColor;
                if (view.CenterRect.width / (ViewTime / timeStep) > 6)
                {
                    var minorTickColor = tickColor.WithAlpha(0.3f);
                    var firstMinorTick = Mathf.CeilToInt(ViewTimeMin / timeStep);
                    var lastMinorTick = Mathf.FloorToInt(ViewTimeMax / timeStep);
                    for (var tick = firstMinorTick; tick <= lastMinorTick; tick++)
                    {
                        var x = TimeToPos(tick * timeStep);
                        EditorGUI.DrawRect(new Rect(x, Styles.TOP_MARGIN - 3, 1, 3), minorTickColor);
                    }
                }

                for (var tick = view.FirstTick; tick <= view.LastTick; tick++)
                {
                    var time = tick * interval;
                    var x = TimeToPos(time);
                    EditorGUI.DrawRect(new Rect(x, Styles.TOP_MARGIN - 8, 1, 8), tickColor);
                    var label = doFrames ? Mathf.RoundToInt(time * frameRate).ToString() : time.ToString("0.00");
                    GUI.Label(new Rect(x + 3, -2, 55, Styles.TOP_MARGIN), label, EditorStyles.miniLabel);
                }

                //长度位置箭头图片和预退出长度指示
                var ruler = new Rect(0, 0, topMiddleRect.width, topMiddleRect.height);
                var durationWidth = Mathf.Clamp(TimeToPos(asset.Length), 0, ruler.width);
                EditorGUI.DrawRect(new Rect(0, ruler.yMax - 2, durationWidth, 2), Styles.DurationMarker);
                if (IsTimeVisible(asset.Length))
                {
                    EditorGUI.DrawRect(DurationHandleRect(ruler), Styles.DurationMarker);
                }

                if (Event.current.type == EventType.Repaint && IsTimeVisible(player.CurrentTime))
                {
                    DrawRulerPlayhead(PlayheadRect(ruler));
                }
            }
            GUI.EndGroup();
        }

        private bool IsTimeVisible(float time) => time >= ViewTimeMin && time <= ViewTimeMax;

        private float TimeToGuideX(float time) => Mathf.Clamp(TimeToPos(time), 0, view.CenterRect.width - 1);

        private Rect PlayheadRect(Rect ruler) =>
            new Rect(ruler.x + TimeToGuideX(player.CurrentTime) - 3, ruler.yMax - 17, 7, 15);

        private Rect DurationHandleRect(Rect ruler) =>
            new Rect(ruler.x + TimeToGuideX(asset.Length) - 5, ruler.yMax - 12, 6, 12);

        private static void DrawRulerPlayhead(Rect rect)
        {
            var center = rect.center.x;
            var shoulder = rect.yMax - 4;
            using (new Handles.DrawingScope(Color.white))
            {
                Handles.DrawAAConvexPolygon(
                    new Vector3(rect.xMin, rect.yMin),
                    new Vector3(rect.xMax, rect.yMin),
                    new Vector3(rect.xMax, shoulder),
                    new Vector3(center, rect.yMax),
                    new Vector3(rect.xMin, shoulder)
                );
            }

            EditorGUI.DrawRect(new Rect(center - 0.5f, rect.yMax, 1, 2), Color.white);
        }
    }
}
