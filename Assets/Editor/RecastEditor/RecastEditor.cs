
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

        static readonly float WalkableSlopeAngle = 20;

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

            AREATYPE[] areas = RcMarkWalkableTriangles(WalkableSlopeAngle, mesh.vertices, mesh.triangles);
        

        }


        static AREATYPE[] RcMarkWalkableTriangles(float walkableSlopeAngle, Vector3[] verts, int[] tris)
        {

            float walkableThr = Mathf.Cos(walkableSlopeAngle / 180.0f * PI);

            int numTris = tris.Length / 3;
            AREATYPE[] areas = new AREATYPE[numTris];

            for (int i = 0; i < numTris; i++)
            {
                Vector3 norm = CommonUtility.CalcTriNormal(verts[tris[i * 3]], verts[tris[i * 3 + 1]], verts[tris[i * 3 + 2]]);
                if(norm[1] > walkableThr)
                {
                    areas[i] = AREATYPE.Walke;
                }

            }

            return areas;
        }
    }
}
