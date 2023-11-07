
using System;
using UnityEngine;

namespace GameEditor.RecastEditor
{
    internal class RecastMesh
    {
        static public void RcBuildPolyMesh(RcContourSet cset, RcPolyMesh ployMesh)
        {
            int maxVertices = 0;
            int maxTris = 0;
            int maxVertsPerCont = 0;
            for (int i = 0; i < cset.NumConts; ++i)
            {
                if (cset.ContsList[i].NumVerts < 3)
                {
                    continue;
                }
                maxVertices += cset.ContsList[i].NumVerts;
                maxTris += cset.ContsList[i].NumVerts - 2;
                maxVertsPerCont = Mathf.Max(maxVertsPerCont, cset.ContsList[i].NumVerts);
            }

            ployMesh.Verts = new int[maxVertsPerCont * 3];
            Array.Fill(ployMesh.Verts, 0);

            ployMesh.Polys = new int[maxTris * RecastConfig.MaxVertsPerPoly * 2];
            Array.Fill(ployMesh.Polys, 0xff);

            ployMesh.Regs = new int[maxTris];
            Array.Fill(ployMesh.Regs, 0);

            ployMesh.AreaList = new AREATYPE[maxTris];
            Array.Fill(ployMesh.AreaList, AREATYPE.None);



            int[] indices = new int[maxVertsPerCont];
            int[] tris = new int[maxVertsPerCont * 3];

            int[] firstVert = new int[maxVertsPerCont];
            Array.Fill(firstVert, -1);

            int[] nextVert = new int[maxVertsPerCont];
            Array.Fill(nextVert, 0);

            int[] vflags = new int[maxVertsPerCont];
            Array.Fill(vflags, 0);

            int[] polys = new int[(maxVertsPerCont + 1) * RecastConfig.MaxVertsPerPoly];

            for (int i = 0; i < cset.NumConts; ++i)
            {
                RcContour cont = cset.ContsList[i];

                if (cont.NumVerts < 3)
                    continue;


                for (int j = 0; j < cont.NumVerts; ++j)
                {
                    indices[j] = j;
                }

                //拆分多边形为三角形
                int ntris = Triangulate(cont.NumVerts, cont.Verts, indices, tris);
                if (ntris <= 0)
                {

                    Debug.LogErrorFormat("rcBuildPolyMesh: Bad triangulation Contour %d.", i);
                    ntris = -ntris;
                }


                //添加cont的vert到 polyMesh.verts，合并相近节点
                for (int j = 0; j < cont.NumVerts; ++j)
                {
                    int vIndex = j * 4;

                    //返回polyMesh.verts中的索引
                    indices[j] = AddVertex(cont.Verts[vIndex], cont.Verts[vIndex + 1], cont.Verts[vIndex + 2], firstVert, nextVert, ployMesh);

                    //假如该顶点是边缘，则加入移除列表
                    if ((cont.Verts[vIndex + 3] & RecastConfig.RC_BORDER_VERTEX) != 0)
                    {
                        vflags[indices[j]] = 1;
                    }
                }

                int npolys = 0;
                Array.Fill(polys, 0xff);
                for (int j = 0; j < ntris; ++j)
                {
                    int ti = j * 3;
                    if (tris[ti] != tris[ti + 1] && tris[ti] != tris[ti + 2] && tris[ti + 1] != tris[ti + 2])
                    {
                        polys[npolys * RecastConfig.MaxVertsPerPoly] = indices[tris[ti]];
                        polys[npolys * RecastConfig.MaxVertsPerPoly + 1] = indices[tris[ti + 1]];
                        polys[npolys * RecastConfig.MaxVertsPerPoly + 2] = indices[tris[ti + 2]];

                        npolys++;
                    }
                }

                if (npolys == 0)
                    continue;


                //合并三角形
                if (RecastConfig.MaxVertsPerPoly > 3)
                {
                    for (; ; )
                    {
                        // 找到最适合合并的多边形.
                        int bestMergeVal = 0;
                        int bestPa = 0, bestPb = 0, bestEa = 0, bestEb = 0;

                        for (int j = 0; j < npolys - 1; ++j)
                        {
                            int pj = j * RecastConfig.MaxVertsPerPoly;

                            for (int k = j + 1; k < npolys; ++k)
                            {
                                int pk = polys[k * RecastConfig.MaxVertsPerPoly];
                                int ea = 0;
                                int eb = 0;
                                int v = GetPolyMergeValue(pj, pk, ployMesh.Verts, ea, eb); ;
                                if (v > bestMergeVal)
                                {
                                    bestMergeVal = v;
                                    bestPa = j;
                                    bestPb = k;
                                    bestEa = ea;
                                    bestEb = eb;
                                }
                            }
                        }

                    }
                }
            }
        }

        private static int GetPolyMergeValue(int pa, int pb, int[] verts, int ea, int eb)
        {
            return 1;
        }


        private static int AddVertex(int x, int y, int z, int[] firstVert, int[] nextVert, RcPolyMesh polyMesh)
        {

            int[] verts = polyMesh.Verts;
            int bucket = ComputeVertexHash(x, 0, z);
            int i = firstVert[bucket];

            //遍历整个nextVert链查询是否有相近的节点
            while (i != -1)
            {
                int vi = i * 3;
                if (verts[vi] == x && (Math.Abs(verts[vi + 1] - y) <= 2) && verts[vi + 2] == z)
                {
                    return i;
                }

                i = nextVert[i];
            }

            i = polyMesh.NumVerts;
            polyMesh.NumVerts++;

            int vj = i * 3;
            verts[vj] = x;
            verts[vj + 1] = y;
            verts[vj + 2] = z;

            //将之前的节点的索引加入nextVert,
            nextVert[i] = firstVert[bucket];
            firstVert[bucket] = i;

            return i;
        }


        private static int Triangulate(int n, int[] verts, int[] indices, int[] tris)
        {
            int ntris = 0;

            int nvert = 0;

            for (int i = 0; i < n; i++)
            {
                int i1 = RecastUtility.Next(i, n);
                int i2 = RecastUtility.Next(i1, n);


                //把最高位作为标记位，i1为耳朵尖 ，判断i - i1 - i2 是满足耳裁法条件 
                //耳朵必须满足两个条件，耳朵尖是锥形，两个耳根的连线不与任何轮廓边相交
                if (Diagonal(i, i2, n, verts, indices))
                {
                    indices[i1] |= RecastConfig.RC_INDICE;
                }
            }

            while (n > 3)
            {
                int minLen = -1;
                int mini = -1;
                for (int i = 0; i < n; i++)
                {
                    int i1 = RecastUtility.Next(i, n);
                    if ((indices[i1] & RecastConfig.RC_INDICE) != 0)
                    {

                        int p0Index = indices[i] & RecastConfig.RC_INDICE_MASK;
                        int[] p0 = { verts[p0Index], verts[p0Index + 1], verts[p0Index + 2] };

                        int p2Index = indices[RecastUtility.Next(i1, n)] & RecastConfig.RC_INDICE_MASK;
                        int[] p2 = { verts[p2Index], verts[p2Index + 1], verts[p2Index + 2] };

                        int dx = p2[0] - p0[0];
                        int dy = p2[2] - p0[2];
                        int len = dx * dx + dy * dy;

                        if (minLen < 0 || len < minLen)
                        {
                            minLen = len;
                            mini = i;
                        }

                    }

                }

                if (mini == -1)
                {
                    //假如没有找到，可能是轮廓有所重叠导致的，放松裁剪的标准
                    for (int i = 0; i < n; i++)
                    {
                        int i1 = RecastUtility.Next(i, n);
                        int i2 = RecastUtility.Next(i1, n);

                        if (Diagonal(i, i2, n, verts, indices, true))
                        {
                            int p0Index = indices[i] & RecastConfig.RC_INDICE_MASK;
                            int[] p0 = { verts[p0Index], verts[p0Index + 1], verts[p0Index + 2] };

                            int p2Index = indices[RecastUtility.Next(i1, n)] & RecastConfig.RC_INDICE_MASK;
                            int[] p2 = { verts[p2Index], verts[p2Index + 1], verts[p2Index + 2] };

                            int dx = p2[0] - p0[0];
                            int dy = p2[2] - p0[2];
                            int len = dx * dx + dy * dy;

                            if (minLen < 0 || len < minLen)
                            {
                                minLen = len;
                                mini = i;
                            }
                        }

                        if (mini == -1)
                        {
                            //简化太严重，无法分割三角形
                            return -ntris;
                        }
                    }

                }

                int j = mini;
                int j1 = RecastUtility.Next(j, n);
                int j2 = RecastUtility.Next(j1, n);


                tris[nvert++] = indices[j] & RecastConfig.RC_INDICE_MASK;
                tris[nvert++] = indices[j1] & RecastConfig.RC_INDICE_MASK;
                tris[nvert++] = indices[j2] & RecastConfig.RC_INDICE_MASK;

                ntris++;

                //移除p[i1]节点
                n--;

                for (int k = j1; k < n; k++)
                    indices[k] = indices[k + 1];

                if (j1 >= n) j1 = 0;
                j = RecastUtility.Prev(j1, n);

                //更新 最新的i和i1节点的是否满足耳裁法

                if (Diagonal(RecastUtility.Prev(j, n), j1, n, verts, indices))
                    indices[j] |= RecastConfig.RC_INDICE;
                else
                    indices[j] &= RecastConfig.RC_INDICE_MASK;

                if (Diagonal(j, RecastUtility.Next(j1, n), n, verts, indices))
                    indices[j1] |= RecastConfig.RC_INDICE;
                else
                    indices[j1] &= RecastConfig.RC_INDICE_MASK;
            }


            tris[nvert++] = indices[0] & RecastConfig.RC_INDICE_MASK;
            tris[nvert++] = indices[1] & RecastConfig.RC_INDICE_MASK;
            tris[nvert++] = indices[2] & RecastConfig.RC_INDICE_MASK;

            ntris++;

            return ntris;
        }

        private static bool Vequal(int[] a, int[] b)
        {
            return a[0] == b[0] && a[2] == b[2];
        }


        private static bool Diagonal(int i, int j, int n, int[] verts, int[] indices, bool loose = false)
        {

            int pjIndex = indices[j] & RecastConfig.RC_INDICE_MASK;
            int[] pj = { verts[pjIndex], verts[pjIndex + 1], verts[pjIndex + 2] };

            int piIndex = indices[i] & RecastConfig.RC_INDICE_MASK;
            int[] pi = { verts[piIndex], verts[piIndex + 1], verts[piIndex + 2] };

            int pi1Index = indices[RecastUtility.Next(i, n)] & RecastConfig.RC_INDICE_MASK;
            int[] pi1 = { verts[pi1Index], verts[pi1Index + 1], verts[pi1Index + 2] };

            int pin1Index = indices[RecastUtility.Prev(i, n)] & RecastConfig.RC_INDICE_MASK;
            int[] pin1 = { verts[pin1Index], verts[pin1Index + 1], verts[pin1Index + 2] };

            return RecastUtility.InCone(pi, pi1, pin1, pj, loose) && Diagonalie(i, j, n, verts, indices, loose);
        }

        // i j 构成的线段是否与 verts所在的多边形的边缘相交,不相交返回true
        private static bool Diagonalie(int i, int j, int n, int[] verts, int[] indices, bool loose = false)
        {
            int d0Index = indices[i] & RecastConfig.RC_INDICE_MASK;
            int[] d0 = { verts[d0Index], verts[d0Index + 1], verts[d0Index + 2], verts[d0Index + 3] };

            int d1Index = indices[j] & RecastConfig.RC_INDICE_MASK;
            int[] d1 = { verts[d1Index], verts[d1Index + 1], verts[d1Index + 2], verts[d1Index + 3] };

            //遍历 （k，k+1)
            for (int k = 0; k < n; k++)
            {
                int k1 = RecastUtility.Next(k, n);
                //跳过当前点所在边缘
                if (!((k == i) || (k1 == i) || (k == j) || (k1 == j)))
                    continue;

                int p0Index = indices[k] & RecastConfig.RC_INDICE_MASK;
                int p1Index = indices[k1] & RecastConfig.RC_INDICE_MASK;

                int[] p0 = { verts[p0Index], verts[p0Index + 1], verts[p0Index + 2], verts[p0Index + 3] };
                int[] p1 = { verts[p1Index], verts[p1Index + 1], verts[p1Index + 2], verts[p1Index + 3] };

                if (Vequal(d0, p0) || Vequal(d1, p0) || Vequal(d0, p1) || Vequal(d1, p1))
                    continue;

                if (RecastUtility.Intersect(d0, d1, p0, p1, loose))
                    return false;
            }
            return true;
        }

        private static int ComputeVertexHash(int x, int y, int z)
        {
            uint h1 = 0x8da6b343;
            uint h2 = 0xd8163841;
            uint h3 = 0xcb1ab31f;
            uint n = (uint)(h1 * x + h2 * y + h3 * z);
            return (int)(n & (RecastConfig.VERTEX_BUCKET_COUNT - 1));
        }
    }
}

