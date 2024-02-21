
namespace GameEditor.RecastEditor
{
    internal class RecastConfig
    {
        public static readonly float PI = 3.1415926f;

        public static readonly string MapResPath = "Assets/Resources/Map/";
        public static readonly string MapElement = "MapElement";

        //构建寻路网格的参数
        public static readonly int MAX_HEIGHT = 500;
        public static readonly int RC_NOT_CONNECTED = 0x3f;//空心高度场，相邻不可行走标志，
        public static readonly int MAX_LAYERS = RC_NOT_CONNECTED - 1;//RecastConfig.RC_NOT_CONNECTED-1 为最大层级

        public static readonly int EV_UNDEF = -1;//未定义的边缘
        public static readonly int EV_HULL = -2;
        public static readonly int RC_AREA_BORDER = 0x20000;
        public static readonly int RC_CONTOUR_REG_MASK = 0xffff;

        public static readonly int RC_INDICE_MASK = 0x3fffffff;
        public static readonly int RC_INDICE = 0x40000000;

        public static readonly int RC_MESH_NULL_IDX = 0xffff; //空节点标志位
        public static readonly int VERTEX_BUCKET_COUNT = (1 << 12);

        public static readonly int RC_MULTIPLE_REGS = 0;

        public static readonly int RC_UNSET_HEIGHT = 0xffff; //未设置高度

        public static readonly int Detail_MAX_VERTS = 127;    //构建多边形高度细节时的单个多边形最大顶点数
        public static readonly int Detail_MAX_TRIS = 255;    // Max tris for delaunay is 2n-2-k (n=num verts, k=num hull verts).
        public static readonly int Detail_MAX_VERTS_PER_EDGE = 32; //构建多边形高度细节时每个边缘的的最大顶点数


        public static readonly float AgentMaxSlope = 60;
        public static readonly float AgentMaxClimb = 2f;
        public static readonly float AgentHeight = 2.0f;
        public static readonly float AgentRadius = 0.6f;
        public static readonly float CellSize = 0.3f; //xz平面的尺寸
        public static readonly float CellHeight = 0.2f; // y轴的尺寸

        public static readonly bool FilterLowHangingObstacles = false;//过滤悬空可走的span
        public static readonly bool FilterLedgeSpans = true;//过滤高度差过大的span
        public static readonly bool FilterWalkableLowHeightSpans = false;//过滤不可通过的高度的span

        public static readonly int MinRegionSize = 8; //小于此则被剔除 area = size*size
        public static readonly int MergeRegionSize = 20; //小于此则与周围区域合并

        public static readonly float MaxSimplificationError = 1.3f; //最大简化误差 ，大于则视为简化点
        public static readonly float MaxEdgeLen = 12; //最大边缘长度
        public static readonly bool TessellateWallEdges = false;//是否根据最大边缘长度细化不可行走的边缘
        public static readonly bool TessellateAreaEdges = false;//是否根据最大边缘长度是否细化区域之间的边缘


        public static readonly int MaxVertsPerPoly = 6; //单个多边形最大顶点数

        public static readonly float DetailSampleDist = 6f; //构建多边形高度细节时最大边缘长度
        public static readonly float DetailSampleMaxError = 1f; //构建多边形高度细节时最大误差

    }
}
