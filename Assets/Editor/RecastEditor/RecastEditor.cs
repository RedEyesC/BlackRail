
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameEditor

{
    public class RecastEditor
    {
        protected enum AREATYPE
        {
            Walke,
        }

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

            RecastConfig config = new RecastConfig(mesh, WalkableSlopeAngle, WalkableClimb, CellSize, CellHeight);


            AREATYPE[] areas = RcMarkWalkableTriangles(config.WalkableSlopeAngle, mesh.vertices, mesh.triangles);

            RcRasterizeTriangles(mesh.vertices, mesh.triangles, areas, config);

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
        static void RcRasterizeTriangles(Vector3[] verts, int[] tris, AREATYPE[] areas, RecastConfig config)
        {

            int numTris = tris.Length / 3;
            for (int i = 0; i < numTris; i++)
            {
                Vector3 vert1 = verts[tris[i * 3]];
                Vector3 vert2 = verts[tris[i * 3 + 1]];
                Vector3 vert3 = verts[tris[i * 3 + 2]];

                if (!RasterizeTri(vert1, vert2, vert3, areas[i],config))
                {
                    Debug.LogError("rcRasterizeTriangles: Out of memory.");
                }

            }

        }

        static bool RasterizeTri(Vector3 v0, Vector3 v1, Vector3 v2, AREATYPE areaType, RecastConfig config)
        {
            Vector3 triBBMin = v0;
            triBBMin = Vector3.Min(triBBMin, v1);
            triBBMin = Vector3.Min(triBBMin, v2);

            Vector3 triBBMax = v0;
            triBBMax = Vector3.Max(triBBMax, v1);
            triBBMax = Vector3.Max(triBBMax, v2);

            Vector3 hfBBMin = config.minBounds;
            Vector3 hfBBMax = config.maxBounds;

            float CellSize = config.CellSize;
            // 三角形的包围盒不在目标包围盒范围内就放弃这个三角形
            if (!CommonUtility.OverlapBounds(triBBMin, triBBMax, hfBBMin, hfBBMax))
            {
                return true;
            }

            //在xz平面上，以体素为单位的长度，z轴的最大值和最小值。以包围盒的z为0点,
            int z0 = (int)((triBBMin[2] - hfBBMin[2]) / CellSize);
            int z1 = (int)((triBBMax[2] - hfBBMin[2]) / CellSize);

            // 案例里写着·使用-1比0 更好的平铺三角形？？，为什么呢
            z0 = Mathf.Clamp(z0, -1, config.Height);
            z1 = Mathf.Clamp(z1, 0, config.Height);


            
            Vector3[] buff = new Vector3[7 * 4];

            int nvRow = 0;
            int nvIn = 3;
            //按z轴开始切割
            for (int z = z0; z <= z1; ++z)
            {
                
                float cellZ = hfBBMin[2] + (float)z * CellSize;
                CommonUtility.DividePoly(buff, nvIn, out nvRow, out nvIn, cellZ + CellSize, RcAxis.AXIS_X);

            }
                return true;
        }

    }


    public class RecastConfig
    {
        public float WalkableSlopeAngle = 0;
        public float WalkableClimb = 0;
        public float CellSize = 0;
        public float CellHeight = 0;

        public int Width = 0;
        public int Height = 0;

        public Vector3 minBounds = new Vector3();
        public Vector3 maxBounds = new Vector3();

        public RecastConfig(Mesh mesh,float walkableSlopeAngle, float walkableClimb, float cellSize, float cellHeight)
        {
            WalkableClimb = walkableClimb;
            CellSize = cellSize;    
            CellHeight = cellHeight;    
            WalkableSlopeAngle = walkableSlopeAngle;    
            WalkableClimb = walkableClimb;

            CommonUtility.CalcBounds(mesh.vertices, out minBounds, out maxBounds);

            CommonUtility.CalcGridSize(minBounds,maxBounds, CellSize, out Width,out Height);

        }
    }
}
