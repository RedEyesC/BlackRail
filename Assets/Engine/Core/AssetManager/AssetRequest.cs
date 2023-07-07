
using System;
using System.Collections.Generic;

namespace GameFramework.Runtime
{
    public enum AssetRequestType
    {
        LoadOne,
        LoadAll,
        UnloadOne,
        Download,
    }

    public class AssetRequest
    {

        public int RequestID { get; set; }

        public AssetRequestType RequestType { get; private set; }
        public string AssetName { get; private set; }
        public AssetBundleInfo BundleInfo { get; private set; }

        public bool IsRunning { get; private set; }
        public bool IsSuccess { get; private set; }
        public bool IsCancel { get; set; }

        public bool AutoRelease { get; private set; }
        public bool NeedDownload { get; private set; }

        private Action<AssetRequest> TaskFinishCallBack = null;
        private RequestCallBack RequestFinishCallBack = null;

        private static HashSet<int> s_AddBundleRefRequestTypeSet = new HashSet<int>()
        {
            (int) AssetRequestType.LoadOne,

        };

        #region Initial

        public AssetRequest(AssetBundleInfo bundleInfo, string assetName, AssetRequestType type)
        {
            BundleInfo = bundleInfo;
            AssetName = assetName;

            RequestType = type;
            IsRunning = true;

            TryAddBundleRef(bundleInfo, RequestType);
        }

        private void TryAddBundleRef(AssetBundleInfo bundleInfo, AssetRequestType requestType)
        {
            if (s_AddBundleRefRequestTypeSet.Contains((int)requestType))
            {
                BundleInfo.AddRef();
            }
        }

        private void TryDelBundleRef(AssetBundleInfo bundleInfo, AssetRequestType requestType)
        {
            if (s_AddBundleRefRequestTypeSet.Contains((int)requestType))
            {
                BundleInfo.DelRef();
            }
        }

        public void Reset()
        {
            TryDelBundleRef(BundleInfo, RequestType);

            BundleInfo = null;
            TaskFinishCallBack = null;
            RequestFinishCallBack = null;

            IsRunning = false;
            IsCancel = false;
            IsSuccess = false;
            AutoRelease = false;
        }
        #endregion

        #region Excution
        public void ProcessRequest()
        {
            switch (RequestType)
            {
                case AssetRequestType.LoadOne:
                    {
                        BundleInfo.StartLoadAsset();
                        LoadAssetTask task = new LoadAssetTask(BundleInfo, this);
                        GlobalCenter.GetModule<AssetManager>().AddTask(task);
                    }
                    break;
                case AssetRequestType.UnloadOne:
                    {
                        UnLoadAssetTask task = new UnLoadAssetTask(BundleInfo, this);
                        GlobalCenter.GetModule<AssetManager>().AddTask(task);

                    }
                    break;
                default:
                    break;
            }
        }



        public void OnTaskFinish(bool success)
        {
            IsRunning = false;
            IsSuccess = success;

            if (TaskFinishCallBack != null)
                TaskFinishCallBack(this);
        }

        public void SetTaskFinishCallBack(Action<AssetRequest> callback)
        {
            TaskFinishCallBack = callback;
        }

        public void SetRequestFinishCallBack(RequestCallBack callback)
        {
            RequestFinishCallBack = callback;
        }

        public void OnRequestFinish()
        {

            if (RequestFinishCallBack != null)
            {
                RequestFinishCallBack(RequestID, IsSuccess);
                RequestFinishCallBack = null;
            }
        }

        #endregion


    }
}