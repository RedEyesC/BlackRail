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

        private static Dictionary<string, DateTime> _timerList = new Dictionary<string, DateTime>();

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

        public static void DoStartTimer(string label)
        {
            DateTime time = DateTime.Now;

            _timerList.Add(label, time);

        }

        public static void StopTimer(string label)
        {
            if (_timerList.TryGetValue(label, out DateTime startTime))
            {
                _timerList.Remove(label);
                DateTime time = DateTime.Now;
                Debug.Log(label + " Total Time : " + (time - startTime));
            }
            else
            {
                Debug.LogWarning("Not Found " + label);
            }

        }

        public static void ResetTimers()
        {
            _timerList.Clear();
        }
    }
}

#endif
