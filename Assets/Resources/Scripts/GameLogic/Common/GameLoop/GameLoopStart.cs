namespace GameFramework.Runtime
{
    internal class GameLoopStart : StateBase
    {
        BaseView _view;
        public override string GetID()
        {
            return "start";
        }

        public override void StateEnter(params object[] paramList)
        {
            GameCenter.GetModule<ModuleCenter>().GetModule<LoginCtrl>().OpenLoginView();

            //GameCenter.GetModule<ModuleCenter>().GetModule<SceneCtrl>().LoadScene(1001);

            //Role mainRole = GameCenter.GetModule<ModuleCenter>().GetModule<SceneCtrl>().CreateMainRole();

            //// 远景 28，50，35，0，0，中景 20，50，35，0，0， 近景 10，60，20，0，0   
            //GlobalCenter.GetModule<CameraManager>().SetTarget(mainRole.root, 10f, 35, 20, 0, 0);
        }

        public override void StateUpdate(float elapseSeconds, float realElapseSeconds)
        {
            GameCenter.GetModule<ModuleCenter>().GetModule<SceneCtrl>().Update(elapseSeconds, realElapseSeconds);
            GameCenter.GetModule<ModuleCenter>().GetModule<InputCtrl>().Update(elapseSeconds, realElapseSeconds);
        }
    }
}
