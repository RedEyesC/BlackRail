using System.Collections.Generic;
using UnityEngine;

namespace GameEditor.RecastEditor
{
    public class RcHeightfield
    {
        //可行走角度
        public float walkableSlopeAngle = 0;
        //可攀爬高度
        public int walkableClimb = 0;
        //物体高度
        public int walkableHeight = 0;
        //物体半径
        public int walkableRadius = 0;

        public float cellSize = 0;
        public float cellHeight = 0;

        public int width = 0;
        public int height = 0;

        public Vector3 minBounds = new Vector3();
        public Vector3 maxBounds = new Vector3();

        public RcSpan[] spans;

        public RcHeightfield(Mesh mesh, float agentMaxSlope, float agentMaxClimb, float agentHeight, float agentRadius, float cellSize, float cellHeight)
        {

            this.cellSize = cellSize;
            this.cellHeight = cellHeight;
            walkableSlopeAngle = agentMaxSlope;

            RecastUtility.CalcBounds(mesh.vertices, out minBounds, out maxBounds);

            RecastUtility.CalcGridSize(minBounds, maxBounds, cellSize, out width, out height);

            spans = new RcSpan[height * width];

            walkableClimb = (int)(agentMaxClimb / cellHeight);
            walkableHeight = (int)(agentHeight / cellHeight);
            walkableRadius = (int)(agentRadius / cellSize);
        }
    }


    public class RcSpan
    {
        public int min;
        public int max;
        public AREATYPE areaID;
        public RcSpan next = null;

        public RcSpan(int min, int max, AREATYPE areaID)
        {
            this.min = min;
            this.max = max;
            this.areaID = areaID;
        }
    }

    public class RcCompactHeightfield
    {
        //可行走角度
        public float walkableSlopeAngle = 0;
        //可攀爬高度
        public int walkableClimb = 0;
        //物体高度
        public int walkableHeight = 0;
        //物体半径
        public int walkableRadius = 0;

        public float cellSize = 0;
        public float cellHeight = 0;

        public int width = 0;
        public int height = 0;

        public int spanCount = 0;

        public CompactCell[] cells;
        public CompactSpan[] spans;
        public AREATYPE[] areas;

        public Vector3 minBounds = new Vector3();
        public Vector3 maxBounds = new Vector3();

        //距离场
        public int[] distanceToBoundary;

        //最大距离
        public int maxDistance = 0;

        //最大区域id
        public int maxRegions = 0;
        public RcCompactHeightfield(RcHeightfield hf)
        {

            width = hf.width;
            height = hf.height;

            cellHeight = hf.cellHeight;
            cellSize = hf.cellSize;

            walkableSlopeAngle = hf.walkableSlopeAngle;
            walkableClimb = hf.walkableClimb;
            walkableHeight = hf.walkableHeight;
            walkableRadius = hf.walkableRadius;

            cells = new CompactCell[width * height];

            spanCount = RecastUtility.RcGetHeightFieldSpanCount(hf);

            spans = new CompactSpan[spanCount];
            areas = new AREATYPE[spanCount];
            distanceToBoundary = new int[spanCount];

            minBounds = hf.minBounds;
            maxBounds = hf.maxBounds;
        }
    }

    public class CompactCell
    {
        public int index = 0; //对应spanList开始的序号
        public int count = 0; //当前xz平面体素点存在span数量

        public CompactCell(int index, int count)
        {
            this.index = index;
            this.count = count;
        }
    }

    public class CompactSpan
    {
        public int y;
        public int h;
        public int reg;
        public int con; //记录相邻的层级是否可行走,每个方向用六位来存放可行走层级，最高63层（6位都为1为不可行走,所以64-1=63）
        public AREATYPE areaID;

        public CompactSpan(int y, int h, AREATYPE areaID)
        {
            this.y = y;
            this.h = h;
            this.areaID = areaID;
        }
    }

    public class LevelStackEntry
    {
        public int x;
        public int y;
        public int index;

        public LevelStackEntry(int x, int y, int index)
        {
            this.x = x;
            this.y = y;
            this.index = index;
        }
    }


    public class DirtyEntry
    {
        public int index;
        public int region;
        public int distance2;

        public DirtyEntry(int index, int region, int distance2)
        {
            this.index = index;
            this.region = region;
            this.distance2 = distance2;
        }
    }


    public class RcRegion
    {
        public int spanCount = 0;
        public int id;
        public AREATYPE areaType = 0;
        public bool remap = false;
        public bool visited = false;
        public bool overlap = false; //是否多层
        public int ymin = 0xffff;
        public int ymax = 0;
        public List<int> connections = new List<int>();
        public List<int> floors = new List<int>();

        public RcRegion(int index)
        {
            this.id = index;
        }
    }

    public class RcContourSet
    {
        public float cellSize = 0;
        public float cellHeight = 0;

        public int width = 0;
        public int height = 0;

        public int numConts = 0;

        public List<RcContour> conts = new List<RcContour>();

        public Vector3 minBounds = new Vector3();
        public Vector3 maxBounds = new Vector3();

        public RcContourSet(RcCompactHeightfield chf)
        {

            width = chf.width;
            height = chf.height;

            cellHeight = chf.cellHeight;
            cellSize = chf.cellSize;

            minBounds = chf.minBounds;
            maxBounds = chf.maxBounds;

        }

    }

    public class RcContour
    {
        public int[] verts;
        public int numVerts;
        public int reg;
        public AREATYPE area;
    };

    public class RcContourRegion
    {
        public RcContour outline;
        public RcContourHole[] holes;
        public int numHoles;
    };

    public class RcContourHole
    {
        public RcContour contour;
        public int minX;
        public int minZ;
        public int leftMost;
    };

    public class RcPotentialDiagonal
    {
        public int vert;
        public float dist;

        public RcPotentialDiagonal(int vert, float dist)
        {
            this.vert = vert;
            this.dist = dist;
        }
    };

    public class RcPolyMesh
    {
        public float cellSize = 0;
        public float cellHeight = 0;

        public int numConts = 0;

        public int numVerts = 0;
        public int numPolys = 0;
        public int maxPolys = 0;

        public int[] verts;
        //每个多边形占据 RecastConfig.MaxVertsPerPoly * 2，
        //前RecastConfig.MaxVertsPerPoly存放多边形顶点，后RecastConfig.MaxVertsPerPoly这个点存放邻接多边形信息
        public int[] polys;
        public int[] regs;
        public int[] flags;
        public AREATYPE[] areas;

        public Vector3 minBounds = new Vector3();
        public Vector3 maxBounds = new Vector3();

        public RcPolyMesh(RcContourSet cset)
        {

            cellHeight = cset.cellHeight;
            cellSize = cset.cellSize;

            minBounds = cset.minBounds;
            maxBounds = cset.maxBounds;

        }

    }

    public class RcEdge
    {
        public int[] vert = new int[2];  //边的两个点
        public int[] polyEdge = new int[2]; //邻接的两个多边形的边的索引
        public int[] poly = new int[2]; //邻接的两个多边形的索引

        public RcEdge(int vert0, int vert1, int poly0, int poly1, int polyEdge0, int polyEdge1)
        {

            vert[0] = vert0;
            vert[1] = vert1;
            poly[0] = poly0;
            poly[1] = poly1;
            polyEdge[0] = polyEdge0;
            polyEdge[1] = polyEdge1;

        }

    }

    public class RcPolyMeshDetail
    {

        public int numMeshes = 0;
        public int numVerts = 0;
        public int numTris = 0;

        public int[] meshes;
        public float[] verts;
        public int[] tris;

    }

    public class RcHeightPatch
    {
        public int xmin = 0;
        public int ymin = 0;
        public int width = 0;
        public int height = 0;

        public int[] data;
    }



}
