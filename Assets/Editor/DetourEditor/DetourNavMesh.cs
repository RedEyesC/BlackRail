
using System;

namespace GameEditor.DetourEditor
{

    internal class DetourNavMesh
    {

        private readonly float[] halfExtents = { 2, 4, 2 };
        private readonly int batchSize = 32;

        public DtNavData navData;

        public int nodesNum;

        public void Init(DtNavData param)
        {
            navData = param;
     
        }


        public void SearchPath(float startX, float startY, float startZ, float endX, float endY, float endZ)
        {
            float[] startPos = { startX, startY, startZ };
            float[] endPos = { endX, endY, endZ };
        }

     
        public QueryParams FindNearestPoly(float[] pos)
        {

            int[] polyRefs = new int[batchSize];

            float[] pmin = new float[3];
            float[] pmax = new float[3];

            DetourUtility.DtVsub(pmin, pos, halfExtents);
            DetourUtility.DtVadd(pmax, pos, halfExtents);

            int[] bmin = new int[3];
            int[] bmax = new int[3];

            bmin[0] = (int)(navData.quantFactor * pmin[0]) & 0xfffe;
            bmin[1] = (int)(navData.quantFactor * pmin[1]) & 0xfffe;
            bmin[2] = (int)(navData.quantFactor * pmin[2]) & 0xfffe;
            bmax[0] = (int)(navData.quantFactor * pmax[0] + 1) | 1;
            bmax[1] = (int)(navData.quantFactor * pmax[1] + 1) | 1;
            bmax[2] = (int)(navData.quantFactor * pmax[2] + 1) | 1;

            int cur = 0;
            int end = nodesNum;


            QueryParams queryParams = new QueryParams();
            queryParams.nearestDistanceSqr = float.MaxValue;

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


            return queryParams;
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
                if (posOverPoly)
                {
                    posOverPoly = true;
                    return;
                }

            }

            if (posOverPoly)
            {
                posOverPoly = false;
                return;
            };

            ClosestPointOnDetailEdges(dtPolyRef, pos, closest);
        }

        private bool GetPolyHeight(int poly, float[] pos, ref float height)
        {


            int startVert = navData.detailMeshes[poly * 4 + 0];
            int startTri = navData.detailMeshes[poly * 4 + 2];
            int ntris = navData.detailMeshes[poly * 4 + 3];


            float[][] v = new float[3][];

            for (int j = 0; j < ntris; ++j)
            {

                for (int k = 0; k < 3; k++)
                {

                    int vertIndex = navData.detailTris[(startTri + j) * 4 + k];

                    float cellX = navData.verts[(startVert + vertIndex) * 3];
                    float cellY = navData.verts[(startVert + vertIndex) * 3 + 1];
                    float cellZ = navData.verts[(startVert + vertIndex) * 3 + 2];

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

            for (int i = 0; i < ntris; ++i)
            {

                for (int k = 0; k < 3; k++)
                {

                    int vertIndex = navData.detailTris[(startTri + i) * 4 + k];

                    float cellX = navData.verts[(startVert + vertIndex) * 3];
                    float cellY = navData.verts[(startVert + vertIndex) * 3 + 1];
                    float cellZ = navData.verts[(startVert + vertIndex) * 3 + 2];

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

    }
}

