

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System;

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
 
            RecastUtility.CalcBounds(mesh.vertices, out float[] meshMinBounds, out float[] meshMaxBounds);

            RecastUtility.CalcGridSize(meshMinBounds, meshMaxBounds, RecastConfig.CellSize, out int meshWidth, out int meshHeight);

            RcHeightfield hf = new RcHeightfield(meshMinBounds, meshMaxBounds,meshWidth,meshHeight, RecastConfig.AgentMaxSlope, RecastConfig.AgentMaxClimb, RecastConfig.AgentHeight, RecastConfig.AgentRadius, RecastConfig.CellSize, RecastConfig.CellHeight);

            //判断三角形是否可行走
            AREATYPE[] areas = RcMarkWalkableTriangles(hf.walkableSlopeAngle, mesh.vertices, mesh.triangles);

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

            //DrawHeightfield(hf,true);

            //构建空心高度场
            RcCompactHeightfield chf = new RcCompactHeightfield(hf);

            RecastHeightField.RcBuildCompactHeightfield(hf, chf);

            //设置边缘不可行走
            RecastHeightField.RcErodeWalkableArea(chf);

            //设置特殊地形标识，用于设置的标记区域的多边形是y值恒定的多边形
            //RecastHeightField.RcMarkConvexPolyArea(chf, vertices,AREATYPE.None);

            //构建距离场
            RecastHeightField.RcBuildDistanceField(chf);

            //分水岭算法构建区域 
            RecastContour.RcBuildRegions(chf);

            //DrawCompactHeightField(chf, 3);

            RcContourSet cset = new RcContourSet(chf);

            //计算区域边界
            RecastContour.RcBuildContours(chf, cset);

            //DrawFieldContour(cset);

            RcPolyMesh pmesh = new RcPolyMesh(cset);

            //构建PolyMesh
            RecastMesh.RcBuildPolyMesh(cset, pmesh);

            DrawFieldMesh(pmesh);

            RcPolyMeshDetail dmesh = new RcPolyMeshDetail();

            RecastMeshDetail.RcBuildPolyMeshDetail(pmesh,chf,dmesh);
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

            float walkableThr = (float)Math.Cos(walkableSlopeAngle / 180.0f * RecastConfig.PI);

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
        public static void DrawHeightfield(RcHeightfield hf, bool showWalk = false)
        {

            int total = 0;
            int walkable = 0;

            float[] hfBBMin = hf.minBounds;
            float cellSize = hf.cellSize;
            float cellHeight = hf.cellHeight;

            float[][] cubeList = new float[hf.spans.Length][];

            for (int i = 0; i < hf.spans.Length; i++)
            {
                int x = i % hf.width;
                int z = i / hf.width;

                RcSpan currentSpan = hf.spans[i];
                List<float> spanCube = new List<float>();
                while (currentSpan != null)
                {
                    for (int y = currentSpan.min; y < currentSpan.max; y++)
                    {
                        float cellX = hfBBMin[0] + (float)x * cellSize + cellSize / 2;
                        float cellZ = hfBBMin[2] + (float)z * cellSize + cellSize / 2;
                        float cellY = hfBBMin[1] + (float)y * cellHeight + cellHeight / 2;

                        total++;


                        spanCube.Add(cellX);
                        spanCube.Add(cellY);
                        spanCube.Add(cellZ);

                        if (showWalk && currentSpan.areaID != AREATYPE.Walke && y == currentSpan.max - 1)
                        {
                            spanCube.Add(0);
                        }
                        else
                        {
                            spanCube.Add(7);
                            walkable++;
                        }

                    }

                    currentSpan = currentSpan.next;
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
        }


        //用于绘制计算出来的空心高度场，绘制距离场参数,并标记区域 ,type 1 距离场,type 2 可行走区域,type 3 划分区域
        public static void DrawCompactHeightField(RcCompactHeightfield chf, int type = 1)
        {


            float[] hfBBMin = chf.minBounds;
            float cellSize = chf.cellSize;
            float cellHeight = chf.cellHeight;


            int w = chf.width;
            int h = chf.height;

            int maxDistance = 0;

            if (chf.distanceToBoundary != null)
            {
                foreach (int i in chf.distanceToBoundary)
                {
                    maxDistance = Math.Max(maxDistance, i);
                }
            }

            float[][] cubeList = new float[h * w][];

            for (int z = 0; z < h; ++z)
            {
                for (int x = 0; x < w; ++x)
                {
                    CompactCell c = chf.cells[x + z * w];


                    float[] spanCube = new float[c.count * 4];

                    for (int i = c.index, ni = (c.index + c.count); i < ni; ++i)
                    {

                        CompactSpan span = chf.spans[i];

                        //为了方便观察，CompactSpan只构建一个cellHeight，实际不止
                        float cellX = hfBBMin[0] + x * cellSize + cellSize / 2;
                        float cellZ = hfBBMin[2] + z * cellSize + cellSize / 2;
                        float cellY = hfBBMin[1] + span.y * cellHeight + cellHeight / 2;

                        int index = i - c.index;

                        spanCube[index * 4] = cellX;
                        spanCube[index * 4 + 1] = cellY;
                        spanCube[index * 4 + 2] = cellZ;

                        if (chf.distanceToBoundary != null && type == 1)
                        {
                            spanCube[index * 4 + 3] = (float)chf.distanceToBoundary[i] / (float)maxDistance;
                        }
                        else if (chf.areas[i] == AREATYPE.Walke && type == 2)
                        {
                            spanCube[index * 4 + 3] = 7;
                        }
                        else if (type == 3)
                        {
                            spanCube[index * 4 + 3] = span.reg;
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

            float[] hfBBMin = rcContourSet.minBounds;
            float cellSize = rcContourSet.cellSize;
            float cellHeight = rcContourSet.cellHeight;

            if (root.GetComponent<RecastComponent>() == null)
            {
                root.AddComponent<RecastComponent>();
            }

            float[][] vertList = new float[rcContourSet.numConts][];

            for (int i = 0; i < rcContourSet.numConts; ++i)
            {
                RcContour cont = rcContourSet.conts[i];

                float[] contVert = new float[cont.nverts * 4];

                for (int j = 0; j < cont.nverts; j++)
                {
                    float cellX = hfBBMin[0] + cont.verts[j * 4] * cellSize;
                    float cellZ = hfBBMin[2] + cont.verts[j * 4 + 2] * cellSize;
                    float cellY = hfBBMin[1] + cont.verts[j * 4 + 1] * cellHeight;

                    contVert[j * 4] = cellX;
                    contVert[j * 4 + 1] = cellY;
                    contVert[j * 4 + 2] = cellZ;
                    contVert[j * 4 + 3] = cont.verts[j * 4 + 3];

                }
                vertList[i] = contVert;
            }

            root.GetComponent<RecastComponent>().SetContour(vertList);
        }


        //用于绘制计算出来的分割的多边形
        public static void DrawFieldMesh(RcPolyMesh pmesh)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            string activeSceneName = activeScene.name;

            GameObject root = GameObject.Find("/" + activeSceneName);

            float[] hfBBMin = pmesh.minBounds;
            float cellSize = pmesh.cellSize;
            float cellHeight = pmesh.cellHeight;

            if (root.GetComponent<RecastComponent>() == null)
            {
                root.AddComponent<RecastComponent>();
            }

 
            float[][] polyList = new float[pmesh.npolys][];

            for (int i = 0; i < pmesh.npolys; ++i)
            {

                List<float> ployVert = new List<float>();

                for (int j = 0; j < RecastConfig.MaxVertsPerPoly; j++)
                {

                    int vertIndex = pmesh.polys[i * RecastConfig.MaxVertsPerPoly * 2 + j];

                    if(vertIndex == RecastConfig.RC_MESH_NULL_IDX)
                    {
                        continue;
                    }

                    float cellX = hfBBMin[0] + pmesh.verts[vertIndex * 3] * cellSize;
                    float cellZ = hfBBMin[2] + pmesh.verts[vertIndex * 3 + 2] * cellSize;
                    float cellY = hfBBMin[1] + pmesh.verts[vertIndex * 3 + 1] * cellHeight;

                    ployVert.Add(cellX);
                    ployVert.Add(cellY);
                    ployVert.Add(cellZ);
                    ployVert.Add(0);

                }
                polyList[i] = ployVert.ToArray();
            }

            root.GetComponent<RecastComponent>().SetContour(polyList);
        }
    }

}

