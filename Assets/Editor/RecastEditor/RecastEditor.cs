

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;

namespace GameEditor.RecastEditor

{

    public class RecastEditor
    {

        [MenuItem("Assets/GameEditor/导出地图navmesh", false, 900)]
        public static void ExportRecast()
        {
            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                ExportRecastInfo(path);
            }
        }

        [MenuItem("Assets/GameEditor/导出地图navmesh", true)]
        public static bool ValidExportRecast()
        {
            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (path.Contains(RecastConfig.MapResPath) && AssetDatabase.IsValidFolder(path))
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

        public static void ExportRecastInfo(string path)
        {

            DirectoryInfo dirInfo = new DirectoryInfo(path);
            foreach (var file in dirInfo.GetFiles())
            {
                if (file.Extension.ToLower() == ".unity")
                {
                    EditorSceneManager.OpenScene(path + "/" + file.Name); ;
                    break;
                }
            }

            Scene activeScene = SceneManager.GetActiveScene();
            string activeSceneName = activeScene.name;

            GameObject root = GameObject.Find("/" + activeSceneName);

            Transform navRoot = root.transform.Find(RecastConfig.MapElement);

            Mesh mesh = CombineMesh(navRoot);

            Heightfield hf = new Heightfield(mesh, RecastConfig.AgentMaxSlope, RecastConfig.AgentMaxClimb, RecastConfig.AgentHeight, RecastConfig.AgentRadius, RecastConfig.CellSize, RecastConfig.CellHeight);

            //判断三角形是否可行走
            AREATYPE[] areas = RcMarkWalkableTriangles(hf.WalkableSlopeAngle, mesh.vertices, mesh.triangles);

            //体素化三角形，构建高度场
            RecastHeightField.RcRasterizeTriangles(mesh.vertices, mesh.triangles, areas, hf);


            //过滤悬空的可走障碍物
            if (RecastConfig.FilterLowHangingObstacles)
            {
                RecastHeightField.RcFilterLowHangingWalkableObstacles(hf);
            }

            //过滤高度差过大的span
            if (RecastConfig.FilterLedgeSpans)
            {
                RecastHeightField.RcFilterLedgeSpans(hf);
            }

            //过滤不可通过高度span
            if (RecastConfig.FilterWalkableLowHeightSpans)
            {
                RecastHeightField.RcFilterWalkableLowHeightSpans(hf);
            }

            //DrawHeightfield(hf);

            //构建空心高度场
            CompactHeightfield chf = RecastHeightField.RcBuildCompactHeightfield(hf);

            //设置边缘不可行走
            RecastHeightField.RcErodeWalkableArea(chf);

            //设置特殊地形标识，用于设置的标记区域的多边形是y值恒定的多边形
            //RecastHeightField.RcMarkConvexPolyArea(chf, vertices,AREATYPE.None);

            //构建距离场
            RecastHeightField.RcBuildDistanceField(chf);

            //分水岭算法构建区域 
            RecastContour.RcBuildRegions(chf);

            DrawCompactHeightField(chf, 3);

            RcContourSet cset = new RcContourSet(chf);

            //计算区域边界
            RecastContour.RcBuildContours(chf, cset);

        }

        private static Mesh CombineMesh(Transform navRoot)
        {
            //合并mesh
            MeshFilter[] meshFilters = navRoot.GetComponentsInChildren<MeshFilter>();
            CombineInstance[] combines = new CombineInstance[meshFilters.Length];

            for (int i = 0; i < meshFilters.Length; i++)
            {
                combines[i].mesh = meshFilters[i].sharedMesh;
                combines[i].transform = navRoot.transform.worldToLocalMatrix * meshFilters[i].transform.localToWorldMatrix;
            }

            Mesh mesh = new Mesh();

            mesh.CombineMeshes(combines, true);

            return mesh;
        }


        private static AREATYPE[] RcMarkWalkableTriangles(float walkableSlopeAngle, Vector3[] verts, int[] tris)
        {

            float walkableThr = Mathf.Cos(walkableSlopeAngle / 180.0f * RecastConfig.PI);

            int numTris = tris.Length / 3;
            AREATYPE[] areas = new AREATYPE[numTris];

            for (int i = 0; i < numTris; i++)
            {


                //计算三角形法向量
                Vector3 norm = RecastUtility.CalcTriNormal(verts[tris[i * 3]], verts[tris[i * 3 + 1]], verts[tris[i * 3 + 2]]);

                //法向量在y轴的投影越大，倾斜角度越小
                if (norm[1] > walkableThr)
                {
                    areas[i] = AREATYPE.Walke;
                }
                else
                {
                    areas[i] = AREATYPE.None;
                }

            }

            return areas;
        }



        //用于绘制计算出来的高度场,并标记可行走区域
        public static void DrawHeightfield(Heightfield hf, bool showWalk = false)
        {

            int total = 0;
            int walkable = 0;

            Vector3 hfBBMin = hf.MinBounds;
            float cellSize = hf.CellSize;
            float cellHeight = hf.CellHeight;

            float[][] cubeList = new float[hf.SpanList.Length][];

            for (int i = 0; i < hf.SpanList.Length; i++)
            {
                int x = i % hf.Width;
                int z = i / hf.Width;

                Span currentSpan = hf.SpanList[i];
                List<float> spanCube = new List<float>();
                while (currentSpan != null)
                {
                    for (int y = currentSpan.Min; y < currentSpan.Max; y++)
                    {
                        float cellX = hfBBMin[0] + (float)x * cellSize + cellSize / 2;
                        float cellZ = hfBBMin[2] + (float)z * cellSize + cellSize / 2;
                        float cellY = hfBBMin[1] + (float)y * cellHeight + cellHeight / 2;

                        total++;


                        spanCube.Add(cellX);
                        spanCube.Add(cellY);
                        spanCube.Add(cellZ);

                        if (showWalk && currentSpan.AreaID != AREATYPE.Walke && y == currentSpan.Max - 1)
                        {
                            spanCube.Add(0);
                        }
                        else
                        {
                            spanCube.Add(7);
                            walkable++;
                        }

                    }

                    currentSpan = currentSpan.Next;
                }
                cubeList[i] = spanCube.ToArray();
            }

            Vector3 cubeSize = new Vector3(cellSize, cellHeight, cellSize);

            Scene activeScene = SceneManager.GetActiveScene();
            string activeSceneName = activeScene.name;

            GameObject root = GameObject.Find("/" + activeSceneName);

            if (root.GetComponent<RecastComponent>() == null)
            {
                root.AddComponent<RecastComponent>();
            }

            root.GetComponent<RecastComponent>().SetCubeList(cubeList, cubeSize);

            Debug.Log("Total Voxels:" + total + ",Walkable Voxels:" + walkable);
        }


        //用于绘制计算出来的空心高度场，绘制距离场参数,并标记区域 ,type 1 距离场,type 2 可行走区域,type 3 划分区域
        public static void DrawCompactHeightField(CompactHeightfield chf, int type = 1)
        {


            Vector3 hfBBMin = chf.MinBounds;
            float cellSize = chf.CellSize;
            float cellHeight = chf.CellHeight;


            int w = chf.Width;
            int h = chf.Height;

            int maxDistance = 0;

            if (chf.DistanceToBoundary != null)
            {
                foreach (int i in chf.DistanceToBoundary)
                {
                    maxDistance = Mathf.Max(maxDistance, i);
                }
            }

            float[][] cubeList = new float[h * w][];

            for (int z = 0; z < h; ++z)
            {
                for (int x = 0; x < w; ++x)
                {
                    CompactCell c = chf.CellList[x + z * w];


                    float[] spanCube = new float[c.Count * 4];

                    for (int i = c.Index, ni = (c.Index + c.Count); i < ni; ++i)
                    {

                        CompactSpan span = chf.SpanList[i];

                        //为了方便观察，CompactSpan只构建一个cellHeight，实际不止
                        float cellX = hfBBMin[0] + (float)x * cellSize + cellSize / 2;
                        float cellZ = hfBBMin[2] + (float)z * cellSize + cellSize / 2;
                        float cellY = hfBBMin[1] + (float)span.Y * cellHeight + cellHeight / 2;

                        int index = i - c.Index;

                        spanCube[index * 4] = cellX;
                        spanCube[index * 4 + 1] = cellY;
                        spanCube[index * 4 + 2] = cellZ;

                        if (chf.DistanceToBoundary != null && type == 1)
                        {
                            spanCube[index * 4 + 3] = (float)chf.DistanceToBoundary[i] / (float)maxDistance;
                        }
                        else if (chf.AreaList[i] == AREATYPE.Walke && type == 2)
                        {
                            spanCube[index * 4 + 3] = 7;
                        }
                        else if (type == 3)
                        {
                            spanCube[index * 4 + 3] = span.Reg;
                        }
                    }

                    cubeList[x + z * w] = spanCube;
                }
            }

            Vector3 cubeSize = new Vector3(cellSize, cellHeight, cellSize);

            Scene activeScene = SceneManager.GetActiveScene();
            string activeSceneName = activeScene.name;

            GameObject root = GameObject.Find("/" + activeSceneName);

            if (root.GetComponent<RecastComponent>() == null)
            {
                root.AddComponent<RecastComponent>();
            }

            root.GetComponent<RecastComponent>().SetCubeList(cubeList, cubeSize);
        }


        //用于绘制计算出来的区域边缘
        public static void DrawFieldContour(RcContourSet rcContourSet)
        {

            Scene activeScene = SceneManager.GetActiveScene();
            string activeSceneName = activeScene.name;

            GameObject root = GameObject.Find("/" + activeSceneName);

            Vector3 hfBBMin = rcContourSet.MinBounds;
            float cellSize = rcContourSet.CellSize;
            float cellHeight = rcContourSet.CellHeight;

            if (root.GetComponent<RecastComponent>() == null)
            {
                root.AddComponent<RecastComponent>();
            }

            float[][] vertList = new float[rcContourSet.NumConts][];

            for (int i = 0; i < rcContourSet.NumConts; ++i)
            {
                RcContour cont = rcContourSet.ContsList[i];

                float[] contVert = new float[cont.NumVerts * 4];

                for (int j = 0; j < cont.NumVerts; j++)
                {
                    float cellX = hfBBMin[0] + (float)cont.Verts[j * 4] * cellSize;
                    float cellZ = hfBBMin[2] + (float)cont.Verts[j * 4 + 2] * cellSize;
                    float cellY = hfBBMin[1] + (float)cont.Verts[j * 4 + 1] * cellHeight;

                    contVert[j * 4] = cellX;
                    contVert[j * 4 + 1] = cellY;
                    contVert[j * 4 + 2] = cellZ;
                    contVert[j * 4 + 3] = cont.Verts[j * 4 + 3];

                }
                vertList[i] = contVert;
            }

            root.GetComponent<RecastComponent>().SetContour(vertList);
        }
    }

}

