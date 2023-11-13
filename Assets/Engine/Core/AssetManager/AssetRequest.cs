
using System;
using System.Collections.Generic;

namespace GameFramework.Runtime
{
    public enum AssetRequestType
    {
        LoadOne,
        LoadScene,
        LoadAll,
        UnloadOne,
        UnloadScene,
        Download,
    }

    public class AssetRequest
    {

        public int requestID { get; set; }

        public AssetRequestType requestType { get; private set; }
        public AssetInfo assetInfo { get; private set; }

        public bool isRunning { get; private set; }
        public bool isSuccess { get; private set; }
        public bool isCancel { get; set; }


        private RequestCallBack _taskFinishCallBack = null;
        private Action<AssetRequest> _requestFinishCallBack = null;


        #region Initial

        public AssetRequest(AssetInfo assetInfo, AssetRequestType type, Action<AssetRequest> callback)
        {
            this.assetInfo = assetInfo;

            this.requestType = type;
            this._requestFinishCallBack = callback;

            this.isRunning = true;

        }

        public void Reset()
        {
            assetInfo.UnloadAsset();
            assetInfo.Reset();
            assetInfo = null;

            _taskFinishCallBack = null;
            _requestFinishCallBack = null;

            isRunning = false;
            isCancel = false;
            isSuccess = false;
        }
        #endregion

        #region Excution
        public void ProcessRequest()
        {
            switch (requestType)
            {
                case AssetRequestType.LoadScene:
                    {

                        LoadSceneTask task = new LoadSceneTask(assetInfo, this);
                        GlobalCenter.GetModule<AssetManager>().AddTask(task);
                    }
                    break;
                case AssetRequestType.UnloadScene:
                    {
                        UnLoadSceneTask task = new UnLoadSceneTask(assetInfo, this);
                        GlobalCenter.GetModule<AssetManager>().AddTask(task);

                    }
                    break;
                case AssetRequestType.LoadOne:
                    {

                        LoadAssetTask task = new LoadAssetTask(assetInfo, this);
                        GlobalCenter.GetModule<AssetManager>().AddTask(task);
                    }
                    break;
                case AssetRequestType.UnloadOne:
                    {
                        UnLoadAssetTask task = new UnLoadAssetTask(assetInfo, this);
                        GlobalCenter.GetModule<AssetManager>().AddTask(task);

                    }
                    break;
                default:
                    break;
            }
        }


        public void SetTaskFinishCallBack(RequestCallBack callback)
        {
            _taskFinishCallBack = callback;
        }

        public void OnTaskFinish(bool success)
        {
            isRunning = false;
            isSuccess = success;

            if (_taskFinishCallBack != null)
                _taskFinishCallBack(requestID, isSuccess);
        }


        public void OnRequestFinish()
        {

            if (_requestFinishCallBack != null)
            {
                _requestFinishCallBack(this);
                _requestFinishCallBack = null;
            }
        }

        #endregion


    }
}