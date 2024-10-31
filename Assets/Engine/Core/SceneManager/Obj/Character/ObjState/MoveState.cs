

using GameFramework.Common;

namespace GameFramework.Scene
{
    internal class MoveState : StateBase
    {
        Monster _obj;

        public MoveState(Monster obj)
        {

            _obj = obj;

        }

        public override string GetID()
        {
            return "Move";
        }

        public override void StateUpdate(float nowTime, float elapseSeconds)
        {
            
        }
    }
}
