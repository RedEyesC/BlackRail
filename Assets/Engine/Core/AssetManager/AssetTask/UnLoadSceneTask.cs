
using UnityEngine;

namespace GameFramework.Runtime
{

    public class UnLoadSceneTask : AssetTask
    {
        private AssetInfo _AssetInfo = null;
        private AsyncOperation _AsyncOperate = null;
        private AssetRequest _Request = null;

        public UnLoadSceneTask(AssetInfo assetInfo, AssetRequest req = null)
        {
            _AssetInfo = assetInfo;
            _Request = req;
        }

        protected override bool OnStart()
        {
            _AsyncOperate = _AssetInfo.UnloadScene();
            return true;
        }

        protected override bool OnUpdate()
        {
            if (_AsyncOperate != null)
                return _AsyncOperate.isDone;
            return true;
        }

        protected override void OnEnd()
        {
            _AssetInfo.Reset();
            _Request.OnTaskFinish(true);
            _Request.OnRequestFinish();
        }

        protected override void OnReset()
        {
            _AssetInfo = null;
            _Request = null;
            _AsyncOperate = null;
        }

        private static readonly int _TaskType = (int)AssetTaskType.UnLoadAsset;

        public override int TaskType { get { return _TaskType; } }

    }
}