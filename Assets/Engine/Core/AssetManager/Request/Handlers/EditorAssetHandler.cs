using GameFramework.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFramework.Asset
{
    public class EditorAssetHandler : IAssetHandler
    {
        private enum Step
        {
            LoadDependencies,
            LoadAsset
        }

        private Step _step;

        public bool loadIsDone;
        public float resProgress;
        public Object[] loadAssetObjs;

        public void OnStart(AssetRequest request)
        {
            _step = Step.LoadDependencies;
            loadIsDone = false;
            resProgress = 0.0f;
        }

        public void Update(AssetRequest request)
        {
            switch (_step)
            {
                case Step.LoadDependencies:
                    _step = Step.LoadAsset;
                    AppInterface.StartCoroutine(LoadAssetByResAsync(request));
                    break;

                case Step.LoadAsset:
                    request.progress = 0.5f + resProgress * 0.5f;
                    if (!loadIsDone) return;
                    SetResult(request);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SetResult(AssetRequest request)
        {
            if (request.isAll)
            {
                Dictionary<string, Object> dict = new Dictionary<string, Object>();
                foreach (Object obj in loadAssetObjs)
                {
                    dict.Add(obj.name, obj);
                }
                request.assets = dict;

                if (request.assets == null)
                {
                    request.SetResult(Request.Result.Failed, "assets == null");
                    return;
                }
            }
            else
            {
                request.asset = loadAssetObjs[0];
                if (request.asset == null)
                {
                    request.SetResult(Request.Result.Failed, "asset == null");
                    return;
                }
            }

            request.SetResult(Request.Result.Success);
        }

        public void Dispose(AssetRequest request)
        {
            loadIsDone = false;
            resProgress = 0.0f;
            loadAssetObjs = null;
        }

        public void WaitForCompletion(AssetRequest request)
        {
            if (request.result == Request.Result.Failed) return;
            //  特殊处理，防止异步转同步卡顿。
            if (resProgress == 0.0f)
                LoadAssetByRes(request);
            else
                SetResult(request);
        }


        public static IAssetHandler CreateInstance()
        {
            return new EditorAssetHandler();
        }


        internal Object[] LoadAssetByRes(AssetRequest request)
        {
            string bundleName = request.bundleName;
            string assetName = request.assetName;

            string resPath = bundleName.Remove(bundleName.LastIndexOf(".")) + "/";

            List<Object> objList = new List<Object>();
            List<string> fileList = GetFileList(bundleName, assetName);


            for (int i = 0; i < fileList.Count; i++)
            {
                Object obj = Resources.Load(resPath + fileList[i]);
                if (obj != null)
                    objList.Add(obj);
            }

            return objList.ToArray();
        }

        internal IEnumerator LoadAssetByResAsync(AssetRequest request)
        {
            string bundleName = request.bundleName;
            string assetName = request.assetName;

            string resPath = bundleName.Remove(bundleName.LastIndexOf(".")) + "/";

            List<Object> objList = new List<Object>();
            List<string> fileList = GetFileList(bundleName, assetName);

            yield return null;

            ResourceRequest req = null;
            for (int i = 0; i < fileList.Count; i++)
            {

                resProgress = i / fileList.Count;

                req = Resources.LoadAsync(resPath + fileList[i]);
                yield return req;

                if (req.asset != null)
                    objList.Add(req.asset);

            }

            loadAssetObjs = objList.ToArray();
            loadIsDone = true;
            resProgress = 1.0f;

        }

        private List<string> GetFileList(string bundleName, string asstName = null)
        {
            string resPath = bundleName.Remove(bundleName.LastIndexOf(".")) + "/";
            string filePath = "Assets/Resources/" + resPath;

            List<string> fileList = new List<string>();
            DirectoryInfo dirInfo = new DirectoryInfo(filePath);
            foreach (var item in dirInfo.GetFiles())
            {
                if (item.Extension == ".meta")
                {
                    continue;
                }

                string name = Path.GetFileNameWithoutExtension(item.Name);
                if (asstName != null)
                {
                    if (name == asstName)
                    {
                        fileList.Add(name);
                        break;
                    }
                }
                else
                {
                    fileList.Add(name);
                }
            }

            return fileList;
        }
    }
}