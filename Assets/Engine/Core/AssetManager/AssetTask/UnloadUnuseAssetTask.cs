
using UnityEngine;

namespace GameFramework.Runtime
{

    public class UnloadUnuseAssetTask : AssetTask
    {
        private AsyncOperation _AsyncOperate = null;

        public UnloadUnuseAssetTask()
        {
        }

        protected override bool OnStart()
        {
            _AsyncOperate = Resources.UnloadUnusedAssets();
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

        }

        protected override void OnReset()
        {
            _AsyncOperate = null;
        }

        private static readonly int _TaskType = (int)AssetTaskType.UnloadUnuseAsset;

        public override int TaskType { get { return _TaskType; } }

    }
}