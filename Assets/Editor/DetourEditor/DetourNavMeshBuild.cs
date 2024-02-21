
using GameEditor.RecastEditor;
using System;

namespace GameEditor.DetourEditor
{

    internal class DetourNavMeshBuild
    {
        public static DtNavData DtCreateNavMeshData(RcPolyMesh pmesh, RcPolyMeshDetail dmesh)
        {
            DtNavData param = new DtNavData();

            param.bmin = pmesh.minBounds;
            param.bmax = pmesh.maxBounds;
            param.polyCount = pmesh.npolys;
            param.polys = pmesh.polys;
            param.verts = pmesh.verts;

            param.walkableClimb = pmesh.walkableClimb;
            param.cs = pmesh.cellSize;
            param.ch = pmesh.cellHeight;

            param.polyAreas = pmesh.areas;
            param.polyFlags = pmesh.flags;
             
            //TODO
            //param.detailMeshes = dmesh.meshes;
            //param.detailVerts = dmesh.verts;
            //param.detailTris = dmesh.tris;
            //param.detailVertsCount = dmesh.nverts;

            param.quantFactor = 1.0f / pmesh.cellSize;

            param.treeNodes = CreateBVTree(param);

            param.navPolys = new DtPoly[pmesh.npolys];

            int[] src = param.polys;
            int nvp = DetourConfig.MaxVertsPerPoly;
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
                    if (src[d + j] == DetourConfig.DT_MESH_NULL_IDX)
                    {
                        break;
                    }

                    p.verts[j] = src[d + j];

                    p.neis[j] = src[d + nvp + j];

                    p.vertCount++;
                }

                d += nvp * 2;
            }

            int edgeCount = 0;
            int portalCount = 0;
            for (int i = 0; i < param.polyCount; ++i)
            {
                int p = i * 2 * nvp;
                for (int j = 0; j < nvp; ++j)
                {
                    if (param.polys[p + j] == DetourConfig.DT_MESH_NULL_IDX) break;
                    edgeCount++;

                    int dirIndex = p + nvp + j;
                    if ((param.polys[dirIndex] & 0x8000) == 0)
                    {
                        int dir = param.polys[dirIndex] & 0xf;
                        if (dir != 0xf)
                            portalCount++;
                    }

                }
            }

            param.maxLinkCount = edgeCount + portalCount * 2;

            param.links = new DtLink[param.maxLinkCount];

            param.linksFreeList = 0;

            param.links[param.maxLinkCount - 1].next = DetourConfig.DT_NULL_LINK;

            for (int i = 0; i < param.polyCount; ++i)
            {
                DtPoly poly = param.navPolys[i];
                poly.firstLink = DetourConfig.DT_NULL_LINK;

                for (int j = poly.vertCount - 1; j >= 0; --j)
                {

                    //TODO
                    //if (poly.neis[j]) 
                    //    continue;

                    int idx = AllocLink(param);
                    if (idx != DetourConfig.DT_NULL_LINK)
                    {
                        DtLink link = param.links[idx];
                        link.refId = poly.neis[j] - 1;
                        link.edge = j;
                        link.side = 0xff;
                        link.bmin = link.bmax = 0;
                        link.next = poly.firstLink;
                        poly.firstLink = idx;
                    }
                }

            }

            return param;
        }


        private static int AllocLink(DtNavData param)
        {
            if (param.linksFreeList == DetourConfig.DT_NULL_LINK)
                return DetourConfig.DT_NULL_LINK;
            int link = param.linksFreeList;
            param.linksFreeList = param.links[link].next;
            return link;
        }

        private static void FreeLink(DtNavData param, int link)
        {
            param.links[link].next = param.linksFreeList;
            param.linksFreeList = link;
        }

        private static DtBVNode[] CreateBVTree(DtNavData param)
        {

            DtBVNode[] bnodes = new DtBVNode[param.polyCount];

            for (int i = 0; i < param.polyCount; i++)
            {
                bnodes[i] = new DtBVNode();
            }

            for (int i = 0; i < param.polyCount; i++)
            {
                DtBVNode it = bnodes[i];
                it.i = i;


                for (int j = 0; j < DetourConfig.MaxVertsPerPoly; j++)
                {
                    int vertIndex = param.polys[i * DetourConfig.MaxVertsPerPoly * 2 + j];

                    if (vertIndex == DetourConfig.DT_MESH_NULL_IDX)
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

