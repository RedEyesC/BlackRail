using GameFramework.Common;
using GameFramework.Scene;
using GameLogic;

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
            SceneManager.SetTarget(mainRole.root, 15f, 35, 25, 0, 0);

            mainRole.SetPosition(-66, 499);
            //mainRole.PlayAnim("Idle");

            //创建怪物
            //Monster monster = SceneCtrl.CreateMonster(1001);
            //monster.SetPosition(-66, 550);
        }
    }
}
