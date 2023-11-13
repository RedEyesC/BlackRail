
using UnityEngine;

namespace GameFramework.Runtime
{

    public class LoadSceneTask : AssetTask
    {
        private AssetInfo _assetInfo = null;
        private AsyncOperation _asyncOperate = null;
        private AssetRequest _request = null;

        private float _timeOutTime = 0f;
        private bool _isHandleTimeOut = false;
        private float _processLast = 0f;
        private float _loadFileTimeOut = 10f;

        public new int taskType = (int)AssetTaskType.LoadScene;

        public LoadSceneTask(AssetInfo assetInfo, AssetRequest req)
        {
            _assetInfo = assetInfo;
            _request = req;
        }

        protected override bool OnStart()
        {
            if (_assetInfo.IsLoaded)
            {
                return true;
            }

            _timeOutTime = Time.time + _loadFileTimeOut;
            _asyncOperate = _assetInfo.LoadSceneAsync(_assetInfo.assetName); 
            return true;

        }

        protected override bool OnUpdate()
        {

            if (_assetInfo.IsLoaded)
            {
                return true;
            }

            return _asyncOperate == null || _asyncOperate.isDone;
        }

        public override bool IsTimeOut()
        {
            if ((_timeOutTime < Time.time && _asyncOperate.progress == _processLast))
            {
                return true;
            }
            else
            {
                if (_asyncOperate.progress != _processLast)
                {
                    _timeOutTime = Time.time + _loadFileTimeOut;
                    _processLast = _asyncOperate.progress;
                }
                return false;
            }
        }
        protected override void OnTimeOut()
        {
            if (!_isHandleTimeOut)
            {
                _isHandleTimeOut = true;

                _assetInfo.Reset();
            }
        }

        protected override void OnEnd()
        {
            _assetInfo.OnSceneLoaded();
            _request.OnTaskFinish(true);
            _request.OnRequestFinish();
        }

        protected override void OnReset()
        {
            _assetInfo = null;
            _asyncOperate = null;
            _request = null;

            _timeOutTime = 0f;
            _isHandleTimeOut = false;
            _processLast = 0f;
            _loadFileTimeOut = 10f;
        }

    }
}