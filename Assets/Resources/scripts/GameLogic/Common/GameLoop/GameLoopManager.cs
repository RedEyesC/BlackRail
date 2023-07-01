namespace GameFramework.Runtime
{
    internal class GameLoopManager : GameModule
    {
        private StateMachine _StateMachine;

        public override void Start()
        {
            this._StateMachine = new StateMachine();
        }

        public override void Update(float elapseSeconds, float realElapseSeconds)
        {
            this._StateMachine.Update(elapseSeconds, realElapseSeconds);
        }

        public override void Destroy()
        {

        }
    }
}
