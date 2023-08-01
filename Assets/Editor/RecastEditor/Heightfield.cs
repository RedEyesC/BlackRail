using UnityEngine;

namespace GameEditor
{
    public enum AREATYPE
    {
        Walke,
    }

    public class Heightfield
    {
        //可行走角度
        public float WalkableSlopeAngle = 0;
        //可攀爬高度
        public float WalkableClimb = 0;
        public float CellSize = 0;
        public float CellHeight = 0;

        public int Width = 0;
        public int Height = 0;

        public Vector3 minBounds = new Vector3();
        public Vector3 maxBounds = new Vector3();

        public Span[] SpanList;

        public Heightfield(Mesh mesh, float walkableSlopeAngle, float walkableClimb, float cellSize, float cellHeight)
        {
            WalkableClimb = walkableClimb;
            CellSize = cellSize;
            CellHeight = cellHeight;
            WalkableSlopeAngle = walkableSlopeAngle;
            WalkableClimb = walkableClimb;

            CommonUtility.CalcBounds(mesh.vertices, out minBounds, out maxBounds);

            CommonUtility.CalcGridSize(minBounds, maxBounds, CellSize, out Width, out Height);

            SpanList = new Span[Height * Width];
        }
    }


    public class Span
    {
        public int Min;
        public int Max;
        public AREATYPE AreaID;
        public Span Next;

        public Span(int min, int max, AREATYPE areaID)
        {
            Min = min;
            Max = max;
            AreaID = areaID;
        }
    }
}
