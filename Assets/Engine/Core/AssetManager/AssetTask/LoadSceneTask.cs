
using UnityEngine;

namespace GameFramework.Runtime
{

    public class LoadSceneTask : AssetTask
    {
        private AssetInfo _AssetInfo = null;
        private AsyncOperation _AsyncOperate = null;
        private AssetRequest _Request = null;

        private float _TimeOutTime = 0f;
        private bool _isHandleTimeOut = false;
        private float _ProcessLast = 0f;
        private float _LoadFileTimeOut = 10f;


        public LoadSceneTask(AssetInfo assetInfo, AssetRequest req)
        {
            _AssetInfo = assetInfo;
            _Request = req;
        }

        protected override bool OnStart()
        {
            if (_AssetInfo.IsLoaded)
            {
                return true;
            }

            _TimeOutTime = Time.time + _LoadFileTimeOut;
            _AsyncOperate = _AssetInfo.LoadSceneAsync(_AssetInfo.AssetName); 
            return true;

        }

        protected override bool OnUpdate()
        {

            if (_AssetInfo.IsLoaded)
            {
                return true;
            }

            return _AsyncOperate == null || _AsyncOperate.isDone;
        }

        public override bool IsTimeOut()
        {
            if ((_TimeOutTime < Time.time && _AsyncOperate.progress == _ProcessLast))
            {
                return true;
            }
            else
            {
                if (_AsyncOperate.progress != _ProcessLast)
                {
                    _TimeOutTime = Time.time + _LoadFileTimeOut;
                    _ProcessLast = _AsyncOperate.progress;
                }
                return false;
            }
        }
        protected override void OnTimeOut()
        {
            if (!_isHandleTimeOut)
            {
                _isHandleTimeOut = true;

                _AssetInfo.Reset();
            }
        }

        protected override void OnEnd()
        {
            _AssetInfo.OnSceneLoaded();
            _Request.OnTaskFinish(true);
            _Request.OnRequestFinish();
        }

        protected override void OnReset()
        {
            _AssetInfo = null;
            _AsyncOperate = null;
            _Request = null;

            _TimeOutTime = 0f;
            _isHandleTimeOut = false;
            _ProcessLast = 0f;
            _LoadFileTimeOut = 10f;
        }

        private static readonly int _TaskType = (int)AssetTaskType.LoadScene;

        public override int TaskType { get { return _TaskType; } }

    }
}