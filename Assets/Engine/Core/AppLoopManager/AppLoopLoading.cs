using GameFramework.Common;
using GameFramework.Moudule;

namespace GameFramework.AppLoop
{
    internal class AppLoopLoading : StateBase
    {
        private int _state;
        public override string GetID()
        {
            return "Loading";
        }

        public override void StateEnter(params object[] paramList)
        {

            _state = 0;
        }

        public override void StateUpdate(float elapseSeconds, float realElapseSeconds)
        {

            switch (_state)
            {
                case 0:
                    SceneCtrl.LoadScene(1003);
                    _state = 1;
                    break;
                case 1:


                    break;
                case 2:

                    break;



            }


        }
    }
}
