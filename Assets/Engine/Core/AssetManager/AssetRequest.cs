
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
        public AssetInfo AssetInfo { get; private set; }

        public bool IsRunning { get; private set; }
        public bool IsSuccess { get; private set; }
        public bool IsCancel { get; set; }


        private RequestCallBack _TaskFinishCallBack = null;
        private Action<AssetRequest> _RequestFinishCallBack = null;


        #region Initial

        public AssetRequest(AssetInfo assetInfo, AssetRequestType type, Action<AssetRequest> callback)
        {
            AssetInfo = assetInfo;

            RequestType = type;
            _RequestFinishCallBack = callback;

            IsRunning = true;

        }

        public void Reset()
        {
            AssetInfo = null;
            _TaskFinishCallBack = null;
            _RequestFinishCallBack = null;

            IsRunning = false;
            IsCancel = false;
            IsSuccess = false;
        }
        #endregion

        #region Excution
        public void ProcessRequest()
        {
            switch (RequestType)
            {
                case AssetRequestType.LoadOne:
                    {

                        LoadAssetTask task = new LoadAssetTask(AssetInfo, this);
                        GlobalCenter.GetModule<AssetManager>().AddTask(task);
                    }
                    break;
                case AssetRequestType.UnloadOne:
                    {
                        UnLoadAssetTask task = new UnLoadAssetTask(AssetInfo, this);
                        GlobalCenter.GetModule<AssetManager>().AddTask(task);

                    }
                    break;
                default:
                    break;
            }
        }


        public void SetTaskFinishCallBack(RequestCallBack callback)
        {
            _TaskFinishCallBack = callback;
        }

        public void OnTaskFinish(bool success)
        {
            IsRunning = false;
            IsSuccess = success;

            if (_TaskFinishCallBack != null)
                _TaskFinishCallBack(RequestID, IsSuccess);
        }


        public void OnRequestFinish()
        {

            if (_RequestFinishCallBack != null)
            {
                _RequestFinishCallBack(this);
                _RequestFinishCallBack = null;
            }
        }

        #endregion


    }
}