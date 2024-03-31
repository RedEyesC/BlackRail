
namespace GameFramework.Runtime
{

    public abstract class GameModule
    {
        public virtual int priority
        {
            get
            {
                return 0;
            }
        }

        public abstract void Start();


        /// <summary>
        /// 游戏框架模块轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        public abstract void Update(float elapseSeconds, float realElapseSeconds);


        public abstract void Destroy();
    }
}
