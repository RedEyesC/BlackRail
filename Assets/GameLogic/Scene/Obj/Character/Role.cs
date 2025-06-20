

namespace GameLogic
{

    internal class Role : Obj
    {

        public Role():base(BodyType.Role)
        {

        }



        public override void Init(BodyType bodyType)
        {

            speed = 2f;

            base.Init(bodyType);
        }
    }
}
