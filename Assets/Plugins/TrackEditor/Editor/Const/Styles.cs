using UnityEditor;
using UnityEngine;

namespace TrackEditor
{
    [InitializeOnLoad]
    public static class Styles
    {
        public static Texture2D Logo;
        public static Texture2D stripes;
        public static Texture2D magnetIcon;
        public static Texture2D lockIcon;
        public static Texture2D hiddenIcon;

        public static Texture2D playIcon;
        public static Texture2D playForwardIcon;
        public static Texture2D playBackwardIcon;
        public static Texture2D stepForwardIcon;
        public static Texture2D stepBackwardIcon;
        public static Texture2D pauseIcon;
        public static Texture2D playLoopIcon;

        public static Texture2D carretIcon;
        public static Texture2D cutsceneIconOpen;
        public static Texture2D backIcon;
        public static Texture2D saveIcon;
        public static Texture2D settingsIcon;
        public static Texture2D plusIcon;
        public static Texture2D menuIcon;

        private static GUISkin styleSheet;

        public static Color TrackHeaderSelection => EditorGUIUtility.isProSkin
            ? new Color32(55, 99, 151, 255)
            : new Color32(88, 139, 197, 255);

        public static Color TrackLaneSelection => EditorGUIUtility.isProSkin
            ? new Color(0.33f, 0.56f, 0.83f, 0.26f)
            : new Color(0.26f, 0.48f, 0.76f, 0.22f);

        public static Color DurationMarker => EditorGUIUtility.isProSkin
            ? new Color32(106, 148, 190, 255)
            : new Color32(53, 95, 149, 255);
        public static readonly Color GROUP_COLOR = new Color(0f, 0f, 0f, 0.25f);

        public const float VERTICAL_SCROLLBAR_WIDTH = 16;

        /// <summary>
        /// 工具栏的高度
        /// </summary>
        public const float TOOLBAR_HEIGHT = 20;

        /// <summary>
        /// 上边距
        /// </summary>
        public const float TOP_MARGIN = 20;

        /// <summary>
        /// 组高
        /// </summary>
        public const float GROUP_HEIGHT = 31;

        /// <summary>
        /// 组右边距
        /// </summary>
        public const float GROUP_RIGHT_MARGIN = 4;

        /// <summary>
        /// 第一个组上边距
        /// </summary>
        public const float FIRST_GROUP_TOP_MARGIN = 22;

        /// <summary>
        /// 轨道上下边距
        /// </summary>
        public const float TRACK_MARGINS = 4;

        /// <summary>
        /// 轨道右边距
        /// </summary>
        public const float TRACK_RIGHT_MARGIN = 4;

        /// <summary>
        /// 底部滚动条高度
        /// </summary>
        public const float BOTTOM_HEIGHT = 16;

        internal const float MIN_LEFT_MARGIN = 230f;
        private const float MAX_LEFT_MARGIN = 400f;

        public static float LEFT_MARGIN
        {
            get => ClampLeftMargin(Prefs.trackListLeftMargin);
            set => Prefs.trackListLeftMargin = ClampLeftMargin(value);
        }

        internal static float ClampLeftMargin(float width) => Mathf.Clamp(width, MIN_LEFT_MARGIN, MAX_LEFT_MARGIN);

        public static void Load()
        {
            stripes = (Texture2D)Resources.Load("Stripes");
            magnetIcon = (Texture2D)Resources.Load("magnet");
            lockIcon = (Texture2D)Resources.Load("LockIcon");
            hiddenIcon = (Texture2D)Resources.Load("HiddenIcon");
            playIcon = (Texture2D)Resources.Load("play");
            playForwardIcon = (Texture2D)Resources.Load("playForward");
            playBackwardIcon = (Texture2D)Resources.Load("playBackward");
            playLoopIcon = (Texture2D)Resources.Load("loopPlay");
            stepForwardIcon = (Texture2D)Resources.Load("stepForward");
            stepBackwardIcon = (Texture2D)Resources.Load("stepBackward");
            pauseIcon = (Texture2D)Resources.Load("pause");
            carretIcon = (Texture2D)Resources.Load("CarretIcon");
            cutsceneIconOpen = (Texture2D)Resources.Load("CutsceneIconOpen");
            settingsIcon = (Texture2D)Resources.Load("settings");
            backIcon = (Texture2D)Resources.Load("back");
            saveIcon = (Texture2D)Resources.Load("save");
            plusIcon = (Texture2D)Resources.Load("plus");
            menuIcon = (Texture2D)Resources.Load("menu");

            Logo = (Texture2D)Resources.Load("Logo");

            styleSheet = (GUISkin)Resources.Load("StyleSheet");
        }

        public static Texture2D whiteTexture => EditorGUIUtility.whiteTexture;

        private static GUIStyle _shadowBorderStyle;

        public static GUIStyle shadowBorderStyle =>
            _shadowBorderStyle != null ? _shadowBorderStyle : _shadowBorderStyle = styleSheet.GetStyle("ShadowBorder");

        private static GUIStyle _clipBoxStyle;

        public static GUIStyle clipBoxStyle => _clipBoxStyle != null ? _clipBoxStyle : _clipBoxStyle = styleSheet.GetStyle("ClipBox");

        private static GUIStyle _clipBoxFooterStyle;

        private static GUIStyle _clipBoxHorizontalStyle;

        public static GUIStyle clipBoxHorizontalStyle =>
            _clipBoxHorizontalStyle != null ? _clipBoxHorizontalStyle : _clipBoxHorizontalStyle = styleSheet.GetStyle("ClipBoxHorizontal");

        private static GUIStyle _timeBoxStyle;

        public static GUIStyle timeBoxStyle => _timeBoxStyle != null ? _timeBoxStyle : _timeBoxStyle = styleSheet.GetStyle("TimeBox");

        private static GUIStyle _headerBoxStyle;

        public static GUIStyle headerBoxStyle =>
            _headerBoxStyle != null ? _headerBoxStyle : _headerBoxStyle = styleSheet.GetStyle("HeaderBox");

        private static GUIStyle _hollowFrameStyle;

        public static GUIStyle hollowFrameStyle =>
            _hollowFrameStyle != null ? _hollowFrameStyle : _hollowFrameStyle = styleSheet.GetStyle("HollowFrame");

        private static GUIStyle _hollowFrameHorizontalStyle;

        public static GUIStyle hollowFrameHorizontalStyle =>
            _hollowFrameHorizontalStyle != null
                ? _hollowFrameHorizontalStyle
                : _hollowFrameHorizontalStyle = styleSheet.GetStyle("HollowFrameHorizontal");

        private static GUIStyle _centerLabel;

        public static GUIStyle centerLabel
        {
            get
            {
                if (_centerLabel != null)
                {
                    return _centerLabel;
                }

                _centerLabel = new GUIStyle("label");
                _centerLabel.alignment = TextAnchor.MiddleCenter;
                return _centerLabel;
            }
        }

        private static GUIStyle _lockButton;

        public static GUIStyle lockButton => _lockButton != null ? _lockButton : _lockButton = "IN LockButton";
    }
}
