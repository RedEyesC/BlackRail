using UnityEngine;
using System.Collections.Generic;


namespace GameFramework.Runtime
{
    public class AssetBundleInfo
    {
        public enum AssetState
        {
            Unload,
            Loading,
            Loaded,
        }

        public enum DownloadState
        {
            None,
            Downloading,
            Downloaded,
        }

        public string AssetBundleName { get; private set; }
        public AssetState State { get; private set; }

        private int mRefCount = 0;
        private bool mResourceMode = false;
        private List<AssetBundleInfo> mDirDepsBundleList = new List<AssetBundleInfo>();
        
        private Dictionary<string, Object> mAssetObjMap = new Dictionary<string, Object>();


        private Dictionary<string, AsyncOperation> mAssetRequestMap = new Dictionary<string, AsyncOperation>();

        private static int sAssetBundleNum = 0;
        public static int AssetBundleNum { get { return sAssetBundleNum; } }

        private DownloadState mDownloadState = DownloadState.None;
        private bool mDownloadSuccess = false;

        public AssetBundleInfo(string name, bool resourceMode)
        {
            State = AssetState.Unload;
            AssetBundleName = name;
            mResourceMode = resourceMode;
        }

        public void InitDependency(List<string> depsList)
        {
            if (depsList != null && depsList.Count > 0)
            {
                AssetManager mgr = GlobalCenter.GetModule<AssetManager>();
                for (int i = 0; i < depsList.Count; i++)
                {
                    AssetBundleInfo info = mgr.GetBundleInfo(depsList[i]);
                    mDirDepsBundleList.Add(info);
                }
            }
        }

        public void ClearDependency()
        {
            mDirDepsBundleList.Clear();
        }

        public void ResetDownloadState()
        {
            mDownloadState = DownloadState.None;
            mDownloadSuccess = false;
        }

        #region Get Asset
        public bool IsAssetObjLoaded(string name)
        {
            if (mAssetObjMap != null)
                return mAssetObjMap.ContainsKey(name);
            return false;
        }

        public Object GetAssetObj(string name)
        {
            if (mAssetObjMap != null)
            {
                Object obj = null;
                mAssetObjMap.TryGetValue(name, out obj);
                return obj;
            }
            return null;
        }

        public T GetAssetObjWithType<T>(string name) where T : class
        {
            if (mAssetObjMap != null)
            {
                Object obj = null;
                if (mAssetObjMap.TryGetValue(name, out obj))
                    return obj as T;
            }
            return null;
        }

        #endregion

        #region reference
        public void AddRef()
        {
            ++mRefCount;
            if (mRefCount == 1)
            {
                for (int i = 0; i < mDirDepsBundleList.Count; i++)
                {
                    mDirDepsBundleList[i].AddRef();
                }
            }
        }

        public void DelRef()
        {
            --mRefCount;
            if (mRefCount == 0)
            {
                StartUnloadAsset();

                for (int i = 0; i < mDirDepsBundleList.Count; i++)
                {
                    mDirDepsBundleList[i].DelRef();
                }
            }
        }

        public void DelAssetRef(string assetName)
        {
            Object obj = null;
            if (mAssetObjMap.TryGetValue(assetName, out obj))
            {
                mAssetObjMap.Remove(assetName);

                if (obj is AudioClip)
                    ((AudioClip)obj).UnloadAudioData();
            }

            if (mAssetRequestMap.ContainsKey(assetName))
            {
                mAssetRequestMap.Remove(assetName);
            }
        }

        #endregion

        #region Asset Operate

        public AsyncOperation LoadAssetAsync(string assetName)
        {
            AsyncOperation oper = null;
            if (mAssetRequestMap.TryGetValue(assetName, out oper))
            {
                return oper;
            }
            else
            {

                string path = AssetBundleName.Remove(AssetBundleName.LastIndexOf(".")) + "/" + assetName;
                oper = Resources.LoadAsync(path);


                mAssetRequestMap.Add(assetName, oper);
            }

            return oper;
        }

        public void OnAssetObjLoaded(string assetName)
        {
            if (!mAssetObjMap.ContainsKey(assetName))
            {
                AsyncOperation oper = null;
                if (mAssetRequestMap.TryGetValue(assetName, out oper))
                {
                    Object obj = null;

                    if (mResourceMode)
                    {
                        ResourceRequest req = oper as ResourceRequest;
                        if (req != null)
                            obj = req.asset;
                    }
                    else
                    {
                        AssetBundleRequest req = oper as AssetBundleRequest;
                        if (req != null)
                            obj = req.asset;
                    }
                    mAssetObjMap.Add(assetName, obj);
                }
            }
        }

        public bool TryUnloadAsset(string assetName)
        {
            if (mRefCount > 0)
                return true;

            switch (State)
            {
                case AssetState.Unload:
                    return true;
                case AssetState.Loading:
                    return false;
                case AssetState.Loaded:
                    UnloadSelf();
                    return true;
            }

            return false;
        }

        public void UnloadSelf()
        {
            if (State != AssetState.Loaded)
            {
                Debug.LogErrorFormat("Invalid Unload Bundle {0}", AssetBundleName);
            }

            UnityEngine.Object obj;
            if (mAssetObjMap.TryGetValue(AssetBundleName, out obj))
            {
                mAssetObjMap.Remove(AssetBundleName);
                if (obj)
                {
                    Resources.UnloadAsset(obj);
                }
            }
            State = AssetState.Unload;
            mAssetObjMap.Clear();
            mAssetRequestMap.Clear();
            --sAssetBundleNum;
        }

        public bool IsDependencesLoaded
        {
            get
            {
                for (int i = 0; i < mDirDepsBundleList.Count; i++)
                {
                    if (!mDirDepsBundleList[i].IsLoaded)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public bool IsLoaded
        {
            get
            {
                return State == AssetState.Loaded;
            }
        }


        #endregion

        #region Asset Task

        public void StartLoadAsset(bool needDownload = true, AssetRequest req = null)
        {
            switch (State)
            {
                case AssetState.Unload:
                    State = AssetState.Loading;
                    for (int i = 0; i < mDirDepsBundleList.Count; i++)
                    {
                        mDirDepsBundleList[i].StartLoadAsset(needDownload,req);
                    }

                    LoadAssetTask task = new LoadAssetTask(this,req);
                    GlobalCenter.GetModule<AssetManager>().AddTask(task);
                    break;
                case AssetState.Loading:
                    break;
                case AssetState.Loaded:
                    break;
                default:
                    break;
            }
        }

        private void StartUnloadAsset()
        {
            UnLoadAssetTask task = new UnLoadAssetTask(this);
            GlobalCenter.GetModule<AssetManager>().AddTask(task);
        }
    }
    #endregion
}