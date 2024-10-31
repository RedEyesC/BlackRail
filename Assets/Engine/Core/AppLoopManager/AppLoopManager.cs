using GameFramework.Common;

namespace GameFramework.AppLoop
{
    public class AppLoopManager : GameModule
    {
        private static StateMachine _stateMachine;

        public new int priority = 2;

        public override void Start()
        {
            _stateMachine = new StateMachine();
            _stateMachine.AddState(new AppLoopStart());
            _stateMachine.AddState(new AppLoopLoading());
            _stateMachine.AddState(new AppLoopPlay());
            _stateMachine.ChangeState("Start");
        }

        public override void Update(float nowTime, float elapseSeconds)
        {
            _stateMachine.Update(nowTime, elapseSeconds);
        }

        public override void Destroy()
        {
            _stateMachine.Destroy(null);
        }

        public static void ChangeState(string state)
        {
            _stateMachine.ChangeState(state);
        }
    }
}
