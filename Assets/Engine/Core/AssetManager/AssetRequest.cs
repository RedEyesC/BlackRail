
using System;
using System.Collections.Generic;

namespace GameFramework.Runtime
{
    public class AssetRequest
    {
   
        public enum AssetRequestType
        {
            LoadOne,
            UnloadOne,
            Download,
        }

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
        private Action<int, bool> RequestFinishCallBack = null;
        
        private static HashSet<int> s_AddBundleRefRequestTypeSet = new HashSet<int>()
        {
            (int) AssetRequestType.LoadOne,
        
        };

        #region Initial

        public void InitLoadAssetRequest(AssetBundleInfo bundleInfo, string assetName, bool needDownload)
        {
            BundleInfo = bundleInfo;
            AssetName = assetName;
            NeedDownload = needDownload;
            RequestType = AssetRequestType.LoadOne;
            IsRunning = true;

            TryAddBundleRef(bundleInfo, RequestType);
        }
        
        public void InitUnLoadAssetRequest(AssetBundleInfo bundleInfo, string assetName)
        {
            BundleInfo = bundleInfo;
            AssetName = assetName;
            RequestType = AssetRequestType.UnloadOne;
            IsRunning = true;
            AutoRelease = true;
            
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
                        AssetManager.Instance.AddTask(task);
                    }
                    break;
                case AssetRequestType.UnloadOne:
                {
                    UnLoadAssetTask task = new UnLoadAssetTask(BundleInfo, this);
                    AssetManager.Instance.AddTask(task);
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
            //UnityEngine.Debug.LogFormat("AssetRequest  OnTaskFinish 11111111 " + success.ToString());

            if (TaskFinishCallBack != null)
                TaskFinishCallBack(this);
        }

        public void SetTaskFinishCallBack(Action<AssetRequest> callback)
        {
            TaskFinishCallBack = callback;
        }

        public void SetRequestFinishCallBack(Action<int, bool> callback)
        {
            RequestFinishCallBack = callback;
        }

        public void OnRequestFinish()
        {
            //UnityEngine.Debug.Log("AssetRequest UpdateFinishRequest " + RequestID + "   " + AssetName);

            if (RequestFinishCallBack != null)
            {
                //UnityEngine.Debug.Log("AssetRequest UpdateFinishRequest 4444444444" + RequestID);
                RequestFinishCallBack(RequestID, IsSuccess);
                RequestFinishCallBack = null;
            }
        }

        #endregion


    }
}