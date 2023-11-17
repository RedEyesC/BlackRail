
using System;
using UnityEngine;

namespace GameEditor.RecastEditor
{
    internal class RecastMesh
    {
        static public void RcBuildPolyMesh(RcContourSet cset, RcPolyMesh pmesh)
        {
            int maxVertices = 0;
            int maxTris = 0;
            int maxVertsPerCont = 0;
            for (int i = 0; i < cset.numConts; ++i)
            {
                if (cset.conts[i].numVerts < 3)
                {
                    continue;
                }
                maxVertices += cset.conts[i].numVerts;
                maxTris += cset.conts[i].numVerts - 2;
                maxVertsPerCont = Mathf.Max(maxVertsPerCont, cset.conts[i].numVerts);
            }

            pmesh.verts = new int[maxVertices * 3];
            Array.Fill(pmesh.verts, 0);

            pmesh.polys = new int[maxTris * RecastConfig.MaxVertsPerPoly * 2];
            Array.Fill(pmesh.polys, RecastConfig.RC_MESH_NULL_IDX);

            pmesh.regs = new int[maxTris];
            Array.Fill(pmesh.regs, 0);

            pmesh.areas = new AREATYPE[maxTris];
            Array.Fill(pmesh.areas, AREATYPE.None);



            int[] indices = new int[maxVertsPerCont];
            int[] tris = new int[maxVertsPerCont * 3];

            int[] firstVert = new int[RecastConfig.VERTEX_BUCKET_COUNT];
            Array.Fill(firstVert, -1);

            int[] nextVert = new int[maxVertices];
            Array.Fill(nextVert, 0);

            int[] vflags = new int[maxVertsPerCont];
            Array.Fill(vflags, 0);


            int[] pregs = new int[RecastConfig.MaxVertsPerPoly];
            int[] pareas = new int[RecastConfig.MaxVertsPerPoly];
            int[] polys = new int[maxVertsPerCont * RecastConfig.MaxVertsPerPoly];
            int[] tmpPoly = new int[RecastConfig.MaxVertsPerPoly];

            for (int i = 0; i < cset.numConts; ++i)
            {
                RcContour cont = cset.conts[i];

                if (cont.numVerts < 3)
                    continue;


                for (int j = 0; j < cont.numVerts; ++j)
                {
                    indices[j] = j;
                }

                //拆分多边形为三角形
                int ntris = Triangulate(cont.numVerts, cont.verts, indices, tris);
                if (ntris <= 0)
                {

                    Debug.LogWarningFormat("rcBuildPolyMesh: Bad triangulation Contour {0}", i);
                    ntris = -ntris;
                }


                //添加cont的vert到 polyMesh.verts，合并相近节点
                for (int j = 0; j < cont.numVerts; ++j)
                {
                    int vIndex = j * 4;

                    //返回polyMesh.verts中的索引
                    indices[j] = AddVertex(cont.verts[vIndex], cont.verts[vIndex + 1], cont.verts[vIndex + 2], firstVert, nextVert, pmesh);
                }

                int npolys = 0;
                Array.Fill(polys, RecastConfig.RC_MESH_NULL_IDX);
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
                        //只要保证两个凸多边形共边的两点仍然是凸点，那么组合后仍然是凸多边形
                        int bestMergeVal = 0;
                        int bestPa = 0, bestPb = 0, bestEa = 0, bestEb = 0;

                        for (int j = 0; j < npolys - 1; ++j)
                        {
                            int pj = j * RecastConfig.MaxVertsPerPoly;

                            for (int k = j + 1; k < npolys; ++k)
                            {
                                int pk = k * RecastConfig.MaxVertsPerPoly;
                                int ea = -1;
                                int eb = -1;
                                int v = GetPolyMergeValue(pj, pk, pmesh.verts, polys, out ea, out eb); ;
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

                        if (bestMergeVal > 0)
                        {
                            int pa = bestPa * RecastConfig.MaxVertsPerPoly;
                            int pb = bestPb * RecastConfig.MaxVertsPerPoly;


                            MergePolyVerts(pa, pb, bestEa, bestEb, tmpPoly, polys);
                            if (pregs[bestPa] != pregs[bestPb])
                            {
                                pregs[bestPa] = RecastConfig.RC_MULTIPLE_REGS;
                            }

                            int last = (npolys - 1) * RecastConfig.MaxVertsPerPoly;

                            if (pb != last)
                            {
                                for (int k = 0; k < RecastConfig.MaxVertsPerPoly; ++k)
                                {
                                    polys[pb + k] = polys[last + k];
                                }
                            }

                            pregs[bestPb] = pregs[npolys - 1];
                            pareas[bestPb] = pareas[npolys - 1];
                            npolys--;
                        }
                        else
                        {
                            break;
                        }

                    }
                }

                for (int j = 0; j < npolys; ++j)
                {
                    if (pmesh.numPolys >= maxTris)
                    {
                        break;
                    }

                    int offset = pmesh.numPolys * RecastConfig.MaxVertsPerPoly * 2;

                    for (int k = 0; k < RecastConfig.MaxVertsPerPoly; ++k)
                    {
                        pmesh.polys[offset + k] = polys[j * RecastConfig.MaxVertsPerPoly + k];
                    }

                    pmesh.regs[pmesh.numPolys] = cont.reg;
                    pmesh.areas[pmesh.numPolys] = cont.area;
                    pmesh.numPolys++;

                    if (pmesh.numPolys > maxTris)
                    {
                        Debug.LogErrorFormat("removeVertex: Too many polygons {0} (max:{1}).", pmesh.numPolys, maxTris);
                    }
                }

            }

            if (!BuildMeshAdjacency(pmesh.polys, pmesh.numPolys, pmesh.numVerts))
            {
                Debug.LogError("rcBuildPolyMesh: Adjacency failed.");
            }

        }


        public static void RcBuildPolyMeshDetail(RcPolyMesh pmesh, RcCompactHeightfield chf, RcPolyMeshDetail dmesh)
        {
            if (pmesh.numPolys == 0 || pmesh.numVerts == 0)
                return;

            int nvp = pmesh.numPolys;
            float cs = pmesh.cellSize;
            float ch = pmesh.cellHeight;
            Vector3 orig = pmesh.minBounds;
            float heightSearchRadius = Mathf.Max(1, Mathf.Ceil(RecastConfig.MaxSimplificationError));


            RcHeightPatch hp = new RcHeightPatch();

            int[] bounds = new int[pmesh.numPolys * 4];
            float[] poly = new float[pmesh.numPolys * 3];

            int nPolyVerts = 0;
            int maxhw = 0;
            int maxhh = 0;

            //获取每个多边形的xy范围
            for (int i = 0; i < pmesh.numPolys; ++i)
            {
                int pIndex = i * nvp * 2;

                int xmin = chf.width;
                int xmax = 0;
                int ymin = chf.height;
                int ymax = 0;

                for (int j = 0; j < nvp; ++j)
                {
                    if (pmesh.polys[pIndex] == RecastConfig.RC_MESH_NULL_IDX)
                    {
                        break;
                    }

                    int vIndex = pmesh.polys[pIndex] * 3;
                    xmin = Mathf.Min(xmin, pmesh.verts[vIndex]);
                    xmax = Mathf.Max(xmax, pmesh.verts[vIndex]);
                    ymin = Mathf.Min(ymin, pmesh.verts[vIndex + 2]);
                    ymax = Mathf.Max(ymax, pmesh.verts[vIndex + 2]);
                    nPolyVerts++;
                }
                xmin = Mathf.Max(0, xmin - 1);
                xmax = Mathf.Min(chf.width, xmax + 1);
                ymin = Mathf.Max(0, ymin - 1);
                ymax = Mathf.Min(chf.height, ymax + 1);
                if (xmin >= xmax || ymin >= ymax)
                {
                    continue;
                }
                maxhw = Mathf.Max(maxhw, xmax - xmin);
                maxhh = Mathf.Max(maxhh, ymax - ymin);

                bounds[i * 4 + 0] = xmin;
                bounds[i * 4 + 1] = xmax;
                bounds[i * 4 + 2] = ymin;
                bounds[i * 4 + 3] = ymax;
            }

            dmesh.numMeshes = pmesh.numPolys;
            dmesh.meshes = new int[dmesh.numMeshes * 4];

            int vcap = nPolyVerts + nPolyVerts / 2;
            int tcap = vcap * 2;

            dmesh.verts = new float[vcap * 3];
            dmesh.tris = new int[tcap * 4];

            hp.data = new int[maxhw * maxhh];

            for (int i = 0; i < pmesh.numPolys; ++i)
            {
                int pIndex = i * nvp * 2;

                int npoly = 0;
                for (int j = 0; j < nvp; ++j)
                {
                    if (pmesh.polys[pIndex + j] == RecastConfig.RC_MESH_NULL_IDX)
                    {
                        break;
                    }

                    int vIndex = pmesh.polys[pIndex] * 3;
                    poly[j * 3 + 0] = pmesh.verts[vIndex] * cs;
                    poly[j * 3 + 1] = pmesh.verts[vIndex + 1] * ch;
                    poly[j * 3 + 2] = pmesh.verts[vIndex + 2] * cs;
                    npoly++;
                }

                hp.xmin = bounds[i * 4 + 0];
                hp.ymin = bounds[i * 4 + 2];
                hp.width = bounds[i * 4 + 1] - bounds[i * 4 + 0];
                hp.height = bounds[i * 4 + 3] - bounds[i * 4 + 2];
                //getHeightData(ctx, chf, p, npoly, mesh.verts, borderSize, hp, arr, mesh.regs[i]);

                //// Build detail mesh.
                //int nverts = 0;
                //if (!buildPolyDetail(ctx, poly, npoly,
                //                     sampleDist, sampleMaxError,
                //                     heightSearchRadius, chf, hp,
                //                     verts, nverts, tris,
                //                     edges, samples))
                //{
                //    return false;
                //}

                //// Move detail verts to world space.
                //for (int j = 0; j < nverts; ++j)
                //{
                //    verts[j * 3 + 0] += orig[0];
                //    verts[j * 3 + 1] += orig[1] + chf.ch; // Is this offset necessary?
                //    verts[j * 3 + 2] += orig[2];
                //}
                //// Offset poly too, will be used to flag checking.
                //for (int j = 0; j < npoly; ++j)
                //{
                //    poly[j * 3 + 0] += orig[0];
                //    poly[j * 3 + 1] += orig[1];
                //    poly[j * 3 + 2] += orig[2];
                //}

                //    // Store detail submesh.
                //    const int ntris = tris.size() / 4;

                //    dmesh.meshes[i * 4 + 0] = (unsigned int)dmesh.nverts;
                //dmesh.meshes[i * 4 + 1] = (unsigned int)nverts;
                //dmesh.meshes[i * 4 + 2] = (unsigned int)dmesh.ntris;
                //dmesh.meshes[i * 4 + 3] = (unsigned int)ntris;

                //// Store vertices, allocate more memory if necessary.
                //if (dmesh.nverts + nverts > vcap)
                //{
                //    while (dmesh.nverts + nverts > vcap)
                //        vcap += 256;

                //    float* newv = (float*)rcAlloc(sizeof(float) * vcap * 3, RC_ALLOC_PERM);
                //    if (!newv)
                //    {
                //        ctx->log(RC_LOG_ERROR, "rcBuildPolyMeshDetail: Out of memory 'newv' (%d).", vcap * 3);
                //        return false;
                //    }
                //    if (dmesh.nverts)
                //        memcpy(newv, dmesh.verts, sizeof(float) * 3 * dmesh.nverts);
                //    rcFree(dmesh.verts);
                //    dmesh.verts = newv;
                //}
                //for (int j = 0; j < nverts; ++j)
                //{
                //    dmesh.verts[dmesh.nverts * 3 + 0] = verts[j * 3 + 0];
                //    dmesh.verts[dmesh.nverts * 3 + 1] = verts[j * 3 + 1];
                //    dmesh.verts[dmesh.nverts * 3 + 2] = verts[j * 3 + 2];
                //    dmesh.nverts++;
                //}

                //// Store triangles, allocate more memory if necessary.
                //if (dmesh.ntris + ntris > tcap)
                //{
                //    while (dmesh.ntris + ntris > tcap)
                //        tcap += 256;
                //    unsigned char* newt = (unsigned char*)rcAlloc(sizeof(unsigned char) * tcap * 4, RC_ALLOC_PERM);
                //    if (!newt)
                //    {
                //        ctx->log(RC_LOG_ERROR, "rcBuildPolyMeshDetail: Out of memory 'newt' (%d).", tcap * 4);
                //        return false;
                //    }
                //    if (dmesh.ntris)
                //        memcpy(newt, dmesh.tris, sizeof(unsigned char) * 4 * dmesh.ntris);
                //    rcFree(dmesh.tris);
                //    dmesh.tris = newt;
                //}
                //for (int j = 0; j < ntris; ++j)
                //{
                //    const int* t = &tris[j * 4];
                //    dmesh.tris[dmesh.ntris * 4 + 0] = (unsigned char)t[0];
                //dmesh.tris[dmesh.ntris * 4 + 1] = (unsigned char)t[1];
                //dmesh.tris[dmesh.ntris * 4 + 2] = (unsigned char)t[2];
                //dmesh.tris[dmesh.ntris * 4 + 3] = getTriFlags(&verts[t[0] * 3], &verts[t[1] * 3], &verts[t[2] * 3], poly, npoly);
                //dmesh.ntris++;
            }
        }




        private static int GetPolyMergeValue(int pa, int pb, int[] verts, int[] polys, out int ea, out int eb)
        {
            ea = -1;
            eb = -1;

            int na = CountPolyVerts(pa, polys);
            int nb = CountPolyVerts(pb, polys);

            //假如顶点数超过标准，就不再合并了
            if (na + nb - 2 > RecastConfig.MaxVertsPerPoly)
                return -1;


            for (int i = 0; i < na; ++i)
            {
                int va0 = polys[pa + i];
                int va1 = polys[pa + (i + 1) % na];
                if (va0 > va1)
                {
                    RecastUtility.RcSwap(va0, va1);
                }

                for (int j = 0; j < nb; ++j)
                {
                    int vb0 = polys[pb + j];
                    int vb1 = polys[pb + (j + 1) % nb];
                    if (vb0 > vb1)
                    {
                        RecastUtility.RcSwap(vb0, vb1);
                    }

                    if (va0 == vb0 && va1 == vb1)
                    {
                        ea = i;
                        eb = j;
                        break;
                    }
                }
            }

            //未发现可合并边
            if (ea == -1 || eb == -1)
                return -1;


            //检查合并后的图形是否满足凸多边形的要求
            int va, vb, vc;
            int[] verta, vertb, vertc;

            va = polys[pa + (ea + na - 1) % na];
            vb = polys[pa + ea];
            vc = polys[pb + (eb + 2) % nb];

            verta = new int[] { verts[va * 3], verts[va * 3 + 1], verts[va * 3 + 2] };
            vertb = new int[] { verts[vb * 3], verts[vb * 3 + 1], verts[vb * 3 + 2] };
            vertc = new int[] { verts[vc * 3], verts[vc * 3 + 1], verts[vc * 3 + 2] };

            if (!RecastUtility.Left(verta, vertb, vertc))
            {
                return -1;
            }


            va = polys[pb + (eb + nb - 1) % nb];
            vb = polys[pb + eb];
            vc = polys[pa + (ea + 2) % na];

            verta = new int[] { verts[va * 3], verts[va * 3 + 1], verts[va * 3 + 2] };
            vertb = new int[] { verts[vb * 3], verts[vb * 3 + 1], verts[vb * 3 + 2] };
            vertc = new int[] { verts[vc * 3], verts[vc * 3 + 1], verts[vc * 3 + 2] };


            if (!RecastUtility.Left(verta, vertb, vertc))
            {
                return -1;
            }

            va = polys[pa + ea];
            vb = polys[pa + (ea + 1) % na];

            int dx = (int)verts[va * 3 + 0] - (int)verts[vb * 3 + 0];
            int dy = (int)verts[va * 3 + 2] - (int)verts[vb * 3 + 2];

            return dx * dx + dy * dy;

        }


        private static void MergePolyVerts(int pa, int pb, int ea, int eb, int[] tmp, int[] polys)
        {

            int na = CountPolyVerts(pa, polys);
            int nb = CountPolyVerts(pb, polys);


            Array.Fill(tmp, 0xff);
            int n = 0;

            for (int i = 0; i < na - 1; ++i)
            {
                tmp[n++] = polys[pa + (ea + 1 + i) % na];
            }

            for (int i = 0; i < nb - 1; ++i)
            {
                tmp[n++] = polys[pb + (eb + 1 + i) % nb];
            }

            for (int i = 0; i < RecastConfig.MaxVertsPerPoly; ++i)
            {
                polys[pa + i] = tmp[i];
            }
        }

        /// 构建边信息，每个边与哪个poly相邻
        private static bool BuildMeshAdjacency(int[] polys, int npolys, int nverts)
        {

            int maxEdgeCount = npolys * RecastConfig.MaxVertsPerPoly;

            int[] firstEdge = new int[nverts];
            int[] nextEdge = new int[maxEdgeCount];

            int edgeCount = 0;
            RcEdge[] edges = new RcEdge[maxEdgeCount];

            for (int i = 0; i < nverts; i++)
            {
                firstEdge[i] = RecastConfig.RC_MESH_NULL_IDX;
            }

            for (int i = 0; i < npolys; ++i)
            {
                int t = i * RecastConfig.MaxVertsPerPoly * 2;
                for (int j = 0; j < RecastConfig.MaxVertsPerPoly; ++j)
                {
                    if (polys[t + j] == RecastConfig.RC_MESH_NULL_IDX)
                    {
                        break;
                    }
                    int v0 = polys[t + j];
                    int v1 = (j + 1 >= RecastConfig.MaxVertsPerPoly || polys[t + j + 1] == RecastConfig.RC_MESH_NULL_IDX) ? polys[t] : polys[t + j + 1];
                    // 统一按照v0v1向量生成边
                    if (v0 < v1)
                    {

                        edges[edgeCount] = new RcEdge(v0, v1, i, i, j, 0);

                        //以v0位出发点的边可能有两条，所以先把之前的边的索引放入nextEdge，然后把新的放入firstEdge
                        nextEdge[edgeCount] = firstEdge[v0];
                        firstEdge[v0] = edgeCount;
                        edgeCount++;
                    }

                }
            }

            for (int i = 0; i < npolys; ++i)
            {
                int t = i * RecastConfig.MaxVertsPerPoly * 2;
                for (int j = 0; j < RecastConfig.MaxVertsPerPoly; ++j)
                {
                    if (polys[t + j] == RecastConfig.RC_MESH_NULL_IDX)
                    {
                        break;
                    }
                    int v0 = polys[t + j];
                    int v1 = (j + 1 >= RecastConfig.MaxVertsPerPoly || polys[t + j + 1] == RecastConfig.RC_MESH_NULL_IDX) ? polys[t] : polys[t + j + 1];

                    // 如果是v1v0向量的边，那一定是另个poly也有v0v1组成的边，那么这个索引i代表的poly就是上面遍历（生成edge）中i代表的poly的邻居
                    if (v0 > v1)
                    {
                        for (int e = firstEdge[v1]; e != RecastConfig.RC_MESH_NULL_IDX; e = nextEdge[e])
                        {
                            RcEdge edge = edges[e];
                            if (edge.vert[1] == v0 && edge.poly[0] == edge.poly[1])
                            {
                                edge.poly[1] = i;
                                edge.polyEdge[1] = j;
                                break;
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < edgeCount; ++i)
            {
                RcEdge e = edges[i];
                if (e.poly[0] != e.poly[1])
                {
                    // 记录每个vertex连接哪个poly
                    // 应该是边连接poly，为什么用vertex，因为vertex代表了该vertex和和下一个vertex之间的边
                    int p0Index = e.poly[0] * RecastConfig.MaxVertsPerPoly * 2;
                    int p1Index = e.poly[1] * RecastConfig.MaxVertsPerPoly * 2;
                    polys[p0Index + RecastConfig.MaxVertsPerPoly + e.polyEdge[0]] = e.poly[1];
                    polys[p1Index + RecastConfig.MaxVertsPerPoly + e.polyEdge[1]] = e.poly[0];
                }
            }


            return true;
        }

        private static int AddVertex(int x, int y, int z, int[] firstVert, int[] nextVert, RcPolyMesh polyMesh)
        {

            int[] verts = polyMesh.verts;
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

            i = polyMesh.numVerts;
            polyMesh.numVerts++;

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

                        int p0Index = (indices[i] & RecastConfig.RC_INDICE_MASK) * 4;
                        int[] p0 = { verts[p0Index], verts[p0Index + 1], verts[p0Index + 2] };

                        int p2Index = (indices[RecastUtility.Next(i1, n)] & RecastConfig.RC_INDICE_MASK) * 4;
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
                            int p0Index = (indices[i] & RecastConfig.RC_INDICE_MASK) * 4;
                            int[] p0 = { verts[p0Index], verts[p0Index + 1], verts[p0Index + 2] };

                            int p2Index = (indices[RecastUtility.Next(i1, n)] & RecastConfig.RC_INDICE_MASK) * 4;
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
                        //简化太严重，无法分割三角形
                        return -ntris;
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

            int pjIndex = (indices[j] & RecastConfig.RC_INDICE_MASK) * 4;
            int[] pj = { verts[pjIndex], verts[pjIndex + 1], verts[pjIndex + 2] };

            int piIndex = (indices[i] & RecastConfig.RC_INDICE_MASK) * 4;
            int[] pi = { verts[piIndex], verts[piIndex + 1], verts[piIndex + 2] };

            int pi1Index = (indices[RecastUtility.Next(i, n)] & RecastConfig.RC_INDICE_MASK) * 4;
            int[] pi1 = { verts[pi1Index], verts[pi1Index + 1], verts[pi1Index + 2] };

            int pin1Index = (indices[RecastUtility.Prev(i, n)] & RecastConfig.RC_INDICE_MASK) * 4;
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

        //获取索引为p的多边形的顶点数
        private static int CountPolyVerts(int p, int[] polys)
        {
            for (int i = 0; i < RecastConfig.MaxVertsPerPoly; ++i)
                if (polys[p + i] == RecastConfig.RC_MESH_NULL_IDX)
                    return i;
            return RecastConfig.MaxVertsPerPoly;
        }

        private static void getHeightData(RcCompactHeightfield chf, int[] poly, int numPloys, int[] verts, RcHeightPatch hp)
        {

        }
    }
}

