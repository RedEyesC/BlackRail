
using System.Diagnostics;

namespace GameFramework.Runtime
{
    // 强制卸载某个资源 
    // 不做引用计数
    // 用来做场景lod streaming的卸载，由那边自己做计数
    // 可能的问题：其他地方也引用了这个资源？所以要保证这个资源只在streaming中用
    public class UnLoadAssetTask : AssetTask
    {
        private AssetBundleInfo mBundleInfo = null;
        private AssetRequest mRequest = null;

        //private Stopwatch mWatch = new Stopwatch();

        public UnLoadAssetTask(AssetBundleInfo bundleInfo, AssetRequest req = null)
        {
            mBundleInfo = bundleInfo;
            mRequest = req;
        }

        protected override bool OnStart()
        {
            return true;
        }

        protected override bool OnUpdate()
        {
            return mBundleInfo.TryUnloadAsset(mRequest.AssetName);
        }

        protected override void OnEnd()
        {
 
            mRequest.OnTaskFinish(true);
        }

        protected override void OnReset()
        {
            //mWatch.Reset();
            mBundleInfo = null;
            mRequest = null;
        }

        private static readonly int mTaskType = (int)AssetTaskType.UnLoadAsset;
        private static readonly int mBanSelfRunTaskMask = (int) AssetTaskType.UnloadUnuseAsset;
        public override int TaskType { get { return mTaskType; } }
        public override int BanSelfRunTaskMask { get { return mBanSelfRunTaskMask; } }
        public override bool IsCommonTask { get { return true; } }

        public AssetRequest LoadRequest { get { return mRequest; } }
        public AssetBundleInfo BundleInfo { get { return mBundleInfo; } }
    }
}