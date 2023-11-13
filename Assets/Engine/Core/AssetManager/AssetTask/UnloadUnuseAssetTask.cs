
using UnityEngine;

namespace GameFramework.Runtime
{

    public class UnloadUnuseAssetTask : AssetTask
    {
        private AsyncOperation _asyncOperate = null;
        public new int taskType = (int)AssetTaskType.UnloadUnuseAsset;
        public UnloadUnuseAssetTask()
        {

        }

        protected override bool OnStart()
        {
            _asyncOperate = Resources.UnloadUnusedAssets();
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

        }

        protected override void OnReset()
        {
            _asyncOperate = null;
        }

       

    }
}