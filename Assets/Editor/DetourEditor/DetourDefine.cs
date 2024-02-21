
using GameEditor.RecastEditor;
using System;
using System.Collections.Generic;

namespace GameEditor.DetourEditor
{

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
        public int firstLink;

        public int[] verts;

        public int[] neis;

        public int flags;

        public int vertCount;

        public int areaAndtype;

        public void SetArea(int a) { areaAndtype = (areaAndtype & 0xc0) | (a & 0x3f); }

        public void SetPType(DtPolyTypes t) { areaAndtype = (areaAndtype & 0x3f) | ((int)t << 6); }

        public int GetArea() { return areaAndtype & 0x3f; }

        public int GetPType() { return areaAndtype >> 6; }
    }

    struct DtLink
    {
        public int refId;
        public int next;
        public int edge;
        public int side;
        public int bmin;
        public int bmax;
    };

    internal class QueryParams
    {
        public float[] nearestPoint = new float[3];
        public int nearestRef = -1;
        public float nearestDistanceSqr = float.MaxValue;
        public bool overPoly;
    }

    internal class DtBVNode
    {
        public int[] bmin = new int[3];
        public int[] bmax = new int[3];
        public int i;

    }

    internal class DtNavData
    {
        public int[] verts;
        public int nverts;

        public int[] polys;
        public int polyCount;

        public float[] bmax;
        public float[] bmin;

        public int[] detailMeshes;
        public float[] detailVerts;
        public int[] detailTris;
        public int detailVertsCount;

        public float cs;
        public float ch;

        public DtBVNode[] treeNodes;
        public int nodesNum;

        public DtPoly[] navPolys;

        public DtLink[] links;
        public int maxLinkCount;
        public int linksFreeList;

        public int walkableClimb;
        public float quantFactor;

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
