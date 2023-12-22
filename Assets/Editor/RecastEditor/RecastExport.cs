
using System;
using System.Collections.Generic;

namespace GameEditor.RecastEditor
{

    public class DtBVNode
    {
        public int[] bmin;
        public int[] bmax;
        public int i;
    }


    internal class RecastExport
    {
        public static void ExportNavMeshDataToJson(RcPolyMesh pmesh, RcPolyMeshDetail dmesh)
        {


        }


        public static DtBVNode[] CreateBVTree(RcPolyMesh pmesh, RcPolyMeshDetail? dmesh)
        {

            float quantFactor = 1 / pmesh.cellSize;
            DtBVNode[] bnodes = new DtBVNode[pmesh.npolys];

            for (int i = 0; i < pmesh.npolys; i++)
            {
                bnodes[i] = new DtBVNode();
            }

            for (int i = 0; i < pmesh.npolys; i++)
            {
                DtBVNode it = bnodes[i];
                it.i = i;
                if (dmesh != null)
                {
                    int vb = dmesh.meshes[i * 4 + 0];
                    int ndv = dmesh.meshes[i * 4 + 1];
                    float[] bmin = new float[3];
                    float[] bmax = new float[3];


                    int dvIndex = vb * 3;
                    float[] dv0 = { dmesh.verts[dvIndex], dmesh.verts[dvIndex + 1], dmesh.verts[dvIndex + 2] };

                    RecastUtility.RcVcopy(bmin, dv0);
                    RecastUtility.RcVcopy(bmax, dv0);

                    for (int j = 1; j < ndv; j++)
                    {
                        float[] dv = { dmesh.verts[dvIndex + j * 3], dmesh.verts[dvIndex + j * 3 + 1], dmesh.verts[dvIndex + j * 3 + 2] };
                        RecastUtility.RcVmin(bmin, dv);
                        RecastUtility.RcVmax(bmax, dv);
                    }


                    it.bmin[0] = Math.Clamp((int)((bmin[0] - pmesh.minBounds[0]) * quantFactor), 0, 0xffff);
                    it.bmin[1] = Math.Clamp((int)((bmin[1] - pmesh.minBounds[1]) * quantFactor), 0, 0xffff);
                    it.bmin[2] = Math.Clamp((int)((bmin[2] - pmesh.minBounds[2]) * quantFactor), 0, 0xffff);


                    it.bmax[0] = Math.Clamp((int)((bmax[0] - pmesh.minBounds[0]) * quantFactor), 0, 0xffff);
                    it.bmax[1] = Math.Clamp((int)((bmax[1] - pmesh.minBounds[1]) * quantFactor), 0, 0xffff);
                    it.bmax[2] = Math.Clamp((int)((bmax[2] - pmesh.minBounds[2]) * quantFactor), 0, 0xffff);

                }
                else
                {

                    for (int j = 0; j < RecastConfig.MaxVertsPerPoly; j++)
                    {
                        int vertIndex = pmesh.polys[i * RecastConfig.MaxVertsPerPoly * 2 + j];

                        if (vertIndex == RecastConfig.RC_MESH_NULL_IDX)
                        {
                            break;
                        }


                        float x = pmesh.verts[vertIndex * 3];
                        float y = pmesh.verts[vertIndex * 3 + 1];
                        float z = pmesh.verts[vertIndex * 3 + 2];



                        it.bmin[0] = Math.Min(it.bmin[0], (int)x);
                        it.bmin[1] = Math.Min(it.bmin[1], (int)y);
                        it.bmin[2] = Math.Min(it.bmin[2], (int)z);

                        it.bmax[0] = Math.Max(it.bmax[0], (int)x);
                        it.bmax[1] = Math.Max(it.bmax[1], (int)y);
                        it.bmax[2] = Math.Max(it.bmax[2], (int)z);
                    }

                    it.bmin[1] = (int)Math.Floor(it.bmin[1] * pmesh.cellHeight / pmesh.cellSize);
                    it.bmax[1] = (int)Math.Floor(it.bmax[1] * pmesh.cellHeight / pmesh.cellSize);

                }

            }


            DtBVNode[] treeNodes = new DtBVNode[pmesh.npolys * 2];

            for (int i = 0; i < pmesh.npolys * 2; i++)
            {
                treeNodes[i] = new DtBVNode();
            }

            SubDivide(bnodes, 0, pmesh.npolys, treeNodes, 0);

            return treeNodes;
        }

        private static int SubDivide(DtBVNode[] bnodes, int imin, int imax, DtBVNode[] treeNodes, int curNode)
        {
            int inum = imax - imin;
            int icur = curNode;

            DtBVNode treeNode = treeNodes[curNode++];

            if (inum == 1)
            {
                // Leaf
                treeNode.bmin[0] = bnodes[imin].bmin[0];
                treeNode.bmin[1] = bnodes[imin].bmin[1];
                treeNode.bmin[2] = bnodes[imin].bmin[2];

                treeNode.bmax[0] = bnodes[imin].bmax[0];
                treeNode.bmax[1] = bnodes[imin].bmax[1];
                treeNode.bmax[2] = bnodes[imin].bmax[2];

                treeNode.i = bnodes[imin].i;
            }
            else
            {
                // Split
                CalcExtends(bnodes, imin, imax, treeNode);
            }

            return 1;
        }

        private static void CalcExtends(DtBVNode[] bnodes, int imin, int imax, DtBVNode treeNode)
        {

            treeNode.bmin[0] = bnodes[imin].bmin[0];
            treeNode.bmin[1] = bnodes[imin].bmin[1];
            treeNode.bmin[2] = bnodes[imin].bmin[2];

            treeNode.bmax[0] = bnodes[imin].bmax[0];
            treeNode.bmax[1] = bnodes[imin].bmax[1];
            treeNode.bmax[2] = bnodes[imin].bmax[2];

            for (int i = imin + 1; i < imax; ++i)
            {
                DtBVNode node = bnodes[i];
                if (treeNode.bmin[0] < node.bmin[0]) node.bmin[0] = treeNode.bmin[0];
                if (treeNode.bmin[1] < node.bmin[1]) node.bmin[1] = treeNode.bmin[1];
                if (treeNode.bmin[2] < node.bmin[2]) node.bmin[2] = treeNode.bmin[2];

                if (treeNode.bmax[0] > node.bmax[0]) node.bmax[0] = treeNode.bmax[0];
                if (treeNode.bmax[1] > node.bmax[1]) node.bmax[1] = treeNode.bmax[1];
                if (treeNode.bmax[2] > node.bmax[2]) node.bmax[2] = treeNode.bmax[2];
            }
        }

    }
}
