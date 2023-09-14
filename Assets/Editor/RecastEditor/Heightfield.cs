using UnityEngine;

namespace GameEditor
{
    public class Heightfield
    {
        //可行走角度
        public float WalkableSlopeAngle = 0;
        //可攀爬高度
        public int WalkableClimb = 0;
        //物体高度
        public int WalkableHeight = 0;
        //物体半径
        public int WalkableRadius = 0;

        public float CellSize = 0;
        public float CellHeight = 0;

        public int Width = 0;
        public int Height = 0;

        public Vector3 MinBounds = new Vector3();
        public Vector3 MaxBounds = new Vector3();

        public Span[] SpanList;

        public Heightfield(Mesh mesh, float agentMaxSlope, float agentMaxClimb, float agentHeight,float agentRadius, float cellSize, float cellHeight)
        {

            CellSize = cellSize;
            CellHeight = cellHeight;
            WalkableSlopeAngle = agentMaxSlope;

            RecastUtility.CalcBounds(mesh.vertices, out MinBounds, out MaxBounds);

            RecastUtility.CalcGridSize(MinBounds, MaxBounds, CellSize, out Width, out Height);

            SpanList = new Span[Height * Width];

            WalkableClimb = (int)(agentMaxClimb / cellHeight);
            WalkableHeight = (int)(agentHeight / cellHeight);
            WalkableRadius = (int)(agentRadius / cellSize);
        }
    }


    public class Span
    {
        public int Min;
        public int Max;
        public AREATYPE AreaID;
        public Span Next = null;

        public Span(int min, int max, AREATYPE areaID)
        {
            Min = min;
            Max = max;
            AreaID = areaID;
        }
    }

    public class CompactHeightfield
    {
        //可行走角度
        public float WalkableSlopeAngle = 0;
        //可攀爬高度
        public int WalkableClimb = 0;
        //物体高度
        public int WalkableHeight = 0;
        //物体半径
        public int WalkableRadius = 0;

        public float CellSize = 0;
        public float CellHeight = 0;

        public int Width = 0;
        public int Height = 0;

        public int SpanCount = 0;

        public CompactCell[] CellList;
        public CompactSpan[] SpanList;
        public AREATYPE[] AreaList;

        //距离场
        public int[] DistanceToBoundary;

        //最大距离
        public int MaxDistance = 0;

        public CompactHeightfield(Heightfield hf)
        {

            Width = hf.Width;
            Height = hf.Height;

            CellHeight = hf.CellHeight;
            CellSize = hf.CellSize;

            WalkableSlopeAngle = hf.WalkableSlopeAngle;
            WalkableClimb = hf.WalkableClimb;
            WalkableHeight = hf.WalkableHeight;
            WalkableRadius = hf.WalkableRadius;

            CellList = new CompactCell[Width * Height];

            SpanCount = RecastUtility.RcGetHeightFieldSpanCount(hf);

            SpanList = new CompactSpan[SpanCount];
            AreaList = new AREATYPE[SpanCount];
            DistanceToBoundary = new int[SpanCount];
        }
    }

    public class CompactCell
    {
        public int Index = 0; //对应spanList开始的序号
        public int Count = 0; //当前xz平面体素点存在span数量

        public CompactCell(int index, int count)
        {
            Index = index;
            Count = count;
        }
    }

    public class CompactSpan
    {
        public int Y;
        public int H;
        public int Con; //记录相邻的层级是否可行走,每个方向用六位来存放可行走层级，最高63层（6位都为1为不可行走,所以64-1=63）
        public AREATYPE AreaID;

        public CompactSpan(int y, int h, AREATYPE areaID)
        {
            Y = y;
            H = h;
            AreaID = areaID;
        }
    }

    public class LevelStackEntry
    {
        public int X;
        public int Y;
        public int Index; 

        public LevelStackEntry(int x, int y, int index)
        {
            X = x;
            Y = y;
            Index = index;
        }
    }


    public class DirtyEntry
    {
        public int Index;
        public int Region;
        public int Distance2;

        public DirtyEntry(int index, int region, int distance2)
        {
            Index = index;
            Region = region;
            Distance2 = distance2;
        }
    }
}
