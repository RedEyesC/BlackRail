

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


            RcContourSet cset = new RcContourSet(chf);

            //DrawCompactHeightField(chf, 3);

            //计算区域边界
            RecastContour.RcBuildContours(chf, cset);

            //DrawFieldContour(cset);
            

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

            GameObject root = GameObject.Find("EditorRoot");
            if (root)
            {
                GameObject.DestroyImmediate(root);
            }

            root = new GameObject("EditorRoot");

            int total = 0;
            int walkable = 0;

            Vector3 hfBBMin = hf.MinBounds;
            float cellSize = hf.CellSize;
            float cellHeight = hf.CellHeight;

            Material newMat = null;

            for (int i = 0; i < hf.SpanList.Length; i++)
            {
                int x = i % hf.Width;
                int z = i / hf.Width;

                Span currentSpan = hf.SpanList[i];
                while (currentSpan != null)
                {
                    for (int y = currentSpan.Min; y < currentSpan.Max; y++)
                    {
                        float cellX = hfBBMin[0] + (float)x * cellSize + cellSize / 2;
                        float cellZ = hfBBMin[2] + (float)z * cellSize + cellSize / 2;
                        float cellY = hfBBMin[1] + (float)y * cellHeight + cellHeight / 2;

                        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        total++;
                        cube.transform.localScale = new Vector3(cellSize, cellHeight, cellSize);
                        cube.transform.position = new Vector3(cellX, cellY, cellZ);
                        cube.transform.SetParent(root.transform);

                        if (showWalk && currentSpan.AreaID == AREATYPE.Walke && y == currentSpan.Max - 1)
                        {

                            if (newMat == null)
                            {
                                Material mat = cube.GetComponent<MeshRenderer>().sharedMaterial;
                                newMat = UnityEngine.Material.Instantiate(mat);
                                newMat.color = UnityEngine.Color.red;
                            }

                            cube.GetComponent<MeshRenderer>().sharedMaterial = newMat;
                            walkable++;
                        }

                    }
                    currentSpan = currentSpan.Next;
                }
            }

            Debug.Log("Total Voxels:" + total + ",Walkable Voxels:" + walkable);
        }


        //用于绘制计算出来的空心高度场，绘制距离场参数,并标记区域 ,type 1 距离场,type 2 可行走区域,type 3 划分区域
        public static void DrawCompactHeightField(CompactHeightfield chf, int type = 1)
        {

            GameObject root = GameObject.Find("EditorRoot");
            if (root)
            {
                GameObject.DestroyImmediate(root);
            }

            root = new GameObject("EditorRoot");

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

            Color[] colorMap = { Color.red, Color.green, Color.blue, Color.white, Color.black,
                Color.yellow, Color.cyan, Color.magenta, Color.gray };


            Material newMat = null;
            Dictionary<int, Material> matMap = new Dictionary<int, Material>();

            for (int z = 0; z < h; ++z)
            {
                for (int x = 0; x < w; ++x)
                {
                    CompactCell c = chf.CellList[x + z * w];
                    for (int i = c.Index, ni = (c.Index + c.Count); i < ni; ++i)
                    {

                        CompactSpan span = chf.SpanList[i];


                        //为了方便观察，CompactSpan只构建一个cellHeight，实际不止
                        float cellX = hfBBMin[0] + (float)x * cellSize + cellSize / 2;
                        float cellZ = hfBBMin[2] + (float)z * cellSize + cellSize / 2;
                        float cellY = hfBBMin[1] + (float)span.Y * cellHeight + cellHeight / 2;

                        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        cube.transform.localScale = new Vector3(cellSize, cellHeight, cellSize);
                        cube.transform.position = new Vector3(cellX, cellY, cellZ);
                        cube.transform.SetParent(root.transform);

                        if (chf.DistanceToBoundary != null && type == 1)
                        {

                            if (!matMap.ContainsKey(chf.DistanceToBoundary[i]))
                            {
                                Material mat = cube.GetComponent<MeshRenderer>().sharedMaterial;
                                Material cloneMat = UnityEngine.Material.Instantiate(mat);
                                float color = (float)chf.DistanceToBoundary[i] / (float)maxDistance;
                                cloneMat.color = new UnityEngine.Color(color, color, color);

                                matMap.Add(chf.DistanceToBoundary[i], cloneMat);
                            }

                            cube.GetComponent<MeshRenderer>().sharedMaterial = matMap[chf.DistanceToBoundary[i]];
                            continue;
                        }

                        if (chf.AreaList[i] == AREATYPE.Walke && type == 2)
                        {

                            if (newMat == null)
                            {
                                Material mat = cube.GetComponent<MeshRenderer>().sharedMaterial;
                                newMat = UnityEngine.Material.Instantiate(mat);
                                newMat.color = UnityEngine.Color.red;
                            }
                            cube.GetComponent<MeshRenderer>().sharedMaterial = newMat;
                            continue;
                        }

                        if (type == 3)
                        {

                            if (!matMap.ContainsKey(span.Reg))
                            {
                                Material mat = cube.GetComponent<MeshRenderer>().sharedMaterial;
                                Material cloneMat = UnityEngine.Material.Instantiate(mat);
                                cloneMat.color = colorMap[span.Reg % 8];
                                matMap.Add(span.Reg, cloneMat);
                            }

                            cube.GetComponent<MeshRenderer>().sharedMaterial = matMap[span.Reg];
                            continue;
                        }
                    }

                }
            }

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

