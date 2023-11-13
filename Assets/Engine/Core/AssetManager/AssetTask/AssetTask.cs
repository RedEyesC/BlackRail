
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
            LoadScene = 1 << 4,
            UnLoadScene = 1 << 5,
        }
        private bool _running = false;
        private bool _done = false;


        private static int _curTaskNum = 0;
        private static int _maxTaskNum = 7;


        public int taskType;

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
            _maxTaskNum = n;
        }

        public static int Get_curTaskNum()
        {
            return _curTaskNum;
        }

        public static int GetMaxTaskNum()
        {
            return _maxTaskNum;
        }

        public static void ResetTaskNum()
        {
            _maxTaskNum = 7;
            _curTaskNum = 0;
        }

        public bool Update()
        {
            if (!_running)
            {
                if (_curTaskNum < _maxTaskNum)
                {
                    if (OnStart())
                    {
                        _running = true;
                        ++_curTaskNum;
                    }
                }
                return false;
            }

            bool done = _done;
            if (!done)
            {
                _done = OnUpdate();
            }

            if (done)
            {
                _running = false;
                --_curTaskNum;

                OnEnd();
                return true;
            }
            else
            {
                if (IsTimeOut())
                {
                    _running = false;
                    --_curTaskNum;

                    OnTimeOut();
                    return true;
                }
            }
            return false;
        }

        public void Reset()
        {
            _done = false;
            OnReset();
        }

    }
}