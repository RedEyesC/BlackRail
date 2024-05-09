using GameFramework.Common;
using GameFramework.Moudule;
using GameFramework.Scene;

namespace GameFramework.AppLoop
{
    internal class AppLoopPlay : StateBase
    {
        public override string GetID()
        {
            return "Play";
        }

        public override void StateEnter(params object[] paramList)
        {
            Role mainRole = SceneCtrl.GetMainRole();

            //// 远景 28，50，35，0，0，中景 20，50，35，0，0， 近景 10，60，20，0，0   
            SceneManager.SetTarget(mainRole.root, 10f, 35, 20, 0, 0);

            mainRole.SetPosition(-66,0,499);
        }
    }
}