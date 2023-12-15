
using System;
using System.Collections.Generic;

namespace GameEditor.RecastEditor
{

    public class Line
    {
        public int start;
        public int end;
        public float[] mid;

        public Line(int v0, int v1)
        {
            start = v0;
            end = v1;
        }

    }


    public class NavPoly
    {
        public int index;//导出时的索引
        public Dictionary<int, Line> portals; //邻接边
        public int[] verts;//顶点在vert中的索引
        public float[] centroid; //重心
        public float boundingRadius;//外接圆半径

        public NavPoly(int[] vertIndexList, int triIndex)
        {
            index = triIndex;

            verts = vertIndexList;
        }
    }

    internal class RecastExport
    {
        public static void ExportNavMeshDataToJson(RcPolyMeshDetail dmesh)
        {
            NavPoly[] polys = new NavPoly[dmesh.ntris];

            int tri = 0;
            for (int i = 0; i < dmesh.nmeshes; i++)
            {

                int startVert = dmesh.meshes[i * 4 + 0];
                int startTri = dmesh.meshes[i * 4 + 2];
                int ntris = dmesh.meshes[i * 4 + 3];

                for (int j = 0; j < ntris; ++j)
                {

                    List<float> ployVert = new List<float>();
                    List<int> vertIndexList = new List<int>();
                    for (int k = 0; k < 3; k++)
                    {

                        int vertIndex = dmesh.tris[(startTri + j) * 4 + k];


                        vertIndexList.Add(startVert + vertIndex);

                    }

                    NavPoly poly = new NavPoly(vertIndexList.ToArray(), tri);
                    polys[tri] = poly;
                    tri++;
                }

            }



            int[] edges = new int[polys.Length * 3];
            int edgeCount = 0;
            foreach (NavPoly poly in polys)
            {

                //初始化顶点
                List<float[]> points = new List<float[]>();
                for (int i = 0; i < poly.verts.Length; i++)
                {
                    float[] point = new float[3];
                    point[0] = dmesh.verts[poly.verts[i] * 3];
                    point[1] = dmesh.verts[poly.verts[i] * 3 + 1];
                    point[2] = dmesh.verts[poly.verts[i] * 3 + 2]; ;

                    points.Add(point);
                }

                //初始化中心（凸多边形简单采用中心）
                float[] centroid = new float[3];
                for (int i = 0; i < points.Count; i++)
                {
                    centroid[0] += points[i][0];
                    centroid[1] += points[i][1];
                    centroid[2] += points[i][2];
                }

                centroid[0] /= points.Count;
                centroid[1] /= points.Count;
                centroid[2] /= points.Count;

                poly.centroid = centroid;

                //初始化外接圆半径
                float boundingRadius = 0;
                for (int i = 0; i < points.Count; i++)
                {
                    var dis = RecastUtility.Vdist3(centroid[0], centroid[1], centroid[2], points[i][0], points[i][1], points[i][2]);
                    boundingRadius = dis > boundingRadius ? dis : boundingRadius;
                }

                poly.boundingRadius = boundingRadius;


                //for (int i = 0; i < points.Count; i++)
                //{
                //    int v0 = poly.verts[i];
                //    int v1 = (i + 1 >= points.Count) ? poly.verts[0] : poly.verts[i + 1];

                //    if (v0 < v1)
                //    {

                //        edges[edgeCount] = new Line(v0, v1);

                //        //以v0位出发点的边可能有两条，所以先把之前的边的索引放入nextEdge，然后把新的放入firstEdge
                //        nextEdge[edgeCount] = firstEdge[v0];
                //        firstEdge[v0] = edgeCount;
                //        edgeCount++;
                //    }

                //}

            }

        }

    }
}
