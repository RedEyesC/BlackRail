
using System;
using System.Collections.Generic;

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
                maxVertsPerCont = Math.Max(maxVertsPerCont, cset.conts[i].numVerts);
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

                    RecastUtility.LogWarningFormat("rcBuildPolyMesh: Bad triangulation Contour {0}", i);
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

                                int v = GetPolyMergeValue(pj, pk, pmesh.verts, polys, out int ea, out int eb); ;
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

                            int last = (npolys - 1) * RecastConfig.MaxVertsPerPoly;

                            if (pb != last)
                            {
                                for (int k = 0; k < RecastConfig.MaxVertsPerPoly; ++k)
                                {
                                    polys[pb + k] = polys[last + k];
                                }
                            }

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
                        RecastUtility.LogErrorFormat("removeVertex: Too many polygons {0} (max:{1}).", pmesh.numPolys, maxTris);
                    }
                }

            }

            if (!BuildMeshAdjacency(pmesh.polys, pmesh.numPolys, pmesh.numVerts))
            {
                RecastUtility.LogError("rcBuildPolyMesh: Adjacency failed.");
            }

        }


        public static void RcBuildPolyMeshDetail(RcPolyMesh pmesh, RcCompactHeightfield chf, RcPolyMeshDetail dmesh)
        {
            if (pmesh.numPolys == 0 || pmesh.numVerts == 0)
                return;

            float cs = pmesh.cellSize;
            float ch = pmesh.cellHeight;
            float[] orig = pmesh.minBounds;
            int heightSearchRadius = Math.Max(1, (int)Math.Ceiling(RecastConfig.MaxSimplificationError));


            RcHeightPatch hp = new RcHeightPatch();

            Stack<int> arr = new Stack<int>();
            List<int> tris = new List<int>();
            List<float> samples = new List<float>();
            List<float> edges = new List<float>();

            float[] verts = new float[RecastConfig.Detail_MAX_VERTS * 3];

            int[] bounds = new int[pmesh.numPolys * 4];
            float[] poly = new float[pmesh.numPolys * 3];

            int nPolyVerts = 0;
            int maxhw = 0;
            int maxhh = 0;

            //获取每个多边形的xy范围
            for (int i = 0; i < pmesh.numPolys; ++i)
            {
                int pIndex = i * RecastConfig.MaxVertsPerPoly * 2;

                int xmin = chf.width;
                int xmax = 0;
                int ymin = chf.height;
                int ymax = 0;

                for (int j = 0; j < RecastConfig.MaxVertsPerPoly; ++j)
                {
                    if (pmesh.polys[pIndex] == RecastConfig.RC_MESH_NULL_IDX)
                    {
                        break;
                    }

                    int vIndex = pmesh.polys[pIndex] * 3;
                    xmin = Math.Min(xmin, pmesh.verts[vIndex]);
                    xmax = Math.Max(xmax, pmesh.verts[vIndex]);
                    ymin = Math.Min(ymin, pmesh.verts[vIndex + 2]);
                    ymax = Math.Max(ymax, pmesh.verts[vIndex + 2]);
                    nPolyVerts++;
                }
                xmin = Math.Max(0, xmin - 1);
                xmax = Math.Min(chf.width, xmax + 1);
                ymin = Math.Max(0, ymin - 1);
                ymax = Math.Min(chf.height, ymax + 1);
                if (xmin >= xmax || ymin >= ymax)
                {
                    continue;
                }
                maxhw = Math.Max(maxhw, xmax - xmin);
                maxhh = Math.Max(maxhh, ymax - ymin);

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
                int pIndex = i * RecastConfig.MaxVertsPerPoly * 2;

                int npoly = 0;
                for (int j = 0; j < RecastConfig.MaxVertsPerPoly; ++j)
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
                GetHeightData(chf, pIndex, pmesh.polys, npoly, pmesh.verts, hp, arr, pmesh.regs[i]);

                int nverts = 0;
                if (!BuildPolyDetail(poly, npoly, heightSearchRadius, chf, hp, verts, nverts, tris, edges, samples))
                {

                }

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
                    RecastUtility.RcSwap(ref va0, ref va1);
                }

                for (int j = 0; j < nb; ++j)
                {
                    int vb0 = polys[pb + j];
                    int vb1 = polys[pb + (j + 1) % nb];
                    if (vb0 > vb1)
                    {
                        RecastUtility.RcSwap(ref vb0, ref vb1);
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


            Array.Fill(tmp, RecastConfig.RC_MESH_NULL_IDX);
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

        static void Push3(Stack<int> queue, int v1, int v2, int v3)
        {
            queue.Push(v1);
            queue.Push(v2);
            queue.Push(v3);
        }

        private static void GetHeightData(RcCompactHeightfield chf, int pIndex, int[] polys, int numPloys, int[] verts,
            RcHeightPatch hp, Stack<int> queue, int region)
        {

            queue.Clear();

            Array.Fill(hp.data, 0xff);

            bool empty = true;

            if (region != RecastConfig.RC_MULTIPLE_REGS)
            {

                // 遍历poly包围盒内所有的点，如果同region则设置高度，并把region边界保存到queue
                for (int hy = 0; hy < hp.height; hy++)
                {
                    int y = hp.ymin + hy;
                    for (int hx = 0; hx < hp.width; hx++)
                    {
                        int x = hp.xmin + hx;

                        CompactCell c = chf.cells[x + y * chf.width];
                        for (int i = c.index, ni = c.index + c.count; i < ni; ++i)
                        {
                            CompactSpan s = chf.spans[i];
                            if (s.reg == region)
                            {

                                hp.data[hx + hy * hp.width] = s.y;
                                empty = false;


                                bool border = false;
                                for (int dir = 0; dir < 4; ++dir)
                                {
                                    if (RecastUtility.RcGetCon(s, dir) != RecastConfig.RC_NOT_CONNECTED)
                                    {
                                        int ax = x + RecastUtility.RcGetDirOffsetX(dir);
                                        int ay = y + RecastUtility.RcGetDirOffsetY(dir);
                                        int ai = chf.cells[ax + ay * chf.width].index + RecastUtility.RcGetCon(s, dir);

                                        CompactSpan a2 = chf.spans[ai];
                                        if (a2.reg != region)
                                        {
                                            border = true;
                                            break;
                                        }
                                    }
                                }
                                //把region边界保存到queue
                                if (border)
                                {
                                    Push3(queue, x, y, i);
                                }

                                break;
                            }
                        }
                    }
                }
            }

            //如果为空，多边形可能是在一个区域的重叠多边形，
            if (empty)
            {
                SeedArrayWithPolyCenter(chf, pIndex, polys, numPloys, verts, hp, queue);
            }


            //存在多层span ，region border上的span是为了确定在哪一层，以这一层的span再进行泛洪
            while (3 < queue.Count)
            {
                int cx = queue.Pop();
                int cy = queue.Pop();
                int ci = queue.Pop();

                CompactSpan cs = chf.spans[ci];
                for (int dir = 0; dir < 4; ++dir)
                {
                    if (RecastUtility.RcGetCon(cs, dir) == RecastConfig.RC_NOT_CONNECTED)
                    {
                        continue;
                    }

                    int ax = cx + RecastUtility.RcGetDirOffsetX(dir);
                    int ay = cy + RecastUtility.RcGetDirOffsetY(dir);
                    int hx = ax - hp.xmin;
                    int hy = ay - hp.ymin;


                    if (hx >= hp.width || hy >= hp.height)
                    {
                        continue;
                    }


                    if (hp.data[hx + hy * hp.width] != RecastConfig.RC_UNSET_HEIGHT)
                    {
                        continue;
                    }

                    int ai = chf.cells[ax + ay * chf.width].index + RecastUtility.RcGetCon(cs, dir);
                    CompactSpan cs2 = chf.spans[ai];
                    hp.data[hx + hy * hp.width] = cs2.y;
                    Push3(queue, ax, ay, ai);
                }
            }
        }

        private static void SeedArrayWithPolyCenter(RcCompactHeightfield chf, int pIndex, int[] polys, int npoly, int[] verts,
            RcHeightPatch hp, Stack<int> queue)
        {

            int[] offset = { 0, 0, -1, -1, 0, -1, 1, -1, 1, 0, 1, 1, 0, 1, -1, 1, -1, 0, };

            int startCellX = 0, startCellY = 0, startSpanIndex = -1;
            int dmin = RecastConfig.RC_UNSET_HEIGHT;

            //遍历所有多边形，偏移多边形顶点坐标
            for (int j = 0; j < npoly && dmin > 0; ++j)
            {
                for (int k = 0; k < 9 && dmin > 0; ++k)
                {
                    int vIndex = polys[j * RecastConfig.MaxVertsPerPoly * 2] * 3;
                    int ax = verts[vIndex + 0] + offset[k * 2 + 0];
                    int ay = verts[vIndex + 1];
                    int az = verts[vIndex + 2] + offset[k * 2 + 1];

                    //判断偏移后多边形是否符合hp范围
                    if (ax < hp.xmin || ax >= hp.xmin + hp.width || az < hp.ymin || az >= hp.ymin + hp.height)
                    {
                        continue;
                    }

                    CompactCell c = chf.cells[ax + az * chf.width];
                    for (int i = c.index, ni = c.index + c.count; i < ni && dmin > 0; ++i)
                    {
                        //找到和当前多边形y轴距离最近的
                        CompactSpan s = chf.spans[i];
                        int d = Math.Abs(ay - (int)s.y);
                        if (d < dmin)
                        {
                            startCellX = ax;
                            startCellY = az;
                            startSpanIndex = i;
                            dmin = d;
                        }
                    }
                }
            }

            if (startSpanIndex == -1)
            {
                RecastUtility.LogError("Cant find CompactSpan");
                return;
            }


            //找到多边形所有多边形的中心点 pcx，pcy
            int pcx = 0, pcy = 0;
            for (int j = 0; j < npoly; ++j)
            {
                int vIndex = polys[pIndex] * 3;
                pcx += verts[vIndex + 0];
                pcy += verts[vIndex + 2];
            }
            pcx /= npoly;
            pcy /= npoly;


            queue.Clear();
            queue.Push(startCellX);
            queue.Push(startCellY);
            queue.Push(startSpanIndex);

            int[] dirs = { 0, 1, 2, 3 };

            Array.Fill(hp.data, 0);

            int cx = -1, cy = -1, ci = -1;

            //将queue中的点向pcx，pcy移动
            while (true)
            {
                if (queue.Count < 3)
                {
                    RecastUtility.LogWarning("Walk towards polygon center failed to reach center");
                    break;
                }

                ci = queue.Pop();
                cy = queue.Pop();
                cx = queue.Pop();

                if (cx == pcx && cy == pcy)
                {
                    break;
                }

                //判断一下更偏向哪个方向
                int directDir;
                if (cx == pcx)
                    directDir = RecastUtility.RcGetDirForOffset(0, pcy > cy ? 1 : -1);
                else
                    directDir = RecastUtility.RcGetDirForOffset(pcx > cx ? 1 : -1, 0);

                //方向放在最后
                RecastUtility.RcSwap(ref dirs[directDir], ref dirs[3]);

                CompactSpan cs = chf.spans[ci];
                for (int i = 0; i < 4; i++)
                {
                    int dir = dirs[i];
                    if (RecastUtility.RcGetCon(cs, dir) == RecastConfig.RC_NOT_CONNECTED)
                        continue;

                    int newX = cx + RecastUtility.RcGetDirOffsetX(dir);
                    int newY = cy + RecastUtility.RcGetDirOffsetY(dir);

                    int hpx = newX - hp.xmin;
                    int hpy = newY - hp.ymin;
                    if (hpx < 0 || hpx >= hp.width || hpy < 0 || hpy >= hp.height)
                        continue;

                    if (hp.data[hpx + hpy * hp.width] != 0)
                        continue;

                    hp.data[hpx + hpy * hp.width] = 1;
                    queue.Push(newX);
                    queue.Push(newY);
                    queue.Push(chf.cells[newX + newY * chf.width].index + RecastUtility.RcGetCon(cs, dir));
                }

                RecastUtility.RcSwap(ref dirs[directDir], ref dirs[3]);
            }

            queue.Clear();

            queue.Push(cx);
            queue.Push(cy);
            queue.Push(ci);

            Array.Fill(hp.data, 0xff, 0, hp.width * hp.height);
            CompactSpan cs2 = chf.spans[ci];
            hp.data[cx - hp.xmin + (cy - hp.ymin) * hp.width] = cs2.y;
        }


        private static float getJitterX(int i)
        {
            return (((i * 0x8da6b343) & 0xffff) / 65535.0f * 2.0f) - 1.0f;
        }

        private static float getJitterY(int i)
        {
            return (((i * 0xd8163841) & 0xffff) / 65535.0f * 2.0f) - 1.0f;
        }


        private static bool BuildPolyDetail(float[] poly, int npoly, int heightSearchRadius, RcCompactHeightfield chf,
                                RcHeightPatch hp, float[] verts, int nverts,
                                List<int> tris, List<float> edges, List<float> samples)
        {

            float sampleDist = RecastConfig.DetailSampleDist < 0.9f ? 0 : RecastConfig.CellSize * RecastConfig.DetailSampleDist;
            float sampleMaxError = RecastConfig.CellHeight * RecastConfig.DetailSampleMaxError;


            float[] edge = new float[(RecastConfig.Detail_MAX_VERTS_PER_EDGE + 1) * 3];
            int[] hull = new int[RecastConfig.Detail_MAX_VERTS]; //存放新增加的顶点
            int nhull = 0;

            nverts = npoly;

            for (int i = 0; i < npoly; ++i)
            {
                verts[i * 3] = poly[i * 3];
                verts[i * 3 + 1] = poly[i * 3 + 1];
                verts[i * 3 + 2] = poly[i * 3 + 2];
            }


            Array.Fill(edge, 0);

            tris.Clear();

            float cs = chf.cellSize;
            float ics = 1.0f / cs;


            float minExtent = PolyMinExtent(verts, nverts);

            if (sampleDist > 0)
            {
                for (int i = 0, j = npoly - 1; i < npoly; j = i++)
                {
                    int vj = j * 3;
                    int vi = i * 3;

                    bool swapped = false;

                    // x,z数值比较大的作为vi
                    if (Math.Abs(poly[vj] - poly[vi]) < 1e-6f)
                    {
                        if (poly[vj + 2] > poly[vi + 2])
                        {
                            RecastUtility.RcSwap(ref vj, ref vi);
                            swapped = true;
                        }
                    }
                    else
                    {
                        if (poly[vj] > poly[vi])
                        {
                            RecastUtility.RcSwap(ref vj, ref vi);
                            swapped = true;
                        }
                    }

                    float dx = poly[vi] - poly[vj];
                    float dy = poly[vi + 1] - poly[vj + 1];
                    float dz = poly[vi + 2] - poly[vj + 2];
                    float d = (float)Math.Sqrt(dx * dx + dz * dz);
                    int nn = 1 + (int)Math.Floor(d / sampleDist);

                    if (nn >= RecastConfig.Detail_MAX_VERTS_PER_EDGE)
                    {
                        nn = RecastConfig.Detail_MAX_VERTS_PER_EDGE - 1;
                    }
                    if (nverts + nn >= RecastConfig.Detail_MAX_VERTS)
                    {
                        nn = RecastConfig.Detail_MAX_VERTS - 1 - nverts;
                    }

                    //分割边缘
                    for (int k = 0; k <= nn; ++k)
                    {
                        float u = k / nn;
                        int pos = k * 3;
                        edge[pos] = poly[vj] + dx * u;
                        edge[pos + 1] = poly[vj + 1] + dy * u;
                        edge[pos + 2] = poly[vj + 2] + dz * u;
                        edge[pos + 1] = GetHeight(edge[pos], edge[pos + 1], edge[pos + 2], ics, chf.cellHeight, heightSearchRadius, hp) * chf.cellHeight;
                    }

                    int[] idx = new int[RecastConfig.Detail_MAX_VERTS_PER_EDGE];
                    idx[0] = 0;
                    idx[1] = nn;

                    int nidx = 2;
                    for (int k = 0; k < nidx - 1;)
                    {
                        int a = idx[k];
                        int b = idx[k + 1];

                        float vax = edge[a * 3];
                        float vay = edge[a * 3 + 1];
                        float vaz = edge[a * 3 + 2];

                        float vbx = edge[b * 3];
                        float vby = edge[b * 3 + 1];
                        float vbz = edge[b * 3 + 2];


                        float maxd = 0;
                        int maxi = -1;
                        for (int m = a + 1; m < b; ++m)
                        {
                            float dev = RecastUtility.DistancePtSeg(edge[m * 3], edge[m * 3 + 1], edge[m * 3 + 2], vax, vay, vaz, vbx, vby, vbz);
                            if (dev > maxd)
                            {
                                maxd = dev;
                                maxi = m;
                            }
                        }
                        //选择距离最远的点并且大于sampleMaxError的点作为简化点，并插入idx
                        if (maxi != -1 && maxd > Math.Sqrt(sampleMaxError))
                        {
                            for (int m = nidx; m > k; --m)
                            {
                                idx[m] = idx[m - 1];
                            }

                            idx[k + 1] = maxi;
                            nidx++;
                        }
                        else
                        {
                            ++k;
                        }
                    }

                    hull[nhull++] = j;

                    if (swapped)
                    {
                        for (int k = nidx - 2; k > 0; --k)
                        {
                            verts[nverts * 3] = edge[idx[k] * 3];
                            verts[nverts * 3 + 1] = edge[idx[k] * 3 + 1];
                            verts[nverts * 3 + 2] = edge[idx[k] * 3 + 2];
                            hull[nhull++] = nverts;
                            nverts++;
                        }
                    }
                    else
                    {
                        for (int k = 1; k < nidx - 1; ++k)
                        {
                            verts[nverts * 3] = edge[idx[k] * 3];
                            verts[nverts * 3 + 1] = edge[idx[k] * 3 + 1];
                            verts[nverts * 3 + 2] = edge[idx[k] * 3 + 2];

                            hull[nhull++] = nverts;
                            nverts++;
                        }
                    }
                }
            }

            //根据新增的点构建三角形
            TriangulateHull(verts, nhull, hull, npoly, tris);

            //多边形过于狭窄不再添加内部点
            if (minExtent < sampleDist * 2)
            {
                return true;
            }

            if (tris.Count == 0)
            {
                RecastUtility.LogWarningFormat("buildPolyDetail: Could not triangulate polygon (%{0} verts).", nverts);
                return true;
            }


            if (sampleDist > 0)
            {

                float[] bmin = new float[3] { poly[0], poly[1], poly[2] };
                float[] bmax = new float[3] { poly[0], poly[1], poly[2] };

                for (int i = 1; i < npoly; i++)
                {
                    bmin[0] = Math.Min(bmin[0], poly[npoly * 3]);
                    bmax[0] = Math.Max(bmax[0], poly[npoly * 3]);


                    bmin[1] = Math.Min(bmin[1], poly[npoly * 3 + 1]);
                    bmax[1] = Math.Max(bmax[1], poly[npoly * 3 + 1]);

                    bmin[2] = Math.Min(bmin[2], poly[npoly * 3 + 2]);
                    bmax[2] = Math.Max(bmax[2], poly[npoly * 3 + 2]);
                }

                int x0 = (int)Math.Floor(bmin[0] / sampleDist);
                int x1 = (int)Math.Ceiling(bmax[0] / sampleDist);
                int z0 = (int)Math.Floor(bmin[2] / sampleDist);
                int z1 = (int)Math.Ceiling(bmax[2] / sampleDist);

                samples.Clear();

                //在多边形内部添加点
                for (int z = z0; z < z1; ++z)
                {
                    for (int x = x0; x < x1; ++x)
                    {
                        float[] pt = new float[3];
                        pt[0] = x * sampleDist;
                        pt[1] = (bmax[1] + bmin[1]) * 0.5f;
                        pt[2] = z * sampleDist;

                        //保证新增的内部的点不会太过于靠近边缘
                        if (RecastUtility.DistToPoly(npoly, poly, pt) > -sampleDist / 2)
                        {
                            continue;
                        }

                        samples.Add(x);
                        samples.Add(GetHeight(pt[0], pt[1], pt[2], ics, chf.cellHeight, heightSearchRadius, hp));
                        samples.Add(z);
                        samples.Add(0);
                    }
                }

                int nsamples = samples.Count / 4;
                for (int iter = 0; iter < nsamples; ++iter)
                {
                    if (nverts >= RecastConfig.Detail_MAX_VERTS)
                    {
                        break;
                    }

                    float[] bestpt = { 0, 0, 0 };
                    float bestd = 0;
                    int besti = -1;
                    for (int i = 0; i < nsamples; ++i)
                    {
                        int s = i * 4;

                        if (samples[s + 3] != 0)
                        {
                            continue;
                        }

                        float[] pt = new float[3];

                        //对样本位置进行抖动处理，以消除网格结构中对称数据造成的一些不良三角测量。
                        pt[0] = samples[s] * sampleDist + getJitterX(i) * cs * 0.1f;
                        pt[1] = samples[s + 1] * chf.cellHeight;
                        pt[2] = samples[s + 2] * sampleDist + getJitterY(i) * cs * 0.1f;

                        //计算点到三角形的距离，过于近的舍弃
                        float d = DistToTriMesh(pt, verts, tris, tris.Count / 4);
                        if (d < 0)
                        {
                            continue;
                        }

                        if (d > bestd)
                        {
                            bestd = d;
                            besti = i;

                            bestpt[0] = pt[0];
                            bestpt[1] = pt[1];
                            bestpt[2] = pt[2];
                        }
                    }


                    if (bestd <= sampleMaxError || besti == -1)
                    {
                        break;
                    }

                    samples[besti * 4 + 3] = 1;

                    verts[nverts * 3] = bestpt[0];
                    verts[nverts * 3 + 1] = bestpt[1];
                    verts[nverts * 3 + 2] = bestpt[2];

                    nverts++;

                    //构建三角形.
                    edges.Clear();
                    tris.Clear();
                    DelaunayHull(nverts, verts, nhull, hull, tris, edges);

                }
            }

            return true;
        }

        /// verts中每两个点组成边，其他vert到这个边的最远距离maxEdgeDist
        /// maxEdgeDist中的最小距离为poly的最小延展minExtent
        private static float PolyMinExtent(float[] verts, int nverts)
        {

            float minDist = float.MaxValue;
            for (int i = 0; i < nverts; i++)
            {
                int ni = (i + 1) % nverts;
                float p1x = verts[i * 3];
                float p1z = verts[i * 3 + 2];

                float p2x = verts[ni * 3];
                float p2z = verts[ni * 3 + 2];

                float maxEdgeDist = 0;
                for (int j = 0; j < nverts; j++)
                {
                    if (j == i || j == ni)
                    {
                        continue;
                    }
                    float d = RecastUtility.DistancePtSeg2D(verts[j * 3], verts[j * 3 + 2], p1x, p1z, p2x, p2z);
                    maxEdgeDist = Math.Max(maxEdgeDist, d);
                }

                minDist = Math.Min(minDist, maxEdgeDist);
            }
            return (float)Math.Sqrt(minDist);
        }

        private static int GetHeight(float fx, float fy, float fz, float ics, float ch, int radius, RcHeightPatch hp)
        {
            int ix = (int)Math.Floor(fx * ics + 0.01f);
            int iz = (int)Math.Floor(fz * ics + 0.01f);
            ix = Math.Clamp(ix - hp.xmin, 0, hp.width - 1);
            iz = Math.Clamp(iz - hp.ymin, 0, hp.height - 1);
            int h = hp.data[ix + iz * hp.width];
            if (h == RecastConfig.RC_UNSET_HEIGHT)
            {

                //如果没找到数据，就去相邻的位置去获取数据
                int x = 1, z = 0, dx = 1, dz = 0;
                int maxSize = radius * 2 + 1;
                int maxIter = maxSize * maxSize - 1;

                int nextRingIterStart = 8;
                int nextRingIters = 16;

                float dmin = float.MaxValue;
                for (int i = 0; i < maxIter; i++)
                {
                    int nx = ix + x;
                    int nz = iz + z;

                    if (nx >= 0 && nz >= 0 && nx < hp.width && nz < hp.height)
                    {
                        int nh = hp.data[nx + nz * hp.width];
                        if (nh != RecastConfig.RC_UNSET_HEIGHT)
                        {
                            float d = Math.Abs(nh * ch - fy);
                            if (d < dmin)
                            {
                                h = nh;
                                dmin = d;
                            }
                        }
                    }

                    //限制了搜索范围，当在nextRingIterStart找到高度就不再继续寻找了，没有就向外再扩张寻找范围
                    if (i + 1 == nextRingIterStart)
                    {
                        if (h != RecastConfig.RC_UNSET_HEIGHT)
                            break;

                        nextRingIterStart += nextRingIters;
                        nextRingIters += 8;
                    }

                    if ((x == z) || ((x < 0) && (x == -z)) || ((x > 0) && (x == 1 - z)))
                    {
                        int tmp = dx;
                        dx = -dz;
                        dz = tmp;
                    }
                    x += dx;
                    z += dz;
                }
            }
            return h;
        }

        private static void TriangulateHull(float[] verts, int nhull, int[] hull, int nin, List<int> tris)
        {

            int start = 0, left = 1, right = nhull - 1;

            //选择三角形中周长最短的作为要裁切的三角形
            //把三角形顶点放入tris中，然后去掉耳尖顶点，留下两个耳根顶点，以这两个耳根定点为耳尖的两个三角形进行比较，周长较短的作为要裁切的三角形，重复2
            float dmin = float.MaxValue;
            for (int i = 0; i < nhull; i++)
            {
                if (hull[i] >= nin)
                {
                    continue;
                }
                int pi = RecastUtility.Prev(i, nhull);
                int ni = RecastUtility.Next(i, nhull);

                float pvx = verts[hull[pi] * 3];
                float pvz = verts[hull[pi] * 3 + 2];

                float cvx = verts[hull[i] * 3];
                float cvz = verts[hull[i] * 3 + 2];

                float nvx = verts[hull[ni] * 3];
                float nvz = verts[hull[ni] * 3 + 2];

                float d = RecastUtility.Vdist2(pvx, pvz, cvx, cvz) + RecastUtility.Vdist2(cvx, cvz, nvx, nvz) + RecastUtility.Vdist2(nvx, nvz, pvx, pvz);
                if (d < dmin)
                {
                    start = i;
                    left = ni;
                    right = pi;
                    dmin = d;
                }
            }


            tris.Add(hull[start]);
            tris.Add(hull[left]);
            tris.Add(hull[right]);
            tris.Add(0);

            while (RecastUtility.Next(left, nhull) != right)
            {
                int nleft = RecastUtility.Next(left, nhull);
                int nright = RecastUtility.Prev(right, nhull);

                float cvleftx = verts[hull[left] * 3];
                float cvleftz = verts[hull[left] * 3 + 2];

                float nvleftx = verts[hull[nleft] * 3];
                float nvleftz = verts[hull[nleft] * 3 + 2];

                float cvrightx = verts[hull[right] * 3];
                float cvrightz = verts[hull[right] * 3 + 2];

                float nvrightx = verts[hull[nright] * 3];
                float nvrightz = verts[hull[nright] * 3 + 2];

                float dleft = RecastUtility.Vdist2(cvleftx, cvleftz, nvleftx, nvleftz) + RecastUtility.Vdist2(nvleftx, nvleftz, cvrightx, cvrightz);
                float dright = RecastUtility.Vdist2(cvrightx, cvrightz, nvrightx, nvrightz) + RecastUtility.Vdist2(cvleftx, cvleftz, nvrightx, nvrightz);

                if (dleft < dright)
                {
                    tris.Add(hull[left]);
                    tris.Add(hull[nleft]);
                    tris.Add(hull[right]);
                    tris.Add(0);

                    left = nleft;
                }
                else
                {
                    tris.Add(hull[left]);
                    tris.Add(hull[nright]);
                    tris.Add(hull[right]);
                    tris.Add(0);

                    right = nright;
                }
            }
        }

        public static float DistToTriMesh(float[] p, float[] verts, List<int> tris, int ntris)
        {

            float dmin = float.MaxValue;
            for (int i = 0; i < ntris; ++i)
            {
                float[] va = new float[3];
                float[] vb = new float[3];
                float[] vc = new float[3];


                va[0] = verts[tris[i * 4 + 0] * 3];
                va[1] = verts[tris[i * 4 + 0] * 3 + 1];
                va[2] = verts[tris[i * 4 + 0] * 3 + 2];

                vb[0] = verts[tris[i * 4 + 1] * 3];
                vb[1] = verts[tris[i * 4 + 1] * 3 + 1];
                vb[2] = verts[tris[i * 4 + 1] * 3 + 2];

                vc[0] = verts[tris[i * 4 + 2] * 3];
                vc[1] = verts[tris[i * 4 + 2] * 3 + 1];
                vc[2] = verts[tris[i * 4 + 2] * 3 + 2];


                float d = RecastUtility.DistPtTri(p, va, vb, vc);
                if (d < dmin)
                {
                    dmin = d;
                }
            }
            if (dmin == float.MaxValue)
            {
                return -1;
            }
            return dmin;
        }


        private static bool OverlapEdges(float[] pts, int[] edges, int nedges, int s1, int t1)
        {
            for (int i = 0; i < nedges; ++i)
            {
                int s0 = edges[i * 4 + 0];
                int t0 = edges[i * 4 + 1];
                // Same or connected edges do not overlap.
                if (s0 == s1 || s0 == t1 || t0 == s1 || t0 == t1)
                    continue;
                if (overlapSegSeg2d(&pts[s0 * 3], &pts[t0 * 3], &pts[s1 * 3], &pts[t1 * 3]))
                    return true;
            }
            return false;
        }

        private static int overlapSegSeg2d(int p1, int p2, int p3, int p4, float[] pts)
        {
            float[] a = { pts[p1], pts[p1 + 1], pts[p1 + 2] };
            float[] b = { pts[p2], pts[p2 + 1], pts[p2 + 2] };
            float[] c = { pts[p3], pts[p3 + 1], pts[p3 + 2] };
            float[] d = { pts[p4], pts[p4 + 1], pts[p4 + 2] };

            float a1 = RecastUtility.Vcross2(a, b, d);
            float a2 = RecastUtility.Vcross2(a, b, c);
            if (a1 * a2 < 0.0f)
            {
                float a3 = RecastUtility.Vcross2(c, d, a);
                float a4 = a3 + a2 - a1;
                if (a3 * a4 < 0.0f)
                    return 1;
            }
            return 0;
        }

        private static int FindEdge(List<float> edges, int nedges, int s, int t)
        {
            for (int i = 0; i < nedges; i++)
            {
                int e = i * 4;
                if ((edges[e] == s && edges[e + 1] == t) || (edges[e] == t && edges[e + 1] == s))
                    return i;
            }
            return RecastConfig.EV_UNDEF;
        }

        private static int AddEdge(List<float> edges, int nedges, int maxEdges, int s, int t, int l, int r)
        {
            if (nedges >= maxEdges)
            {
                RecastUtility.LogErrorFormat("addEdge: Too many edges ({0}/{1}).", nedges, maxEdges);
                return RecastConfig.EV_UNDEF;
            }

            int e = FindEdge(edges, nedges, s, t);

            if (e == RecastConfig.EV_UNDEF)
            {
                int edge = nedges * 4;

                edges[edge] = s;
                edges[edge + 1] = t;
                edges[edge + 2] = l;
                edges[edge + 3] = r;

                return nedges++;
            }

            else
            {
                return RecastConfig.EV_UNDEF;
            }
        }



        private static float Vcross2(int p1, int p2, int p3, float[] pts)
        {
            float u1 = pts[p2] - pts[p1];
            float v1 = pts[p2 + 2] - pts[p1 + 2];
            float u2 = pts[p3] - pts[p1];
            float v2 = pts[p3 + 2] - pts[p1 + 2];
            return u1 * v2 - v1 * u2;
        }


        private static bool CircumCircle(int pi1, int pi2, int pi3, float[] c, float[] pts, ref float r)
        {
            float EPS = 1e-6f;
            // Calculate the circle relative to p1, to avoid some precision issues.
            float[] v1 = { 0, 0, 0 };
            float[] v2 = new float[3];
            float[] v3 = new float[3];

            float[] p1 = { pts[pi1], pts[pi1 + 1], pts[pi1 + 2] };
            float[] p2 = { pts[pi2], pts[pi2 + 1], pts[pi2 + 2] };
            float[] p3 = { pts[pi3], pts[pi3 + 1], pts[pi3 + 2] };

            RecastUtility.RcVsub(v2, p2, p1);
            RecastUtility.RcVsub(v3, p3, p1);

            float cp = RecastUtility.Vcross2(v1, v2, v3);
            if (Math.Abs(cp) > EPS)
            {
                float v1Sq = RecastUtility.Vdot2(v1, v1);
                float v2Sq = RecastUtility.Vdot2(v2, v2);
                float v3Sq = RecastUtility.Vdot2(v3, v3);

                c[0] = (v1Sq * (v2[2] - v3[2]) + v2Sq * (v3[2] - v1[2]) + v3Sq * (v1[2] - v2[2])) / (2 * cp);
                c[1] = 0;
                c[2] = (v1Sq * (v3[0] - v2[0]) + v2Sq * (v1[0] - v3[0]) + v3Sq * (v2[0] - v1[0])) / (2 * cp);

                r = RecastUtility.Vdist2(c[0], c[2], v1[0], v1[1]);
                RecastUtility.RcVadd(c, c, p1);
                return true;
            }

            RecastUtility.RcVcopy(c, p1);
            r = 0;
            return false;
        }


        private static void CompleteFacet(float[] pts, int npts, int[] edges, int nedges, int maxEdges, int nfaces, int e)
        {

            float EPS = 1e-5f;

            int index = e * 4;   //&edges[e * 4];

            // Cache s and t.
            int s, t;
            if (edges[index + 2] == RecastConfig.EV_UNDEF)
            {
                s = edges[index];
                t = edges[index + 1];
            }
            else if (edges[index + 3] == RecastConfig.EV_UNDEF)
            {
                s = edges[index + 1];
                t = edges[index];
            }
            else
            {
                // Edge already completed.
                return;
            }

            // Find best point on left of edge.
            int pt = npts;
            float[] c = { 0, 0, 0 };
            float r = -1;
            for (int u = 0; u < npts; ++u)
            {
                if (u == s || u == t) continue;
                if (Vcross2(s * 3, t * 3, u * 3, pts) > EPS)
                {
                    if (r < 0)
                    {
                        // The circle is not updated yet, do it now.
                        pt = u;
                        CircumCircle(s * 3, t * 3, u * 3, c, pts, ref r);
                        continue;
                    }

                    float d = RecastUtility.Vdist2(c[0], c[2], pts[u * 3], pts[u * 3 + 2]);
                    float tol = 0.001f;
                    if (d > r * (1 + tol))
                    {
                        // Outside current circumcircle, skip.
                        continue;
                    }
                    else if (d < r * (1 - tol))
                    {
                        // Inside safe circumcircle, update circle.
                        pt = u;
                        CircumCircle(s * 3, t * 3, u * 3, c, pts, ref r);
                    }
                    else
                    {
                        // Inside epsilon circum circle, do extra tests to make sure the edge is valid.
                        // s-u and t-u cannot overlap with s-pt nor t-pt if they exists.
                        if (OverlapEdges(pts, edges, nedges, s, u))
                            continue;
                        if (OverlapEdges(pts, edges, nedges, t, u))
                            continue;
                        // Edge is valid.
                        pt = u;
                        CircumCircle(s * 3, t * 3, u * 3, c, pts, ref r);
                    }
                }
            }

            // Add new triangle or update edge info if s-t is on hull.
            if (pt < npts)
            {
                // Update face information of edge being completed.
                updateLeftFace(&edges[e * 4], s, t, nfaces);

                // Add new edge or update face info of old edge.
                e = findEdge(edges, nedges, pt, s);
                if (e == EV_UNDEF)
                    addEdge(ctx, edges, nedges, maxEdges, pt, s, nfaces, EV_UNDEF);
                else
                    updateLeftFace(&edges[e * 4], pt, s, nfaces);

                // Add new edge or update face info of old edge.
                e = findEdge(edges, nedges, t, pt);
                if (e == EV_UNDEF)
                    AddEdge(edges, nedges, maxEdges, t, pt, nfaces, EV_UNDEF);
                else
                    updateLeftFace(&edges[e * 4], t, pt, nfaces);

                nfaces++;
            }
            else
            {
                updateLeftFace(&edges[e * 4], s, t, EV_HULL);
            }
        }



        public static void DelaunayHull(int nverts, float[] verts, int nhull, int[] hulls, List<int> tris, List<float> edges)
        {
            int nfaces = 0;
            int nedges = 0;
            int maxEdges = nverts * 10;

            for (int i = 0, j = nhull - 1; i < nhull; j = i++)
            {
                AddEdge(edges, nedges, maxEdges, hulls[j], hulls[i], RecastConfig.EV_HULL, RecastConfig.EV_UNDEF);
            }


            int currentEdge = 0;
            while (currentEdge < nedges)
            {
                if (edges[currentEdge * 4 + 2] == RecastConfig.EV_UNDEF)
                {
                    //completeFacet(ctx, pts, npts, &edges[0], nedges, maxEdges, nfaces, currentEdge);
                }

                if (edges[currentEdge * 4 + 3] == RecastConfig.EV_UNDEF)
                {
                    //completeFacet(ctx, pts, npts, &edges[0], nedges, maxEdges, nfaces, currentEdge);
                }

                currentEdge++;
            }

        }


    }
}

