
using UnityEngine;

namespace GameFramework.Runtime
{

    public class LoadAssetTask : AssetTask
    {
        private AssetInfo _AssetInfo = null;
        private AsyncOperation _AsyncOperate = null;
        private AssetRequest _Request = null;


        public LoadAssetTask(AssetInfo assetInfo, AssetRequest req)
        {
            _AssetInfo = assetInfo;
            _Request = req;
        }

        protected override bool OnStart()
        {
            if (_AssetInfo.IsLoaded)
            {
                _AsyncOperate = _AssetInfo.LoadAssetAsync(_AssetInfo.AssetName);
                return true;
            }
            return false;
        }

        protected override bool OnUpdate()
        {
            return _AsyncOperate == null || _AsyncOperate.isDone;
        }

        protected override void OnEnd()
        {
            _AssetInfo.OnAssetObjLoaded();
            _Request.OnTaskFinish(_AssetInfo.IsAssetObjLoaded());
            _Request.OnRequestFinish();
        }

        protected override void OnReset()
        {
            _AssetInfo = null;
            _AsyncOperate = null;
            _Request = null;
        }

        private static readonly int mTaskType = (int)AssetTaskType.LoadAsset;

        public override int TaskType { get { return mTaskType; } }

    }
}