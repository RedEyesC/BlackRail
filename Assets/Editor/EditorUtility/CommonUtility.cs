using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
namespace GameEditor.Utility
{
    public class CommonUtility
    {
 
        private static Dictionary<string,DateTime> _timerList = new Dictionary<string, DateTime>();  

        public static void CreateAsset(UnityEngine.Object asset, string path)
        {
            var oldAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
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
            UnityEngine.Object destOldAsset = AssetDatabase.LoadAssetAtPath(destPath, typeof(UnityEngine.Object)) as UnityEngine.Object;
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
                path = path[0..^1];
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

            PrefabUtility.SaveAsPrefabAsset(go, localPath);
        }


        public static void DoStartTimer(string label)
        {
            DateTime time = DateTime.Now;

            _timerList.Add(label, time);

        }

        public static void StopTimer(string label)
        {
            if(_timerList.TryGetValue(label, out DateTime startTime))
            {
                _timerList.Remove(label);
                DateTime time = DateTime.Now;
                Debug.Log(label + " Total Time : " + (time - startTime));
            }
            else
            {
                Debug.LogWarning("Not Found "+label);
            }

        }

        public static void ResetTimers()
        {
            _timerList.Clear();
        }
    }
}

#endif
