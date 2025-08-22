using GameFramework.Common;
using GameLogic;

namespace GameFramework.AppLoop
{
    public class AppLoopStart : StateBase
    {
        public override string GetID()
        {
            return "Start";
        }

        public override void StateUpdate(float nowTime, float elapseSeconds)
        {

        }

        public override void StateEnter(params object[] paramList)
        {
            SysSettingCtrl.InitSysSetting();
            LoginCtrl.OpenLoginView();
        }

    }
}
