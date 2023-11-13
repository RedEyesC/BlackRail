namespace GameFramework.Runtime
{
    public class AppLoopManager : GameModule
    {
        private StateMachine _stateMachine;

        public override void Start()
        {
            _stateMachine = new StateMachine();
            _stateMachine.AddState(new AppLoopStart());

            _stateMachine.ChangeState("start");
        }

        public override void Update(float elapseSeconds, float realElapseSeconds)
        {
            _stateMachine.Update(elapseSeconds, realElapseSeconds);
        }

        public override void Destroy()
        {
            _stateMachine.Destroy(null);
        }
    }
}
