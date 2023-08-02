using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor.Animations;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
namespace GameEditor
{
    public enum RcAxis
    {
        AXIS_X = 0,
        AXIS_Y = 1,
        AXIS_Z = 2
    };

    public enum VertIndex
    {
        In = 0,
        InRow = 7,
        P1 = 14,
        P2 = 21,
    };

    public class CommonUtility
    {
        static Vector3 tempVec1 = new Vector3();
        static Vector3 tempVec2 = new Vector3();

        public static void CreateAsset(Object asset, string path)
        {
            var oldAsset = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (oldAsset)
            {
                EditorUtility.CopySerialized(asset, oldAsset);
                EditorUtility.SetDirty(oldAsset);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            }
            else
            {
                AssetDatabase.CreateAsset(asset, path);
            }
        }

        public static void CopyAsset(string srcPath, string destPath)
        {
            if (srcPath == destPath)
            {
                EditorUtility.DisplayDialog("error", string.Format("{0} srcPath==destPath", srcPath), "ok");
                throw new System.IO.IOException();
            }
            Object destOldAsset = AssetDatabase.LoadAssetAtPath(destPath, typeof(Object)) as Object;
            if (destOldAsset == null)
            {
                AssetDatabase.CopyAsset(srcPath, destPath);
            }
            else
            {
                if (File.Exists(destPath))
                {
                    File.Copy(srcPath, destPath, true);
                }
                else
                {
                    FileUtil.DeleteFileOrDirectory(destPath);
                    FileUtil.CopyFileOrDirectory(srcPath, destPath);
                }
                AssetDatabase.ImportAsset(destPath);
            }
        }

        public static void CreateFolder(string path)
        {
            if (path.EndsWith("/"))
            {
                path = path.Substring(0, path.Length - 1);
            }
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }
            var parentPath = Path.GetDirectoryName(path);
            CreateFolder(parentPath);
            AssetDatabase.CreateFolder(parentPath, Path.GetFileName(path));
        }

        public static string CombinePath(string path_1, string path_2)
        {
            return Path.Combine(path_1, path_2).Replace('\\', '/');
        }
        public static void CreatePrefab(GameObject go, string path)
        {
            CreateFolder(path);
            string localPath = path + go.name + ".prefab";

            // Make sure the file name is unique, in case an existing Prefab has the same name.
            //localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);

            // Create the new Prefab and log whether Prefab was saved successfully.
            PrefabUtility.SaveAsPrefabAsset(go, localPath);
        }


        #region Recast
        public static Vector3 CalcTriNormal(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            tempVec1 = v1 - v0;
            tempVec2 = v2 - v0;

            return Vector3.Normalize(Vector3.Cross(tempVec1, tempVec2));
        }

        public static void CalcBounds(Vector3[] verts, out Vector3 minBounds, out Vector3 maxBounds)
        {
            minBounds = verts[0];
            maxBounds = verts[0];
            for (int i = 1; i < verts.Length; i++)
            {
                minBounds = Vector3.Min(minBounds, verts[i]);
                maxBounds = Vector3.Max(maxBounds, verts[i]);
            }
        }

        public static void CalcGridSize(Vector3 minBounds, Vector3 maxBounds, float cellSize, out int sizeX, out int sizeZ)
        {
            sizeX = (int)((maxBounds[0] - minBounds[0]) / cellSize + 0.5f);
            sizeZ = (int)((maxBounds[2] - minBounds[2]) / cellSize + 0.5f);
        }

        //a的包围盒包含于b的包围盒 或者 a的包围盒与b的包围盒相交
        public static bool OverlapBounds(Vector3 aMin, Vector3 aMax, Vector3 bMin, Vector3 bMax)
        {
            return aMin[0] <= bMax[0] && aMax[0] >= bMin[0] &&
                aMin[1] <= bMax[1] && aMax[1] >= bMin[1] &&
                aMin[2] <= bMax[2] && aMax[2] >= bMin[2];
        }

        public static void DividePoly(Vector3[] VertList, int inVertsCount, float axisOffset, RcAxis axis, out int outVerts1Count, out int outVerts2Count, out Vector3[] outVertList1, out Vector3[] outVertList2)
        {

            Vector3[] outList1 = new Vector3[7];
            Vector3[] outList2 = new Vector3[7];
            float[] inVertAxisDelta = new float[12];
            //多边形顶点到切割线的距离
            for (int inVert = 0; inVert < inVertsCount; ++inVert)
            {
                inVertAxisDelta[inVert] = axisOffset - VertList[inVert][(int)axis];
            }


            int poly1Vert = 0;
            int poly2Vert = 0;
            for (int inVertA = 0, inVertB = inVertsCount - 1; inVertA < inVertsCount; inVertB = inVertA, ++inVertA)
            {
                // 通过与切割线的距离判断是否都是同一侧的
                bool sameSide = (inVertAxisDelta[inVertA] >= 0) == (inVertAxisDelta[inVertB] >= 0);

                if (!sameSide)
                {
                    // b的距离占a和b距离之和的比例 ，因为a，b不在同一侧，a，b距离之和等于b-a
                    float s = inVertAxisDelta[inVertB] / (inVertAxisDelta[inVertB] - inVertAxisDelta[inVertA]);

                    //计算出中间点坐标
                    outList1[poly1Vert] = VertList[inVertB] + (VertList[inVertA] - VertList[inVertB]) * s;
                    outList2[poly2Vert] = outList1[poly1Vert];

                    poly1Vert++;
                    poly2Vert++;


                    //根据a点距离把a点添加到划分好的三角形
                    if (inVertAxisDelta[inVertA] > 0)
                    {
                        outList1[poly1Vert] = VertList[inVertA];
                        poly1Vert++;
                    }
                    else if (inVertAxisDelta[inVertA] < 0)
                    {
                        outList2[poly2Vert] = VertList[inVertA];
                        poly2Vert++;
                    }
                }
                else
                {

                    if (inVertAxisDelta[inVertA] >= 0)
                    {
                        outList1[poly1Vert] = VertList[inVertA];
                        poly1Vert++;
                        if (inVertAxisDelta[inVertA] != 0)
                        {
                            continue;
                        }
                    }
                    outList2[poly2Vert] = VertList[inVertA];
                    poly2Vert++;
                }
            }

            outVerts1Count = poly1Vert;
            outVerts2Count = poly2Vert;

            outVertList1 = outList1;
            outVertList2 = outList2;

        }

        public static int RcGetDirOffsetX(int direction)
        {
            int[] offset = { -1, 0, 1, 0 };
            return offset[direction & 0x03];
        }

        public static int RcGetDirOffsetY(int direction)
        {
            int[] offset = { 0, 1, 0, -1 };
            return offset[direction & 0x03];
        }

        #endregion
    }
}

#endif
