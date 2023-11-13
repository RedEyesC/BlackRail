namespace GameFramework.Runtime
{

    public class UnLoadAssetTask : AssetTask
    {
        private AssetInfo _assetInfo = null;
        private AssetRequest _request = null;

        public UnLoadAssetTask(AssetInfo assetInfo, AssetRequest req = null)
        {
            _assetInfo = assetInfo;
            _request = req;
        }

        protected override bool OnStart()
        {
            return true;
        }

        protected override bool OnUpdate()
        {
            return _assetInfo.UnloadAsset();
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
        }

        private static readonly int _TaskType = (int)AssetTaskType.UnLoadAsset;

    }
}