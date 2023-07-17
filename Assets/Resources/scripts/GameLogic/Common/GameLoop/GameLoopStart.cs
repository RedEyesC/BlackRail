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

           Role mainRole = GameCenter.GetModule<ModuleCenter>().GetModule<SceneCtrl>().CreateMainRole();
        }

        public override void StateUpdate(float elapseSeconds, float realElapseSeconds)
        {
            GameCenter.GetModule<ModuleCenter>().GetModule<SceneCtrl>().Update(elapseSeconds, realElapseSeconds);
        }
    }
}
