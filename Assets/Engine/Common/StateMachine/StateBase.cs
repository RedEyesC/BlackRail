
namespace GameFramework.Common
{
    /// <summary>
    /// 流程基类。
    /// </summary>
    public abstract class StateBase
    {

        /// <summary>
        /// 状态初始化时调用。
        /// </summary>
        public abstract string GetID();


        /// <summary>
        /// 进入状态时调用。
        /// </summary>
        public virtual void StateEnter(params object[] paramList) { }

        /// <summary>
        /// 状态轮询时调用。
        /// </summary>
        /// <param name="nowTime">从游戏开始的流逝时间，以秒为单位。</param>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        public virtual void StateUpdate(float nowTime, float elapseSeconds) { }

        /// <summary>
        /// 状态销毁时调用。
        /// </summary>
        public virtual void StateQuit(params object[] paramList) { }


    }
}
