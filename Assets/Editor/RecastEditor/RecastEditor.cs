
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameEditor

{

    public class RecastEditor
    {
        static readonly float PI = 3.1415926f;

        static readonly string MapResPath = "Assets/Resources/map/";
        static readonly string MapElement = "MapElement";

        //构建寻路网格的参数
        static readonly float WalkableSlopeAngle = 20;
        static readonly float WalkableClimb = 9;
        static readonly float CellSize = 0.2f; //xz平面的尺寸
        static readonly float CellHeight = 0.3f; // y轴的尺寸


        [MenuItem("Assets/GameEditor/导出地图navmesh", false, 900)]
        static void ExportRecast()
        {
            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                ExportRecastInfo(path);
            }
        }

        [MenuItem("Assets/GameEditor/导出地图navmesh", true)]
        static bool ValidExportRecast()
        {
            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (path.Contains(MapResPath) && AssetDatabase.IsValidFolder(path))
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        static void ExportRecastInfo(string path)
        {

            DirectoryInfo dirInfo = new DirectoryInfo(path);
            foreach (var file in dirInfo.GetFiles())
            {
                if (file.Extension.ToLower() == ".unity")
                {
                    Scene tempScene = EditorSceneManager.OpenScene(path + "/" + file.Name); ;

                    break;
                }
            }

            Scene activeScene = SceneManager.GetActiveScene();
            string activeSceneName = activeScene.name;
            var scenePath = activeScene.path;
            string activeScenePath = scenePath.Remove(scenePath.LastIndexOf("/"));

            GameObject root = GameObject.Find("/" + activeSceneName);

            Transform navRoot = root.transform.Find(MapElement);

            MeshFilter[] meshFilters = navRoot.GetComponentsInChildren<MeshFilter>();
            CombineInstance[] combines = new CombineInstance[meshFilters.Length];

            for (int i = 0; i < meshFilters.Length; i++)
            {
                combines[i].mesh = meshFilters[i].sharedMesh;
                combines[i].transform = navRoot.transform.worldToLocalMatrix * meshFilters[i].transform.localToWorldMatrix;
            }

            Mesh mesh = new Mesh();

            mesh.CombineMeshes(combines, true);

            Heightfield hf = new Heightfield(mesh, WalkableSlopeAngle, WalkableClimb, CellSize, CellHeight);


            AREATYPE[] areas = RcMarkWalkableTriangles(hf.WalkableSlopeAngle, mesh.vertices, mesh.triangles);

            RcRasterizeTriangles(mesh.vertices, mesh.triangles, areas, hf);

            var a = hf.SpanList;

        }

        //判断是否符合可行走角度
        static AREATYPE[] RcMarkWalkableTriangles(float walkableSlopeAngle, Vector3[] verts, int[] tris)
        {

            float walkableThr = Mathf.Cos(walkableSlopeAngle / 180.0f * PI);

            int numTris = tris.Length / 3;
            AREATYPE[] areas = new AREATYPE[numTris];

            for (int i = 0; i < numTris; i++)
            {


                //计算三角形法向量
                Vector3 norm = CommonUtility.CalcTriNormal(verts[tris[i * 3]], verts[tris[i * 3 + 1]], verts[tris[i * 3 + 2]]);

                //法向量在y轴的投影越大，倾斜角度越小
                if (norm[1] > walkableThr)
                {
                    areas[i] = AREATYPE.Walke;
                }

            }

            return areas;
        }

        //体素化三角形，构建高度场
        static void RcRasterizeTriangles(Vector3[] verts, int[] tris, AREATYPE[] areas, Heightfield hf)
        {

            int numTris = tris.Length / 3;
            for (int i = 0; i < numTris; i++)
            {
                Vector3 vert1 = verts[tris[i * 3]];
                Vector3 vert2 = verts[tris[i * 3 + 1]];
                Vector3 vert3 = verts[tris[i * 3 + 2]];

                if (!RasterizeTri(vert1, vert2, vert3, areas[i], hf))
                {
                    Debug.LogError("rcRasterizeTriangles: Out of memory.");
                }

            }

        }

        static bool RasterizeTri(Vector3 v0, Vector3 v1, Vector3 v2, AREATYPE areaType, Heightfield hf)
        {
            Vector3 triBBMin = v0;
            triBBMin = Vector3.Min(triBBMin, v1);
            triBBMin = Vector3.Min(triBBMin, v2);

            Vector3 triBBMax = v0;
            triBBMax = Vector3.Max(triBBMax, v1);
            triBBMax = Vector3.Max(triBBMax, v2);

            Vector3 hfBBMin = hf.minBounds;
            Vector3 hfBBMax = hf.maxBounds;

            float CellSize = hf.CellSize;
            float inverseCellSize = 1 / hf.CellSize;
            float inverseCellHeight = 1 / hf.Height;



            float by = hfBBMax[1] - hfBBMin[1];

            // 三角形的包围盒不在目标包围盒范围内就放弃这个三角形
            if (!CommonUtility.OverlapBounds(triBBMin, triBBMax, hfBBMin, hfBBMax))
            {
                return true;
            }

            //在xz平面上，以体素为单位的长度，z轴的最大值和最小值。以包围盒的z为0点,
            int z0 = (int)((triBBMin[2] - hfBBMin[2]) * inverseCellSize);
            int z1 = (int)((triBBMax[2] - hfBBMin[2]) * inverseCellSize);

            // 案例里写着·使用-1比0 更好的平铺三角形？？，为什么呢
            z0 = Mathf.Clamp(z0, -1, hf.Height);
            z1 = Mathf.Clamp(z1, 0, hf.Height);


            //三角形被正方形切割最多切割出7边形，存放四组多边形的数据
            Vector3[] nvInList = new Vector3[7];
            Vector3[] nvRowList = new Vector3[7];
            Vector3[] p1InList = new Vector3[7];

            nvInList[0] = v0;
            nvInList[1] = v1;
            nvInList[2] = v2;

            int nvRow = 0;
            int nvIn = 3;
            //按z轴开始切割
            for (int z = z0; z <= z1; ++z)
            {

                float cellZ = hfBBMin[2] + (float)z * CellSize;
                //切割三角形，nvRowList 为切割线下半部分的顶点 ,输出的nvInList 为切割线上半部分顶点
                CommonUtility.DividePoly(nvInList, nvIn, cellZ + CellSize, RcAxis.AXIS_Z, out nvRow, out nvIn, out nvRowList, out nvInList);

                //没切割到东西，顶点小于3构不成多边形
                if (nvRow < 3)
                {
                    continue;
                }

                if (z < 0)
                {
                    continue;
                }


                //重复之前的步骤，按x轴切割
                float minX = nvRowList[0][0];
                float maxX = nvRowList[0][0];

                for (int vert = 1; vert < nvRow; ++vert)
                {

                    float x = nvRowList[vert][0];

                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);

                }

                int x0 = (int)((minX - hfBBMin[0]) * inverseCellSize);
                int x1 = (int)((maxX - hfBBMin[0]) * inverseCellSize);
                if (x1 < 0 || x0 >= hf.Width)
                {
                    continue;
                }
                x0 = Mathf.Clamp(x0, -1, hf.Width - 1);
                x1 = Mathf.Clamp(x1, 0, hf.Width - 1);


                int nvRow2 = 0;
                int nvIn2 = nvRow;

                for (int x = x0; x <= x1; ++x)
                {
                    float cellX = hfBBMin[0] + (float)x * CellSize;
                    CommonUtility.DividePoly(nvRowList, nvIn2, cellX + CellSize, RcAxis.AXIS_X, out nvRow2, out nvIn2, out p1InList, out nvRowList);

                    if (nvRow2 < 3)
                    {
                        continue;
                    }

                    if (x < 0)
                    {
                        continue;
                    }

                    float spanMin = nvRowList[0][1];
                    float spanMax = nvRowList[0][1];

                    for (int vert = 1; vert < nvRow2; ++vert)
                    {

                        float y = p1InList[vert][1];

                        spanMin = Mathf.Min(spanMin, y);
                        spanMax = Mathf.Max(spanMax, y);

                    }

                    //坐标Y 平移到最小包围盒底部
                    spanMin -= hfBBMin[1];
                    spanMax -= hfBBMin[1];


                    // 假如在包围盒外，舍弃
                    if (spanMax < 0.0f)
                    {
                        continue;
                    }
                    if (spanMin > by)
                    {
                        continue;
                    }

                    // 截取包围盒内的部分
                    if (spanMin < 0.0f)
                    {
                        spanMin = 0;
                    }
                    if (spanMax > by)
                    {
                        spanMax = by;
                    }

                    int spanMinCellIndex = (int)Mathf.Max(spanMin * inverseCellHeight, 0);
                    int spanMaxCellIndex = (int)Mathf.Max(spanMax * inverseCellHeight, spanMinCellIndex + 1);

                    // 加入高度场
                    if (!AddSpan(hf, x, z, spanMinCellIndex, spanMaxCellIndex, areaType))
                    {
                        return false;
                    }
                }



            }
            return true;
        }


        static bool AddSpan(Heightfield hf, int x, int z, int min, int max, AREATYPE areaType)
        {
            Span newSpan = new Span(min, max, areaType);

            int columnIndex = x + z * hf.Width;

            //缓存比newSpan小的体素的列表
            Span previousSpan = null;
            //进行比较的体素
            Span currentSpan = hf.SpanList[columnIndex];

            while (currentSpan != null)
            {
                //newSpan位置在currentSpan之下
                if (currentSpan.Min > newSpan.Max)
                {
                    break;
                }

                //newSpan位置在currentSpan之上
                if (currentSpan.Max < newSpan.Min)
                {
                    //切换到链表的下一个体素
                    previousSpan = currentSpan;
                    currentSpan = currentSpan.Next;
                }
                //newSpan位置与currentSpan重叠
                else
                {
                    if (currentSpan.Min < newSpan.Min)
                    {
                        newSpan.Min = currentSpan.Min;
                    }
                    if (currentSpan.Max > newSpan.Max)
                    {
                        newSpan.Max = currentSpan.Max;
                    }

                    if(Mathf.Abs(newSpan.Max - currentSpan.Max) < hf.WalkableClimb)
                    {
                        newSpan.AreaID = (AREATYPE)Mathf.Max((int)newSpan.AreaID,(int)currentSpan.AreaID);
                    }

                    //从链表中释放currentSpan
                    Span next = currentSpan.Next;
                    if (previousSpan != null)
                    {
                        previousSpan.Next = next;
                    }
                    else
                    {
                        hf.SpanList[columnIndex] = next;
                    }

                    //切换currentSpan 进行下一轮比较
                    currentSpan = next;
                }

            }

            if (previousSpan != null)
            {
                //将newSpan插入 previousSpan 的后面
                newSpan.Next = previousSpan.Next;
                previousSpan.Next = newSpan;
            }
            else
            {
                newSpan.Next = hf.SpanList[columnIndex];
                hf.SpanList[columnIndex] = newSpan;
            }

            return true;
        }

        public static void buildHeightfield(Heightfield hf)
        {
           
            for(int i= 0; i< hf.SpanList.Length; i++)
            {
                int x = i % hf.Width;
                int z = i / hf.Width;

                Span currentSpan = hf.SpanList[i];
                while (currentSpan != null)
                {
                    for(int j = currentSpan.Min; j <= currentSpan.Max; j++)
                    {

                    }
                    currentSpan = currentSpan.Next;
                }
            }
            
        }
    }

}
