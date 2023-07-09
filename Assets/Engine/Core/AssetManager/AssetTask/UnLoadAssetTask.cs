
using System.Diagnostics;

namespace GameFramework.Runtime
{
    // 强制卸载某个资源 
    // 不做引用计数
    // 用来做场景lod streaming的卸载，由那边自己做计数
    // 可能的问题：其他地方也引用了这个资源？所以要保证这个资源只在streaming中用
    public class UnLoadAssetTask : AssetTask
    {
        private AssetInfo _AssetInfo = null;
        private AssetRequest _Request = null;

        //private Stopwatch mWatch = new Stopwatch();

        public UnLoadAssetTask(AssetInfo assetInfo, AssetRequest req = null)
        {
            _AssetInfo = assetInfo;
            _Request = req;
        }

        protected override bool OnStart()
        {
            return true;
        }

        protected override bool OnUpdate()
        {
            return _AssetInfo.TryUnloadAsset();
        }

        protected override void OnEnd()
        {

            _Request.OnTaskFinish(true);
        }

        protected override void OnReset()
        {
            //mWatch.Reset();
            _AssetInfo = null;
            _Request = null;
        }

        private static readonly int mTaskType = (int)AssetTaskType.UnLoadAsset;

        public override int TaskType { get { return mTaskType; } }

    }
}