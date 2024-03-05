

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System;
using GameEditor.Utility;
using GameFramework.Recast;
using GameFramework.Detour;
namespace GameEditor.RecastEditor

{

    public class RecastEditor
    {


        public static readonly string MapResPath = "Assets/Resources/Map/";
        public static readonly string MapElement = "MapElement";


        [MenuItem("Assets/Game Editor/导出地图navmesh", false, 900)]
        public static void ExportRecast()
        {
            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                ExportRecastInfo(path);
            }
        }

        [MenuItem("Assets/Game Editor/导出地图navmesh", true)]
        public static bool ValidExportRecast()
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

        public static void ExportRecastInfo(string path)
        {

            CommonUtility.ResetTimers();
            CommonUtility.DoStartTimer("build NavMesh");


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

            Transform navRoot = root.transform.Find(MapElement);

            Mesh mesh = CombineMesh(navRoot);

            float[] vertices = new float[mesh.vertexCount * 3];

            for (int i = 0; i < mesh.vertexCount; i++)
            {

                vertices[i * 3 + 0] = mesh.vertices[i].x;
                vertices[i * 3 + 1] = mesh.vertices[i].y;
                vertices[i * 3 + 2] = mesh.vertices[i].z;
            }

            int[] triangles = mesh.triangles;

            SoleMeshBuild(vertices, triangles);

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

        public static void SoleMeshBuild(float[] vertices, int[] triangles)
        {

            RecastUtility.CalcBounds(vertices, out float[] meshMinBounds, out float[] meshMaxBounds);

            RecastUtility.CalcGridSize(meshMinBounds, meshMaxBounds, RecastConfig.CellSize, out int meshWidth, out int meshHeight);

            RcHeightfield hf = new RcHeightfield(meshMinBounds, meshMaxBounds, meshWidth, meshHeight, RecastConfig.AgentMaxSlope, RecastConfig.AgentMaxClimb, RecastConfig.AgentHeight, RecastConfig.AgentRadius, RecastConfig.CellSize, RecastConfig.CellHeight);

            //判断三角形是否可行走
            AREATYPE[] areas = RecastUtility.RcMarkWalkableTriangles(hf.walkableSlopeAngle, vertices, triangles);

            //体素化三角形，构建高度场
            RecastHeightField.RcRasterizeTriangles(vertices, triangles, areas, hf);

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

            DrawMesh(pmesh);

            RcPolyMeshDetail dmesh = new RcPolyMeshDetail();

            //细化mesh网格
            RecastMeshDetail.RcBuildPolyMeshDetail(pmesh, chf, dmesh);

            //DrawMeshDetail(dmesh);

            RecastExport.ExportNavMeshDataToJson(pmesh, dmesh);

            DtNavData param = new DtNavData();

            param.bmin = pmesh.minBounds;
            param.bmax = pmesh.maxBounds;
            param.polyCount = pmesh.npolys;
            param.polys = pmesh.polys;

            param.vertCount = pmesh.nverts;
            param.verts = pmesh.verts;

            param.walkableClimb = pmesh.walkableClimb;
            param.quantFactor = 1.0f / pmesh.cellSize;

            param.cs = pmesh.cellSize;
            param.ch = pmesh.cellHeight;

            param.polyFlags = pmesh.flags;

            param.detailMeshes = dmesh.meshes;
            param.detailVerts = dmesh.verts;
            param.detailTris = dmesh.tris;
            param.detailVertsCount = dmesh.nverts;


            //寻路相关测试
            DetourNavMeshBuild.DtCreateNavMeshData(param);

            DetourNavMesh dtnav = new DetourNavMesh();

            dtnav.Init(param);

            float[] path = dtnav.SearchPath(-6.13, -2.33, 20.7, 0.45, 15.41, 11.54);
            //dtnav.SearchPath(-6.13, -2.33, 20.7, 17.2, -2.2, 27.09);
            //DrawPath(path);
        }


        //用于绘制计算出来的高度场,并标记可行走区域
        public static void DrawHeightfield(RcHeightfield hf, bool showWalk = false)
        {

            float[] hfBBMin = hf.minBounds;
            float cellSize = hf.cellSize;
            float cellHeight = hf.cellHeight;

            List<float> param = new List<float>();

            for (int i = 0; i < hf.spans.Length; i++)
            {
                int x = i % hf.width;
                int z = i / hf.width;

                RcSpan currentSpan = hf.spans[i];

                while (currentSpan != null)
                {

                    for (int y = currentSpan.min; y < currentSpan.max; y++)
                    {
                        //只绘制最上层方便观察
                        if (y != currentSpan.max - 1)
                        {
                            continue;
                        }

                        float cellX = hfBBMin[0] + (float)x * cellSize + cellSize / 2;
                        float cellZ = hfBBMin[2] + (float)z * cellSize + cellSize / 2;
                        float cellY = hfBBMin[1] + (float)y * cellHeight + cellHeight / 2;

                        param.Add(1);
                        param.Add(12);

                        param.Add(i);
                        param.Add(cellX);
                        param.Add(cellY);
                        param.Add(cellZ);

                        param.Add(cellSize);
                        param.Add(cellHeight);
                        param.Add(cellSize);

                        //标记不可行走为红色
                        if (showWalk && currentSpan.areaID != AREATYPE.Walke)
                        {
                            param.Add(1);
                            param.Add(0);
                            param.Add(0);
                        }
                        else
                        {
                            param.Add(0);
                            param.Add(0);
                            param.Add(1);
                        }

                    }

                    currentSpan = currentSpan.next;
                }
            }


            SetNavDebug(param.ToArray());
        }


        //用于绘制计算出来的空心高度场，绘制距离场参数,并标记区域 ,type 1 距离场,type 2 可行走区域,type 3 划分区域
        public static void DrawCompactHeightField(RcCompactHeightfield chf, int type = 1)
        {

            float[] hfBBMin = chf.minBounds;
            float cellSize = chf.cellSize;
            float cellHeight = chf.cellHeight;

            Color[] colorMap = {Color.green, Color.blue, Color.white, Color.black,
                Color.yellow, Color.cyan, Color.magenta, Color.gray };

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

            List<float> param = new List<float>();

            for (int z = 0; z < h; ++z)
            {
                for (int x = 0; x < w; ++x)
                {
                    CompactCell c = chf.cells[x + z * w];

                    for (int i = c.index, ni = (c.index + c.count); i < ni; ++i)
                    {

                        CompactSpan span = chf.spans[i];

                        //为了方便观察，CompactSpan只构建一个cellHeight，实际不止
                        float cellX = hfBBMin[0] + x * cellSize + cellSize / 2;
                        float cellZ = hfBBMin[2] + z * cellSize + cellSize / 2;
                        float cellY = hfBBMin[1] + span.y * cellHeight + cellHeight / 2;

                        param.Add(1);
                        param.Add(12);

                        param.Add(i);
                        param.Add(cellX);
                        param.Add(cellY);
                        param.Add(cellZ);

                        param.Add(cellSize);
                        param.Add(cellHeight);
                        param.Add(cellSize);


                        if (chf.distanceToBoundary != null && type == 1)
                        {
                            float color = (float)chf.distanceToBoundary[i] / (float)maxDistance;
                            param.Add(color);
                            param.Add(color);
                            param.Add(color);
                        }
                        else if (chf.areas[i] != AREATYPE.Walke && type == 2)
                        {
                            //标记不可行走为红色
                            param.Add(1);
                            param.Add(0);
                            param.Add(0);
                        }
                        else if (type == 3)
                        {
                            Color color = colorMap[(int)span.reg % 8];
                            param.Add(color.r);
                            param.Add(color.g);
                            param.Add(color.b);
                        }
                        else
                        {
                            param.Add(0);
                            param.Add(0);
                            param.Add(1);
                        }

                    }
                }
            }

            SetNavDebug(param.ToArray());
        }


        //用于绘制计算出来的区域边缘
        public static void DrawFieldContour(RcContourSet rcContourSet)
        {

            float[] hfBBMin = rcContourSet.minBounds;
            float cellSize = rcContourSet.cellSize;
            float cellHeight = rcContourSet.cellHeight;

            List<float> param = new List<float>();

            for (int i = 0; i < rcContourSet.numConts; ++i)
            {
                RcContour cont = rcContourSet.conts[i];


                int len = 4 + cont.nverts * 3;

                param.Add(2);
                param.Add(len);
                param.Add(i);
                param.Add(cont.nverts);

                for (int j = 0; j < cont.nverts; j++)
                {
                    float cellX = hfBBMin[0] + cont.verts[j * 4] * cellSize;
                    float cellY = hfBBMin[1] + cont.verts[j * 4 + 1] * cellHeight;
                    float cellZ = hfBBMin[2] + cont.verts[j * 4 + 2] * cellSize;

                    param.Add(cellX);
                    param.Add(cellY);
                    param.Add(cellZ);

                }
            }

            SetNavDebug(param.ToArray());
        }


        //用于绘制计算出来的分割的多边形
        public static void DrawMesh(RcPolyMesh pmesh)
        {
            float[] hfBBMin = pmesh.minBounds;
            float cellSize = pmesh.cellSize;
            float cellHeight = pmesh.cellHeight;

            List<float> param = new List<float>();

            int len = 4 + RecastConfig.MaxVertsPerPoly * 3;
            float[] po1y = new float[len];

            for (int i = 0; i < pmesh.npolys; ++i)
            {

                int count = 0;

                po1y[0] = 2;
                po1y[1] = len;
                po1y[2] = i;

                for (int j = 0; j < RecastConfig.MaxVertsPerPoly; j++)
                {

                    int vertIndex = pmesh.polys[i * RecastConfig.MaxVertsPerPoly * 2 + j];

                    if (vertIndex == RecastConfig.RC_MESH_NULL_IDX)
                    {
                        continue;
                    }

                    float cellX = hfBBMin[0] + pmesh.verts[vertIndex * 3] * cellSize;
                    float cellZ = hfBBMin[2] + pmesh.verts[vertIndex * 3 + 2] * cellSize;
                    float cellY = hfBBMin[1] + pmesh.verts[vertIndex * 3 + 1] * cellHeight;

                    po1y[4 + count * 3] = cellX;
                    po1y[4 + count * 3 + 1] = cellY;
                    po1y[4 + count * 3 + 2] = cellZ;

                    count++;

                }

                po1y[3] = count;

                for (int j = 0; j < len; j++)
                {
                    param.Add(po1y[j]);
                }

            }

            SetNavDebug(param.ToArray());
        }


        //用于绘制计算出来的细化后的多边形
        public static void DrawMeshDetail(RcPolyMeshDetail dmesh)
        {

            List<float> param = new List<float>();

            for (int i = 0; i < dmesh.nmeshes; i++)
            {

                int startVert = dmesh.meshes[i * 4 + 0];
                int startTri = dmesh.meshes[i * 4 + 2];
                int ntris = dmesh.meshes[i * 4 + 3];

                int len = 4 + ntris * 3 * 3;

                param.Add(2);
                param.Add(len);
                param.Add(i);
                param.Add(ntris * 3);


                for (int j = 0; j < ntris; ++j)
                {

                    for (int k = 0; k < 3; k++)
                    {

                        int vertIndex = dmesh.tris[(startTri + j) * 4 + k];

                        float cellX = dmesh.verts[(startVert + vertIndex) * 3];
                        float cellY = dmesh.verts[(startVert + vertIndex) * 3 + 1];
                        float cellZ = dmesh.verts[(startVert + vertIndex) * 3 + 2];

                        param.Add(cellX);
                        param.Add(cellY);
                        param.Add(cellZ);

                    }
                }

            }

            SetNavDebug(param.ToArray());
        }

        //用于绘制计算出来的寻路结果
        public static void DrawPath(float[] path)
        {

            List<float> param = new List<float>();

            int count = path.Length/3;

            for (int i = 0; i < count - 1; i++)
            {

                int i1 = (i + 1) % count;

                param.Add(3);
                param.Add(9);
                param.Add(i);

                param.Add(path[i * 3]);
                param.Add(path[i * 3 + 1]);
                param.Add(path[i * 3 + 2]);

                param.Add(path[i1 * 3]);
                param.Add(path[i1 * 3 + 1]);
                param.Add(path[i1 * 3 + 2]);

            }

            SetNavDebug(param.ToArray());
        }

        //用于绘制计算出来的RcHeightPatch
        //public static void DrawHeightPatch(RcHeightPatch hp, RcCompactHeightfield chf)
        //{

        //    float[] hfBBMin = chf.minBounds;
        //    float cellSize = chf.cellSize;
        //    float cellHeight = chf.cellHeight;

        //    int w = hp.width;
        //    int h = hp.height;

        //    float[][] cubeList = new float[w * h][];

        //    for (int y = 0; y < h; ++y)
        //    {
        //        for (int x = 0; x < w; ++x)
        //        {
        //            int i = x + y * w;

        //            float[] spanCube = new float[4];

        //            if (hp.data[i] == RecastConfig.RC_UNSET_HEIGHT)
        //            {
        //                hp.data[i] = 50;
        //            }

        //            float cellX = hfBBMin[0] + (hp.xmin + x) * cellSize + cellSize / 2;
        //            float cellZ = hfBBMin[2] + (hp.ymin + y) * cellSize + cellSize / 2;
        //            float cellY = hfBBMin[1] + (hp.data[i]) * cellHeight + cellHeight / 2;

        //            spanCube[0] = cellX;
        //            spanCube[1] = cellY;
        //            spanCube[2] = cellZ;
        //            spanCube[3] = 7;

        //            cubeList[x + y * w] = spanCube;
        //        }
        //    }

        //    Vector3 cubeSize = new Vector3(cellSize, cellHeight, cellSize);



        //}

        public static void SetNavDebug(float[] val)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            string activeSceneName = activeScene.name;

            GameObject root = GameObject.Find("/" + activeSceneName);

            if (root.GetComponent<NavDebugComponent>() == null)
            {
                root.AddComponent<NavDebugComponent>();
            }

            root.GetComponent<NavDebugComponent>().SetDrawParam(val);
        }
    }

}

