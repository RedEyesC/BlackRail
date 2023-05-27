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
    public class CommonUtility
    {

        public static void CreateAssetEx(Object asset, string path)
        {
            var oldAsset = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (oldAsset)
            {
                var tempDir = "Assets/TempCreate";
                var tempAssetPath = tempDir + "/" + Path.GetFileName(path);
                CreateFolder(tempDir);
                AssetDatabase.CreateAsset(asset, tempAssetPath);
                CopyAsset(tempAssetPath, path);
                AssetDatabase.DeleteAsset(tempAssetPath);
                AssetDatabase.DeleteAsset(tempDir);
            }
            else
            {
                AssetDatabase.CreateAsset(asset, path);
            }
        }

        public static void CreateAssetEx2(Object asset, string path)
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
            localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);

            // Create the new Prefab and log whether Prefab was saved successfully.
            PrefabUtility.SaveAsPrefabAsset(go, localPath);
        }
    }
}

#endif
