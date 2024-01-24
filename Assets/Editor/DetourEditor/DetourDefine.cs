
using GameEditor.RecastEditor;
using System;
using System.Collections.Generic;

namespace GameEditor.DetourEditor
{

    internal class DetourConfig
    {
        public static readonly int DT_NODE_PARENT_BITS = 24;
        public static readonly int DT_NODE_STATE_BITS = 2;
        public static readonly int DT_NULL_IDX = -1;
        public static readonly float H_SCALE = 0.999f;
        public static readonly int DT_VERTS_PER_POLYGON = 6;
        public static readonly int MESH_NULL_IDX = 0xffff; //空节点标志位
        public static readonly int DT_EXT_LINK = 0x8000;
    }

    internal enum DtNodeFlags
    {
        DT_NODE_OPEN = 0x01,
        DT_NODE_CLOSED = 0x02,
        DT_NODE_PARENT_DETACHED = 0x04
    };

    internal enum DtPolyTypes
    {
        DT_POLYTYPE_GROUND = 0,
        DT_POLYTYPE_OFFMESH_CONNECTION = 1
    };

    struct DtPoly
    {
        /// Index to first link in linked list. (Or #DT_NULL_LINK if there is no link.)
        public int firstLink;

        /// The indices of the polygon's vertices.
        /// The actual vertices are located in dtMeshTile::verts.
        public int[] verts;

        /// Packed data representing neighbor polygons references and flags for each edge.
        public int[] neis;

        /// The user defined polygon flags.
        public int flags;

        /// The number of vertices in the polygon.
        public int vertCount;

        /// The bit packed area id and polygon type.
        /// @note Use the structure's set and get methods to access this value.
        public int areaAndtype;

        /// Sets the user defined area id. [Limit: < #DT_MAX_AREAS]
        public void SetArea(int a) { areaAndtype = (areaAndtype & 0xc0) | (a & 0x3f); }

        /// Sets the polygon type. (See: #dtPolyTypes.)
        public void SetPType(DtPolyTypes t) { areaAndtype = (areaAndtype & 0x3f) | ((int)t << 6); }

        /// Gets the user defined area id.
        public int GetArea() { return areaAndtype & 0x3f; }

        /// Gets the polygon type. (See: #dtPolyTypes)
        public int GetPType() { return areaAndtype >> 6; }
    }

    internal struct QueryParams
    {
        public float[] nearestPoint;
        public int nearestRef;
        public float nearestDistanceSqr;
        public bool overPoly;
    }

    internal class DtBVNode
    {
        public int[] bmin;
        public int[] bmax;
        public int i;
    }

    internal class DtNavData
    {
        public int[] verts;
        public int nverts;

        public int[] polys;
        public int polyCount;

        public int[] detailMeshes;
        public float[] detailVerts;
        public int[] detailTris;
        public int detailVertsCount;

        public float cs;
        public float ch;

        public DtBVNode[] treeNodes;
        public int nodesNum;

        public DtPoly[] navPolys;

        public int walkableClimb;
        public float quantFactor;
        public int nullIdx;
        public int nvp;

        public AREATYPE[] polyAreas;
        public int[] polyFlags;
    }

    internal class SortWithX : IComparer<DtBVNode>
    {
        public int Compare(DtBVNode x, DtBVNode y)
        {
            if (x.bmin[0] == y.bmin[0])
            {
                return 0;
            }

            return x.bmin[0] - y.bmin[0] > 0 ? 1 : -1;
        }
    }

    internal class SortWithY : IComparer<DtBVNode>
    {
        public int Compare(DtBVNode x, DtBVNode y)
        {
            if (x.bmin[1] == y.bmin[1])
            {
                return 0;
            }

            return x.bmin[1] - y.bmin[1] > 0 ? 1 : -1;
        }
    }

    internal class SortWithZ : IComparer<DtBVNode>
    {
        public int Compare(DtBVNode x, DtBVNode y)
        {
            if (x.bmin[2] == y.bmin[2])
            {
                return 0;
            }

            return x.bmin[2] - y.bmin[2] > 0 ? 1 : -1;
        }
    }
}
