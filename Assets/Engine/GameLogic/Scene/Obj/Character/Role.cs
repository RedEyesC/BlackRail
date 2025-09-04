
using GameFramework.Input;
using GameFramework.Scene;
using MotionMatching;

namespace GameLogic
{

    internal class Role : Obj
    {

        private MotionMatchingController _motionMatchingController;


        public Role() : base(BodyType.Role)
        {


        }



        public override void Init(BodyType bodyType)
        {

            speed = 2f;
            _motionMatchingController = new MotionMatchingController();
            
            base.Init(bodyType);
        }


        public override void EarlyUpdate()
        {
            _motionMatchingController.UpdateInputMove(InputManager.GetAxis("Action", "Horizontal"), InputManager.GetAxis("Action", "Vertical"), SceneManager.GetMainCameraForward());
        }


        public override void StateUpdate(float nowTime, float elapseSeconds)
        {


        }
    }
}
