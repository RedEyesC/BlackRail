
using UnityEngine;

namespace GameFramework.Runtime
{

    public class UnloadUnuseAssetTask : AssetTask
    {
        private AsyncOperation mAsyncOperate = null;

        public UnloadUnuseAssetTask()
        {
        }

        protected override bool OnStart()
        {
            mAsyncOperate = Resources.UnloadUnusedAssets();
            return true;
        }

        protected override bool OnUpdate()
        {
            if (mAsyncOperate != null)
                return mAsyncOperate.isDone;
            return true;
        }

        protected override void OnEnd()
        {

        }

        protected override void OnReset()
        {
            mAsyncOperate = null;
        }

        private static readonly int mTaskType = (int)AssetTaskType.UnloadUnuseAsset;
        private static readonly int mBanSelfRunTaskMask = 0;
        public override int TaskType { get { return mTaskType; } }
        public override int BanSelfRunTaskMask { get { return mBanSelfRunTaskMask; } }
        public override bool IsCommonTask { get { return true; } }
    }
}