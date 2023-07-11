namespace GameFramework.Runtime
{
    public class AppLoopManager : GameModule
    {
        private StateMachine _StateMachine;

        public override void Start()
        {
            _StateMachine = new StateMachine();
            _StateMachine.AddState(new AppLoopStart());

            _StateMachine.ChangeState("start");
        }

        public override void Update(float elapseSeconds, float realElapseSeconds)
        {
            _StateMachine.Update(elapseSeconds, realElapseSeconds);
        }

        public override void Destroy()
        {
            _StateMachine.Destroy(null);
        }
    }
}
