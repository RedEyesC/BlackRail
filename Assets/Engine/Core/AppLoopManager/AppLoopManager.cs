namespace GameFramework.Runtime
{
    public class AppLoopManager : GameModule
    {
        public StateMachine _StateMachine;

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
