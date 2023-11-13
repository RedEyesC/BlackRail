
using UnityEngine;

namespace GameFramework.Runtime
{

    public class UnLoadSceneTask : AssetTask
    {
        private AssetInfo _assetInfo = null;
        private AsyncOperation _asyncOperate = null;
        private AssetRequest _request = null;

        public new int taskType = (int)AssetTaskType.UnLoadAsset;
        public UnLoadSceneTask(AssetInfo assetInfo, AssetRequest req = null)
        {
            _assetInfo = assetInfo;
            _request = req;
        }

        protected override bool OnStart()
        {
            _asyncOperate = _assetInfo.UnloadScene();
            return true;
        }

        protected override bool OnUpdate()
        {
            if (_asyncOperate != null)
                return _asyncOperate.isDone;
            return true;
        }

        protected override void OnEnd()
        {
            _assetInfo.Reset();
            _request.OnTaskFinish(true);
            _request.OnRequestFinish();
        }

        protected override void OnReset()
        {
            _assetInfo = null;
            _request = null;
            _asyncOperate = null;
        }
    }
}