namespace GameFramework.Runtime
{
    internal class GameLoopStart : StateBase
    {
        BaseView _View; 
        public override string GetID()
        {
            return "start";
        }

        public override void StateEnter()
        {
            _View = new LoginView();

            _View.Open();
        }
    }
}
