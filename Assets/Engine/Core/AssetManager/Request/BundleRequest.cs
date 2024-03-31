using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GameFramework.Runtime
{
    public sealed class BundleRequest : LoadRequest
    {
        internal AssetBundle assetBundle { get; private set; }
        public AssetBundleRequest assetBundleRequest;
        public string bundleName;

        public bool loadIsDone = false;
        public float resProgress = 0.0f;
        public Object[] loadAssetObjs = null;

        private ResMod resmod = ResMod.Raw;
        public override int priority => 0;

        protected override void OnStart()
        {
            LoadAssetBundle();
        }

        protected override void OnUpdated()
        {

        }

        protected override void OnWaitForCompletion()
        {

        }

        public void LoadAssetBundle()
        {
            if (resmod == ResMod.Bundle)
            {
                assetBundle = AssetBundle.LoadFromFile(path);
                if (assetBundle == null)
                {
                    SetResult(Result.Failed, $"assetBundle == null, {bundleName}");
                    return;
                }
            }

            progress = 1;
            SetResult(Result.Success);
        }

        protected override void OnDispose()
        {
            loadIsDone = false;
            resProgress = 0f;
            loadAssetObjs = null;

            Remove(this);
            if (assetBundle == null) return;
            assetBundle.Unload(true);
            assetBundle = null;
        }

        #region Internal

        private static readonly Queue<BundleRequest> Unused = new Queue<BundleRequest>();
        public static readonly Dictionary<string, BundleRequest> Loaded = new Dictionary<string, BundleRequest>();

        private static void Remove(BundleRequest request)
        {
            Loaded.Remove(request.bundleName);
            Unused.Enqueue(request);
        }

        internal static BundleRequest Load(string bundleName)
        {
            if (!Loaded.TryGetValue(bundleName, out var request))
            {
                request = Unused.Count > 0 ? Unused.Dequeue() : new BundleRequest();
                request.bundleName = bundleName;
                request.Reset();
                Loaded[bundleName] = request;
            }

            request.LoadAsync();
            return request;
        }


        public float loadProgress
        {
            get
            {
                if (resmod == ResMod.Raw)
                    return resProgress;
                else
                {
                    if (assetBundleRequest == null)
                    {
                        return 1f;
                    }
                    return assetBundleRequest.progress;
                }
            }
        }
        #endregion


        #region load
        internal Object[] LoadAllAssets()
        {

            if (resmod == ResMod.Bundle)
            {
                return assetBundle.LoadAllAssets();
            }
            else
            {
                return LoadAssetByRes();
            }
        }

        internal Object LoadAsset(string asstName)
        {
            if (resmod == ResMod.Bundle)
            {
                return assetBundle.LoadAsset(asstName);
            }
            else
            {
                return LoadAssetByRes(asstName)[0];
            }
        }

        internal void LoadAllAssetsAsync()
        {
            if (resmod == ResMod.Bundle)
            {
                AppInterface.StartCoroutine(LoadAssetByBundleAsync());
            }
            else
            {
                AppInterface.StartCoroutine(LoadAssetByResAsync());
            }
        }

        internal void LoadAssetAsync(string asstName)
        {
            if (resmod == ResMod.Bundle)
            {
                AppInterface.StartCoroutine(LoadAssetByBundleAsync(asstName));
            }
            else
            {
                AppInterface.StartCoroutine(LoadAssetByResAsync(asstName));
            }
        }

        internal Object[] LoadAssetByRes(string asstName = null)
        {
            string resPath = bundleName.Remove(bundleName.LastIndexOf(".")) + "/";

            List<Object> objList = new List<Object>();
            List<string> fileList = getFileList(asstName);


            for (int i = 0; i < fileList.Count; i++)
            {
                Object obj = Resources.Load(resPath + fileList[i]);
                if (obj != null)
                    objList.Add(obj);
            }

            return objList.ToArray();
        }

        internal IEnumerator LoadAssetByResAsync(string asstName = null)
        {
            string resPath = bundleName.Remove(bundleName.LastIndexOf(".")) + "/";

            List<Object> objList = new List<Object>();
            List<string> fileList = getFileList(asstName);

            yield return null;

            ResourceRequest req = null;
            for (int i = 0; i < fileList.Count; i++)
            {
                req = Resources.LoadAsync(resPath + fileList[i]);
                yield return req;

                if (req.asset != null)
                    objList.Add(req.asset);

                resProgress = i / fileList.Count;
            }

            loadAssetObjs = objList.ToArray();
            loadIsDone = true;
            resProgress = 1f;
        }

        private List<string> getFileList(string asstName = null)
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


        private IEnumerator LoadAssetByBundleAsync(string assetName = null)
        {
            if (assetBundle != null)
            {
                if(assetName != null)
                {
                    assetBundleRequest = assetBundle.LoadAllAssetsAsync();
                    yield return assetBundleRequest;

                    if (assetBundleRequest.allAssets != null)
                        loadAssetObjs = assetBundleRequest.allAssets;
                }
                else
                {
                    assetBundleRequest = assetBundle.LoadAssetAsync(assetName);
                    yield return assetBundleRequest;

                    if (assetBundleRequest.asset != null)
                        loadAssetObjs = new Object[] { assetBundleRequest.asset };
                }

            }

            loadIsDone = true;
        }

        #endregion load

    }
}