namespace GameFramework.Runtime
{
    internal class GameLoopStart : StateBase
    {
        BaseView _View; 
        public override string GetID()
        {
            return "start";
        }

        public override void StateEnter(params object[] paramList)
        {
            //GameCenter.GetModule<ModuleCenter>().GetModule<LoginCtrl>().OpenLoginView();
            GameCenter.GetModule<ModuleCenter>().GetModule<SceneCtrl>().LoadScene(1001);
        }
    }
}
