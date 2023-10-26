
namespace GameEditor.RecastEditor
{
    internal class RecastConfig
    {
        public static readonly float PI = 3.1415926f;

        public static readonly string MapResPath = "Assets/Resources/map/";
        public static readonly string MapElement = "MapElement";

        //构建寻路网格的参数
        public static readonly int MAX_HEIGHT = 25;
        public static readonly int RC_NOT_CONNECTED = 0x3f;//空心高度场，相邻不可行走标志，RecastConfig.RC_NOT_CONNECTED-1 为最大层级
        public static readonly int MAX_LAYERS = RC_NOT_CONNECTED - 1;


        public static readonly int RC_BORDER_VERTEX = 0x10000;
        public static readonly int RC_AREA_BORDER = 0x20000;
        public static readonly int RC_CONTOUR_REG_MASK = 0xffff;

        public static readonly float AgentMaxSlope = 45;
        public static readonly float AgentMaxClimb = 3f;
        public static readonly float AgentHeight = 2.0f;
        public static readonly float AgentRadius = 1f;
        public static readonly float CellSize = 1f; //xz平面的尺寸
        public static readonly float CellHeight = 1f; // y轴的尺寸

        public static readonly float MinRegionArea = 10; //小于此则被剔除
        public static readonly float MergeRegionArea = 20; //小于此则与周围区域合并

        public static readonly float MaxSimplificationError = 1; //最大简化误差 ，大于则视为简化点
        public static readonly bool TessellateWallEdges = true;//是否根据最大边缘长度细化不可行走的边缘
        public static readonly bool TessellateAreaEdges = true;//是否根据最大边缘长度是否细化区域之间的边缘
        public static readonly float MaxEdgeLen = 12; //最大边缘长度

        public static readonly bool FilterLowHangingObstacles = false;//过滤悬空可走的span
        public static readonly bool FilterLedgeSpans = false;//过滤高度差过大的span
        public static readonly bool FilterWalkableLowHeightSpans = false;//过滤不可通过的高度的span
    }
}
