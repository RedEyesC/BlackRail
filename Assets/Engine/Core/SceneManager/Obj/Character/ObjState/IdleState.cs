

using GameFramework.Common;

namespace GameFramework.Scene
{
    internal class IdleState : StateBase
    {
        Monster _obj;

        public IdleState(Monster obj)
        {

            _obj = obj;

        }


        public override string GetID()
        {
            return "Idle";
        }
    }
}
