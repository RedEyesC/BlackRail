
using UnityEngine;

namespace GameFramework.Runtime
{

    public class LoadAssetTask : AssetTask
    {
        private AssetBundleInfo mBundleInfo = null;
        private AsyncOperation mAsyncOperate = null;
        private AssetRequest mRequest = null;

     
        public LoadAssetTask(AssetBundleInfo bundleInfo, AssetRequest req)
        {
            mBundleInfo = bundleInfo;
            mRequest = req;
        }

        protected override bool OnStart()
        {
            if (mBundleInfo.IsLoaded && mBundleInfo.IsDependencesLoaded)
            {
                mAsyncOperate = mBundleInfo.LoadAssetAsync(mRequest.AssetName);
                return true;
            }
            return false;
        }

        protected override bool OnUpdate()
        {
            return mAsyncOperate == null || mAsyncOperate.isDone;
        }

        protected override void OnEnd()
        {
           
            mBundleInfo.OnAssetObjLoaded(mRequest.AssetName);
            mRequest.OnTaskFinish(mBundleInfo.IsAssetObjLoaded(mRequest.AssetName));
        }

        protected override void OnReset()
        {
            mBundleInfo = null;
            mAsyncOperate = null;
            mRequest = null;
        }

        private static readonly int mTaskType = (int)AssetTaskType.LoadAsset;
        private static readonly int mBanSelfRunTaskMask = (int) AssetTaskType.UnloadUnuseAsset;
        public override int TaskType { get { return mTaskType; } }
        public override int BanSelfRunTaskMask { get { return mBanSelfRunTaskMask; } }
        public override bool IsCommonTask { get { return true; } }

        public AssetRequest LoadRequest { get { return mRequest; } }
        public AssetBundleInfo BundleInfo { get { return mBundleInfo; } }
    }
}