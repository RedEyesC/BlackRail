
using System;

namespace GameEditor.DetourEditor
{

    internal class DetourNavMesh
    {

        private readonly float[] halfExtents = { 2, 4, 2 };
        private readonly int batchSize = 32;

        private DtNodeQueue _openList;
        private DtNodePool _nodePool;
        public DtNavData navData;

        public void Init(DtNavData param)
        {
            navData = param;

            int maxNodes = param.nodesNum;
            if (_nodePool == null || _nodePool.GetMaxNodes() < maxNodes)
            {
                _nodePool = new DtNodePool(maxNodes, DetourUtility.DtNextPow2(maxNodes / 4));
            }
            else
            {
                _nodePool.Clear();
            }

            if (_openList == null || _openList.GetCapacity() < maxNodes)
            {
                _openList = new DtNodeQueue(maxNodes);
            }
            else
            {
                _openList.Clear();
            }
        }

        public void SearchPath(double startX, double startY, double startZ, double endX, double endY, double endZ)
        {
            SearchPath((float)startX, (float)startY, (float)startZ, (float)endX, (float)endY, (float)endZ);
        }


        public void SearchPath(float startX, float startY, float startZ, float endX, float endY, float endZ)
        {
            float[] startPos = { startX, startY, startZ };
            float[] endPos = { endX, endY, endZ };

            int startRef = -1;
            int endRef = -1;
            float[] newStarPos = new float[3];
            float[] newEndPos = new float[3];
            FindNearestPoly(startPos, newStarPos, ref startRef);
            FindNearestPoly(endPos, newEndPos, ref endRef);

            if (startRef < 0 || endRef < 0)
            {
                return;
            }

            int[] path = FindPath(startRef, endRef, newStarPos, newEndPos);

        }

        public void FindNearestPoly(float[] pos, float[] nearestPt, ref int nearestRef)
        {

            int[] polyRefs = new int[batchSize];

            float[] qmin = new float[3];
            float[] qmax = new float[3];

            DetourUtility.DtVsub(qmin, pos, halfExtents);
            DetourUtility.DtVadd(qmax, pos, halfExtents);

            int[] bmin = new int[3];
            int[] bmax = new int[3];

            float[] tbmin = navData.bmin;
            float[] tbmax = navData.bmax;


            //把世界空间的坐标转换为navmesh网格所在坐标系
            float minx = Math.Clamp(qmin[0], tbmin[0], tbmax[0]) - tbmin[0];
            float miny = Math.Clamp(qmin[1], tbmin[1], tbmax[1]) - tbmin[1];
            float minz = Math.Clamp(qmin[2], tbmin[2], tbmax[2]) - tbmin[2];
            float maxx = Math.Clamp(qmax[0], tbmin[0], tbmax[0]) - tbmin[0];
            float maxy = Math.Clamp(qmax[1], tbmin[1], tbmax[1]) - tbmin[1];
            float maxz = Math.Clamp(qmax[2], tbmin[2], tbmax[2]) - tbmin[2];

            bmin[0] = (int)(navData.quantFactor * minx) & 0xfffe;
            bmin[1] = (int)(navData.quantFactor * miny) & 0xfffe;
            bmin[2] = (int)(navData.quantFactor * minz) & 0xfffe;
            bmax[0] = (int)(navData.quantFactor * maxx + 1) | 1;
            bmax[1] = (int)(navData.quantFactor * maxy + 1) | 1;
            bmax[2] = (int)(navData.quantFactor * maxz + 1) | 1;

            int cur = 0;
            int end = navData.nodesNum;

            QueryParams queryParams = new QueryParams();

            int n = 0;

            while (cur < end)
            {
                DtBVNode node = navData.treeNodes[cur];
                bool isLeaf = node.i >= 0;
                bool overlap = DtOverlapQuantBounds(bmin, bmax, node.bmin, node.bmax);

                if (isLeaf && overlap)
                {

                    polyRefs[n] = node.i;

                    if (n == batchSize - 1)
                    {
                        QueryPolygons(polyRefs, batchSize, pos, queryParams);
                        n = 0;
                    }
                    else
                    {
                        n++;
                    }
                }

                if (overlap || isLeaf)
                {
                    ++cur;
                }
                else
                {
                    cur -= node.i;
                }
            }

            if (n > 0)
            {
                QueryPolygons(polyRefs, n, pos, queryParams);
            }


            nearestRef = queryParams.nearestRef;

            DetourUtility.DtVcopy(nearestPt, queryParams.nearestPoint);

        }

        private void QueryPolygons(int[] polyRefs, int count, float[] pos, QueryParams queryParams)
        {
            for (int i = 0; i < count; ++i)
            {
                int polyRef = polyRefs[i];
                float[] closestPtPoly = new float[3];
                float[] diff = new float[3];
                bool posOverPoly = false;
                float d;

                ClosestPointOnPoly(polyRef, pos, closestPtPoly, ref posOverPoly);

                DetourUtility.DtVsub(diff, pos, closestPtPoly);
                if (posOverPoly)
                {
                    d = Math.Abs(diff[1]) - navData.walkableClimb;
                    d = d > 0 ? d * d : 0;
                }
                else
                {
                    d = DetourUtility.DtVlenSqr(diff);
                }

                if (d < queryParams.nearestDistanceSqr)
                {
                    DetourUtility.DtVcopy(queryParams.nearestPoint, closestPtPoly);

                    queryParams.nearestDistanceSqr = d;
                    queryParams.nearestRef = polyRef;
                    queryParams.overPoly = posOverPoly;
                }
            }
        }

        private static bool DtOverlapQuantBounds(int[] amin, int[] amax, int[] bmin, int[] bmax)
        {

            bool overlap = true;

            overlap = (amin[0] > bmax[0] || amax[0] < bmin[0]) ? false : overlap;
            overlap = (amin[1] > bmax[1] || amax[1] < bmin[1]) ? false : overlap;
            overlap = (amin[2] > bmax[2] || amax[2] < bmin[2]) ? false : overlap;
            return overlap;
        }

        private void ClosestPointOnPoly(int dtPolyRef, float[] pos, float[] closest, ref bool posOverPoly)
        {

            DetourUtility.DtVcopy(closest, pos);

            if (GetPolyHeight(dtPolyRef, pos, ref closest[1]))
            {
                posOverPoly = true;
                return;
            }

            posOverPoly = false;

            ClosestPointOnDetailEdges(dtPolyRef, pos, closest);
        }

        private bool GetPolyHeight(int poly, float[] pos, ref float height)
        {


            float[] verts = new float[DetourConfig.MaxVertsPerPoly * 3];
            DtPoly navPoly = navData.navPolys[poly];
            int nv = navPoly.vertCount;

            for (int i = 0; i < nv; ++i)
            {
                Array.Copy(navData.verts, navPoly.verts[i] * 3, verts, i * 3, 3);
            }

            if(DetourUtility.DtPointInPolygon(pos, verts, nv))
            {
                return false;
            }

            int startVert = navData.detailMeshes[poly * 4 + 0];
            int startTri = navData.detailMeshes[poly * 4 + 2];
            int ntris = navData.detailMeshes[poly * 4 + 3];


            float[][] v = new float[3][];

            v[0] = new float[3];
            v[1] = new float[3];
            v[2] = new float[3];

            for (int j = 0; j < ntris; ++j)
            {

                for (int k = 0; k < 3; k++)
                {

                    int vertIndex = navData.detailTris[(startTri + j) * 4 + k];

                    float cellX = navData.detailVerts[(startVert + vertIndex) * 3];
                    float cellY = navData.detailVerts[(startVert + vertIndex) * 3 + 1];
                    float cellZ = navData.detailVerts[(startVert + vertIndex) * 3 + 2];

                    v[k][0] = cellX;
                    v[k][1] = cellY;
                    v[k][2] = cellZ;

                }
                if (DetourUtility.DtClosestHeightPointTriangle(pos, v[0], v[1], v[2], ref height))
                {
                    return true;
                }
            }

            float[] closest = new float[3];
            ClosestPointOnDetailEdges(poly, pos, closest);
            height = closest[1];
            return true;
        }

        //获取Point最接近的边缘上的位置
        private void ClosestPointOnDetailEdges(int poly, float[] pos, float[] closest)
        {

            float dmin = float.MaxValue;
            float tmin = 0;
            float[] pmin = new float[3];
            float[] pmax = new float[3];

            int startVert = navData.detailMeshes[poly * 4 + 0];
            int startTri = navData.detailMeshes[poly * 4 + 2];
            int ntris = navData.detailMeshes[poly * 4 + 3];

            float[][] v = new float[3][];
            v[0] = new float[3];
            v[1] = new float[3];
            v[2] = new float[3];

            for (int i = 0; i < ntris; ++i)
            {

                for (int k = 0; k < 3; k++)
                {

                    int vertIndex = navData.detailTris[(startTri + i) * 4 + k];

                    float cellX = navData.detailVerts[(startVert + vertIndex) * 3];
                    float cellY = navData.detailVerts[(startVert + vertIndex) * 3 + 1];
                    float cellZ = navData.detailVerts[(startVert + vertIndex) * 3 + 2];

                    v[k][0] = cellX;
                    v[k][1] = cellY;
                    v[k][2] = cellZ;

                }


                for (int k = 0, j = 2; k < 3; j = k++)
                {

                    float t = 0;
                    float d = DetourUtility.DtDistancePtSegSqr2D(pos, v[j], v[k], ref t);
                    if (d < dmin)
                    {
                        dmin = d;
                        tmin = t;
                        pmin = v[j];
                        pmax = v[k];
                    }

                }

            }

            DetourUtility.DtVlerp(closest, pmin, pmax, tmin);
        }

        private int[] FindPath(int startRef, int endRef, float[] startPos, float[] endPos)
        {

            if (startRef == endRef)
            {
                int[] path = { startRef };
                return path;
            }

            _openList.Clear();
            _nodePool.Clear();

            DtNode startNode = _nodePool.GetNode(startRef);
            DetourUtility.DtVcopy(startNode.pos, startPos);
            startNode.pidx = 0;
            startNode.cost = 0;
            startNode.total = DetourUtility.DtVdist(startPos, endPos) * DetourConfig.H_SCALE;
            startNode.id = startRef;
            startNode.flags = (int)DtNodeFlags.DT_NODE_OPEN;
            _openList.Push(startNode);

            DtNode lastBestNode = startNode;
            float lastBestNodeCost = startNode.total;


            while (!_openList.Empty())
            {
                DtNode bestNode = _openList.Pop();


                bestNode.flags &= ~(int)DtNodeFlags.DT_NODE_OPEN; //去除open的标志位
                bestNode.flags |= (int)DtNodeFlags.DT_NODE_CLOSED; //添加closed的标志位

                if (bestNode.id == endRef)
                {
                    lastBestNode = bestNode;
                    break;
                }

                int bestRef = bestNode.id;
                DtPoly bestPoly = navData.navPolys[bestRef];

                int parentRef = 0;
                if (bestNode.pidx != 0)
                    parentRef = _nodePool.GetNodeAtIdx(bestNode.pidx).id;


                for (int i = bestPoly.firstLink; i != DetourConfig.DT_NULL_LINK; i = navData.links[i].next)
                {
                    int neighbourRef = navData.links[i].refId;

                    if (neighbourRef == 0 || neighbourRef == parentRef)
                        continue;

                    DtPoly neighbourPoly = navData.navPolys[neighbourRef];

                    DtNode neighbourNode = _nodePool.GetNode(neighbourRef);
                    if (neighbourNode == null)
                    {
                        continue;
                    }

                    if (neighbourNode.flags == 0)
                    {
                        GetEdgeMidPoint(bestRef, bestPoly, neighbourRef, neighbourPoly, neighbourNode.pos);
                    }

                    float cost;
                    float heuristic;

                    if (neighbourRef == endRef)
                    {
                        float curCost = GetCost(bestNode.pos, neighbourNode.pos);
                        float endCost = GetCost(neighbourNode.pos, endPos);
                        cost = bestNode.cost + curCost + endCost;
                        heuristic = 0;
                    }
                    else
                    {
                        float curCost = GetCost(bestNode.pos, neighbourNode.pos);
                        cost = bestNode.cost + curCost;
                        heuristic = DetourUtility.DtVdist(neighbourNode.pos, endPos) * DetourConfig.H_SCALE;
                    }

                    float total = cost + heuristic;


                    if ((neighbourNode.flags & (int)DtNodeFlags.DT_NODE_OPEN) > 0 && total >= neighbourNode.total)
                        continue;

                    if ((neighbourNode.flags & (int)DtNodeFlags.DT_NODE_CLOSED) > 0 && total >= neighbourNode.total)
                        continue;

                    neighbourNode.pidx = _nodePool.GetNodeIdx(bestNode);
                    neighbourNode.id = neighbourRef;
                    neighbourNode.flags &= ~(int)DtNodeFlags.DT_NODE_CLOSED; //去除closed的标志位
                    neighbourNode.cost = cost;
                    neighbourNode.total = total;

                    if ((neighbourNode.flags & (int)DtNodeFlags.DT_NODE_OPEN) > 0)
                    {
                        _openList.Modify(neighbourNode);
                    }
                    else
                    {
                        neighbourNode.flags |= (int)DtNodeFlags.DT_NODE_OPEN;
                        _openList.Push(neighbourNode);
                    }

                    if (heuristic < lastBestNodeCost)
                    {
                        lastBestNodeCost = heuristic;
                        lastBestNode = neighbourNode;
                    }
                }
            }

            return GetPathToNode(lastBestNode);
        }

        public void GetEdgeMidPoint(int from, DtPoly fromPoly, int to, DtPoly toPoly, float[] mid)
        {
            float[] left = new float[3];
            float[] right = new float[3];
            GetPortalPoints(from, fromPoly, to, toPoly, left, right);
            mid[0] = (left[0] + right[0]) * 0.5f;
            mid[1] = (left[1] + right[1]) * 0.5f;
            mid[2] = (left[2] + right[2]) * 0.5f;
        }


        public void GetPortalPoints(int from, DtPoly fromPoly, int to, DtPoly toPoly, float[] left, float[] right)
        {
            int edge = -1;
            for (int i = fromPoly.firstLink; i != DetourConfig.DT_NULL_LINK; i = navData.links[i].next)
            {
                if (navData.links[i].refId == to)
                {
                    edge = navData.links[i].edge;
                    break;
                }
            }

            if (edge < 0)
            {
                return;
            }

            int v0 = fromPoly.verts[edge];
            int v1 = fromPoly.verts[(edge + 1) % fromPoly.vertCount];

            Array.Copy(navData.navVerts, v0 * 3, left, 0, 3);
            Array.Copy(navData.navVerts, v1 * 3, right, 0, 3);
        }

        public float GetCost(float[] pa, float[] pb)
        {
            return DetourUtility.DtVdist(pa, pb);
        }

        public int[] GetPathToNode(DtNode endNode)
        {

            DtNode curNode = endNode;
            int length = 0;
            while (curNode != null)
            {
                length++;
                curNode = _nodePool.GetNodeAtIdx(curNode.pidx);
            };


            curNode = endNode;
            int writeCount = length;

            int[] path = new int[writeCount];
            for (int i = writeCount - 1; i >= 0; i--)
            {
                path[i] = curNode.id;
                curNode = _nodePool.GetNodeAtIdx(curNode.pidx);
            }

            return path;
        }
    }
}

