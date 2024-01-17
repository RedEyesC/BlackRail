
using System.Collections.Generic;

namespace GameEditor.DetourEditor
{

    internal struct QueryParams {
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
        public int polysCount;

        public int[] detailMeshes;
        public float[] detailVerts;
        public int[] detailTris;
        public int detailVertsCount;

        public float cs;
        public float ch;

        public DtBVNode[] treeNodes;
        public int nodesNum;

        public int walkableClimb;
        public float quantFactor;
        public int nullIdx;
        public int nvp;
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
