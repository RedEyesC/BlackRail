
using GameEditor.RecastEditor;
using System;

namespace GameEditor.DetourEditor
{

    internal class DetourNavMeshBuild
    {
        public static DtNavData DtCreateNavMeshData(RcPolyMesh pmesh, RcPolyMeshDetail dmesh)
        {
            DtNavData param = new DtNavData();

            param.polyCount = pmesh.npolys;
            param.polys = pmesh.polys;

            param.walkableClimb = pmesh.walkableClimb;
            param.cs = pmesh.cellSize;
            param.ch = pmesh.cellHeight;

            param.polyAreas = pmesh.areas;
            param.polyFlags = pmesh.flags;

            param.detailMeshes = dmesh.meshes;
            param.detailVerts = dmesh.verts;
            param.detailTris = dmesh.tris;
            param.detailVertsCount = dmesh.nverts;

            param.quantFactor = 1.0f / pmesh.cellSize;

            param.nvp = pmesh.nvp;
            param.nullIdx = RecastConfig.RC_MESH_NULL_IDX;

            param.treeNodes = CreateBVTree(param);

            param.navPolys = new DtPoly[pmesh.npolys];

            int[] src = param.polys;
            int nvp = param.nvp;
            int d = 0;
            for (int i = 0; i < param.polyCount; ++i)
            {
                DtPoly p = param.navPolys[i];
                p.vertCount = 0;
                p.verts = new int[nvp];
                p.neis = new int[nvp];
                p.flags = param.polyFlags[i];
                p.SetArea((int)param.polyAreas[i]);
                p.SetPType(DtPolyTypes.DT_POLYTYPE_GROUND);
                for (int j = 0; j < nvp; ++j)
                {
                    if (src[d + j] == DetourConfig.MESH_NULL_IDX) {
                        break;
                    } 

                    p.verts[j] = src[d + j];
                   
                    p.neis[j] = src[d + nvp + j];
                    
                    p.vertCount++;
                }

                d += nvp * 2;
            }

            return param;
        }

        private static DtBVNode[] CreateBVTree(DtNavData param)
        {

            DtBVNode[] bnodes = new DtBVNode[param.polyCount];

            float[] bmin = new float[3];
            float[] bmax = new float[3];


            for (int i = 0; i < param.polyCount; i++)
            {
                bnodes[i] = new DtBVNode();
            }

            for (int i = 0; i < param.polyCount; i++)
            {
                DtBVNode it = bnodes[i];
                it.i = i;
                //if (dmesh != null)
                //{
                //    int vb = dmesh.meshes[i * 4 + 0];
                //    int ndv = dmesh.meshes[i * 4 + 1];


                //    int dvIndex = vb * 3;
                //    tempVector3[0] = dmesh.verts[dvIndex];
                //    tempVector3[1] = dmesh.verts[dvIndex + 1];
                //    tempVector3[2] = dmesh.verts[dvIndex + 2];

                //    RecastUtility.RcVcopy(bmin, tempVector3);
                //    RecastUtility.RcVcopy(bmax, tempVector3);

                //    for (int j = 1; j < ndv; j++)
                //    {
                //        tempVector3[0] = dmesh.verts[dvIndex + j * 3];
                //        tempVector3[1] = dmesh.verts[dvIndex + j * 3 + 1];
                //        tempVector3[2] = dmesh.verts[dvIndex + j * 3 + 2];

                //        RecastUtility.RcVmin(bmin, tempVector3);
                //        RecastUtility.RcVmax(bmax, tempVector3);
                //    }


                //    it.bmin[0] = Math.Clamp((int)((bmin[0] - param.minBounds[0]) * param.quantFactor), 0, 0xffff);
                //    it.bmin[1] = Math.Clamp((int)((bmin[1] - param.minBounds[1]) * param.quantFactor), 0, 0xffff);
                //    it.bmin[2] = Math.Clamp((int)((bmin[2] - param.minBounds[2]) * param.quantFactor), 0, 0xffff);


                //    it.bmax[0] = Math.Clamp((int)((bmax[0] - param.minBounds[0]) * param.quantFactor), 0, 0xffff);
                //    it.bmax[1] = Math.Clamp((int)((bmax[1] - param.minBounds[1]) * param.quantFactor), 0, 0xffff);
                //    it.bmax[2] = Math.Clamp((int)((bmax[2] - param.minBounds[2]) * param.quantFactor), 0, 0xffff);

                //}
                //else
                {

                    for (int j = 0; j < param.nvp; j++)
                    {
                        int vertIndex = param.polys[i * param.nvp * 2 + j];

                        if (vertIndex == param.nullIdx)
                        {
                            break;
                        }

                        float x = param.verts[vertIndex * 3];
                        float y = param.verts[vertIndex * 3 + 1];
                        float z = param.verts[vertIndex * 3 + 2];


                        it.bmin[0] = Math.Min(it.bmin[0], (int)x);
                        it.bmin[1] = Math.Min(it.bmin[1], (int)y);
                        it.bmin[2] = Math.Min(it.bmin[2], (int)z);

                        it.bmax[0] = Math.Max(it.bmax[0], (int)x);
                        it.bmax[1] = Math.Max(it.bmax[1], (int)y);
                        it.bmax[2] = Math.Max(it.bmax[2], (int)z);
                    }

                    it.bmin[1] = (int)Math.Floor(it.bmin[1] * param.ch / param.cs);
                    it.bmax[1] = (int)Math.Floor(it.bmax[1] * param.ch / param.cs);

                }

            }


            DtBVNode[] treeNodes = new DtBVNode[param.polyCount * 2];

            for (int i = 0; i < param.polyCount * 2; i++)
            {
                treeNodes[i] = new DtBVNode();
            }

            param.nodesNum = SubDivide(bnodes, 0, param.polyCount, treeNodes, 0);

            return treeNodes;
        }

        private static int SubDivide(DtBVNode[] bnodes, int imin, int imax, DtBVNode[] treeNodes, int curNode)
        {
            int inum = imax - imin;
            int icur = curNode;

            DtBVNode treeNode = treeNodes[curNode++];

            if (inum == 1)
            {
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
                CalcExtends(bnodes, imin, imax, treeNode);

                int axis = LongestAxis(treeNode.bmax[0] - treeNode.bmin[0],
                               treeNode.bmax[1] - treeNode.bmin[1],
                               treeNode.bmax[2] - treeNode.bmin[2]);


                if (axis == 0)
                {
                    Array.Sort(bnodes, imin, imax - imin, new SortWithX());
                }
                else if (axis == 1)
                {
                    Array.Sort(bnodes, imin, imax - imin, new SortWithY());
                }
                else
                {
                    Array.Sort(bnodes, imin, imax - imin, new SortWithZ());
                }

                int isplit = imin + inum / 2;

                curNode = SubDivide(bnodes, imin, isplit, treeNodes, curNode);

                curNode = SubDivide(bnodes, isplit, imax, treeNodes, curNode);

                int iescape = curNode - icur;

                //取反 和叶子节点区分开
                treeNode.i = -iescape;

            }

            return curNode;
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

        private static int LongestAxis(int x, int y, int z)
        {
            int axis = 0;
            int maxVal = x;
            if (y > maxVal)
            {
                axis = 1;
                maxVal = y;
            }
            if (z > maxVal)
            {
                axis = 2;
            }
            return axis;
        }
    }
}

