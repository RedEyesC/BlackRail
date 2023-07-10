
namespace GameFramework.Runtime
{
    public abstract class AssetTask
    {

        public enum AssetTaskType
        {
            LoadAsset = 1,
            UnloadUnuseAsset = 1 << 1,
            DonwloadBundle = 1 << 2,
            UnLoadAsset = 1 << 3,
        }
        private bool _Running = false;
        private bool _Done = false;


        private static int CurTaskNum = 0;
        private static int MaxTaskNum = 7;

        protected abstract bool OnStart();
        protected abstract bool OnUpdate();
        protected abstract void OnEnd();
        protected abstract void OnReset();
        protected virtual void OnTimeOut() { }
        public virtual bool IsTimeOut()
        {
            return false;
        }

        public static void SetMaxTaskNum(int n)
        {
            MaxTaskNum = n;
        }

        public static int GetCurTaskNum()
        {
            return CurTaskNum;
        }

        public static int GetMaxTaskNum()
        {
            return MaxTaskNum;
        }

        public static void ResetTaskNum()
        {
            MaxTaskNum = 7;
            CurTaskNum = 0;
        }

        public abstract int TaskType { get; }

        public bool Update()
        {
            if (!_Running)
            {
                if (CurTaskNum < MaxTaskNum)
                {
                    if (OnStart())
                    {
                        _Running = true;
                        ++CurTaskNum;
                    }
                }
                return false;
            }

            bool done = _Done;
            if (!done)
            {
                _Done = OnUpdate();
            }

            if (done)
            {
                _Running = false;
                --CurTaskNum;

                OnEnd();
                return true;
            }
            else
            {
                if (IsTimeOut())
                {
                    _Running = false;
                    --CurTaskNum;

                    OnTimeOut();
                    return true;
                }
            }
            return false;
        }

        public void Reset()
        {
            _Done = false;
            OnReset();
        }

    }
}