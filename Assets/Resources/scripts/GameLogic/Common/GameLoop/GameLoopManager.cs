namespace GameFramework.Runtime
{
    internal class GameLoopManager : GameModule
    {
        private StateMachine _StateMachine;

        public override void Start()
        {
            _StateMachine = new StateMachine();
            _StateMachine.AddState(new GameLoopStart());

            _StateMachine.ChangeState("start");
        }

        public override void Update(float elapseSeconds, float realElapseSeconds)
        {
            _StateMachine.Update(elapseSeconds, realElapseSeconds);
        }

        public override void Destroy()
        {

        }
    }
}
