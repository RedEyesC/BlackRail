
using System;

namespace GameFramework.Detour
{

    public class DetourNavMeshBuild
    {
        public static DtNavData DtCreateNavMeshData(DtNavData param)
        {

            //初始化二叉树
            param.treeNodes = CreateBVTree(param);


            //变化顶点坐标到世界坐标系下
            param.navVerts = new float[param.vertCount * 3];
            for (int i = 0; i < param.vertCount; ++i)
            {
                param.navVerts[i * 3] = param.bmin[0] + param.verts[i * 3] * param.cs;
                param.navVerts[i * 3 + 1] = param.bmin[1] + param.verts[i * 3 + 1] * param.ch;
                param.navVerts[i * 3 + 2] = param.bmin[2] + param.verts[i * 3 + 2] * param.cs;
            }

            //初始化 navPolys
            param.navPolys = new DtPoly[param.polyCount];

            for (int i = 0; i < param.polyCount; i++)
            {
                param.navPolys[i] = new DtPoly();
            }

            int[] src = param.polys;
            int nvp = DetourConfig.MaxVertsPerPoly;
            int d = 0;
            for (int i = 0; i < param.polyCount; ++i)
            {
                DtPoly p = param.navPolys[i];

                p.vertCount = 0;
                p.verts = new int[nvp];

                p.neis = new int[nvp];
                Array.Fill(p.neis, DetourConfig.DT_MESH_NULL_IDX);

                p.flags = param.polyFlags[i];

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

            //初始化 links
            int edgeCount = 0;
            for (int i = 0; i < param.polyCount; ++i)
            {
                int p = i * 2 * nvp;
                for (int j = 0; j < nvp; ++j)
                {
                    if (param.polys[p + j] == DetourConfig.DT_MESH_NULL_IDX) break;
                    edgeCount++;
                }
            }

            param.maxLinkCount = edgeCount;

            param.links = new DtLink[param.maxLinkCount];

            for (int i = 0; i < param.maxLinkCount; i++)
            {
                param.links[i] = new DtLink();
            }

            param.linksFreeList = 0;

            param.links[param.maxLinkCount - 1].next = DetourConfig.DT_NULL_LINK;

            for (int i = 0; i < param.maxLinkCount - 1; ++i)
            {
                param.links[i].next = i + 1;
            }

            for (int i = 0; i < param.polyCount; ++i)
            {
                DtPoly poly = param.navPolys[i];
                poly.firstLink = DetourConfig.DT_NULL_LINK;

                for (int j = poly.vertCount - 1; j >= 0; --j)
                {

                    if (poly.neis[j] == DetourConfig.DT_MESH_NULL_IDX)
                        continue;

                    int idx = AllocLink(param);
                    if (idx != DetourConfig.DT_NULL_LINK)
                    {
                        DtLink link = param.links[idx];
                        link.refId = poly.neis[j];
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

            float[] tempVector3 = new float[3];
            float[] bmin = new float[3];
            float[] bmax = new float[3];

            for (int i = 0; i < param.polyCount; i++)
            {
                DtBVNode it = bnodes[i];
                it.i = i;


                int vb = param.detailMeshes[i * 4 + 0];
                int ndv = param.detailMeshes[i * 4 + 1];


                int dvIndex = vb * 3;
                tempVector3[0] = param.detailVerts[dvIndex];
                tempVector3[1] = param.detailVerts[dvIndex + 1];
                tempVector3[2] = param.detailVerts[dvIndex + 2];

                DetourUtility.DtVcopy(bmin, tempVector3);
                DetourUtility.DtVcopy(bmax, tempVector3);

                for (int j = 1; j < ndv; j++)
                {
                    tempVector3[0] = param.detailVerts[dvIndex + j * 3];
                    tempVector3[1] = param.detailVerts[dvIndex + j * 3 + 1];
                    tempVector3[2] = param.detailVerts[dvIndex + j * 3 + 2];

                    DetourUtility.DtVmin(bmin, tempVector3);
                    DetourUtility.DtVmax(bmax, tempVector3);
                }


                it.bmin[0] = Math.Clamp((int)((bmin[0] - param.bmin[0]) * param.quantFactor), 0, 0xffff);
                it.bmin[1] = Math.Clamp((int)((bmin[1] - param.bmin[1]) * param.quantFactor), 0, 0xffff);
                it.bmin[2] = Math.Clamp((int)((bmin[2] - param.bmin[2]) * param.quantFactor), 0, 0xffff);


                it.bmax[0] = Math.Clamp((int)((bmax[0] - param.bmin[0]) * param.quantFactor), 0, 0xffff);
                it.bmax[1] = Math.Clamp((int)((bmax[1] - param.bmin[1]) * param.quantFactor), 0, 0xffff);
                it.bmax[2] = Math.Clamp((int)((bmax[2] - param.bmin[2]) * param.quantFactor), 0, 0xffff);

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
                if (node.bmin[0] < treeNode.bmin[0]) treeNode.bmin[0] = node.bmin[0];
                if (node.bmin[1] < treeNode.bmin[1]) treeNode.bmin[1] = node.bmin[1];
                if (node.bmin[2] < treeNode.bmin[2]) treeNode.bmin[2] = node.bmin[2];

                if (node.bmax[0] > treeNode.bmax[0]) treeNode.bmax[0] = node.bmax[0];
                if (node.bmax[1] > treeNode.bmax[1]) treeNode.bmax[1] = node.bmax[1];
                if (node.bmax[2] > treeNode.bmax[2]) treeNode.bmax[2] = node.bmax[2];
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

