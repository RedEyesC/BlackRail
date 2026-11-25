using UnityEditor;
using UnityEngine;

namespace TrackEditor
{
    internal sealed class G
    {
        public string SearchString;

        public float TickInterval;
        public int FirstTick;
        public int LastTick;

        public static float SnapTime(float time)
        {
            if (Event.current.control)
            {
                return time;
            }

            return Prefs.SnapTime(time);
        }

        #region Size

        public float ContentHeight { get; private set; }

        public float ScreenWidth;
        public float ScreenHeight;
        public float LeftMargin;

        public Vector2 ScrollPos;

        public Rect TopLeftRect;

        public Rect LeftRect;

        public Rect TopMiddleRect;

        public Rect CenterRect;

        public void Reset(Rect windowRect, float contentHeight)
        {
            ScreenWidth = windowRect.width;
            ScreenHeight = windowRect.height;
            ContentHeight = contentHeight;
            TopLeftRect = new Rect(0, Styles.TOOLBAR_HEIGHT, LeftMargin, Styles.TOP_MARGIN);

            var viewportHeight = ScreenHeight - Styles.TOOLBAR_HEIGHT - Styles.TOP_MARGIN - Styles.BOTTOM_HEIGHT;
            var maxScroll = Mathf.Max(0, ContentHeight - viewportHeight);
            ScrollPos.y = Mathf.Clamp(ScrollPos.y, 0, maxScroll);
            var scrollbarWidth = maxScroll > 0 ? Styles.VERTICAL_SCROLLBAR_WIDTH : 0;
            var centerWidth = ScreenWidth - LeftMargin - scrollbarWidth;

            TopMiddleRect = new Rect(LeftMargin, Styles.TOOLBAR_HEIGHT, centerWidth, Styles.TOP_MARGIN);

            LeftRect = new Rect(0, Styles.TOOLBAR_HEIGHT + Styles.TOP_MARGIN, LeftMargin, viewportHeight + ScrollPos.y);

            CenterRect = new Rect(LeftMargin, Styles.TOP_MARGIN + Styles.TOOLBAR_HEIGHT, centerWidth,
                viewportHeight + ScrollPos.y);
        }

        #endregion
        
        
        internal bool IsFilteredOutBySearch(DirectableAsset direct)
        {
            if (string.IsNullOrEmpty(SearchString))
            {
                return false;
            }

            if (string.IsNullOrEmpty(direct.name))
            {
                return true;
            }

            return direct.name.IndexOf(SearchString, System.StringComparison.OrdinalIgnoreCase) < 0;
        }
    }
}
