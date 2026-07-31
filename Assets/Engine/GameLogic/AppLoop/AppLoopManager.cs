using GameFramework;
using GameFramework.Common;

namespace GameLogic
{
    public class AppLoopManager : GameModule
    {
        private ModuleManager _modules;
        private static StateMachine _stateMachine;

        public override int priority => 2;

        public override void Start()
        {
            _modules = new ModuleManager();
            _modules.Start();

            _stateMachine = new StateMachine();
            _stateMachine.AddState(new AppLoopStart());
            _stateMachine.AddState(new AppLoopLoading());
            _stateMachine.AddState(new AppLoopPlay());
            _stateMachine.ChangeState("Start");
        }

        public override void Update(float nowTime, float elapseSeconds)
        {
            _modules.Update(nowTime, elapseSeconds);

            _stateMachine.Update(nowTime, elapseSeconds);
        }

        public override void Destroy()
        {
            _stateMachine.Destroy(null);

            _modules.Destroy();
        }

        public static void ChangeState(string state)
        {
            _stateMachine.ChangeState(state);
        }
    }
}
