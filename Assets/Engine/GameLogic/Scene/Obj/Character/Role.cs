

using GameFramework.Input;
using GameFramework.Common;
using UnityEngine;

namespace GameLogic
{

    internal class Role : Obj
    {




        public Role() : base(BodyType.Role)
        {

        }



        public override void Init(BodyType bodyType)
        {

            speed = 2f;

            base.Init(bodyType);
        }


        private float _desiredGait = 0.0f;
        private float _desiredGaitVelocity = 0.0f;


        private float _simulationRunFwrdSpeed = 4.0f;
        private float _simulationRunSideSpeed = 3.0f;
        private float _simulationRunBackSpeed = 2.5f;

        private float _simulationWalkFwrdSpeed = 1.75f;
        private float _simulationWalkSideSpeed = 1.5f;
        private float _simulationWalkBackSpeed = 1.25f;

        public override void UpdateMove(float nowTime, float elapseSeconds)
        {

            Vector3 directionVector = new Vector3(InputManager.GetAxis("Action", "Horizontal"), 0, InputManager.GetAxis("Action", "Vertical"));


            if (directionVector.x != 0 || directionVector.z != 0)
            {
                Debug.Log("x: " + directionVector.x + "z: " + directionVector.z);
            }


            //TODO bool desired_strafe = desired_strafe_update();

            //实现从走到奔跑的缓动
            DesiredGaitUpdate(ref _desiredGait, ref _desiredGaitVelocity, elapseSeconds);

            float simulationFwrdSpeed = Math.Lerpf(_simulationRunFwrdSpeed, _simulationWalkFwrdSpeed, _desiredGait);
            float simulationSideSpeed = Math.Lerpf(_simulationRunSideSpeed, _simulationWalkSideSpeed, _desiredGait);
            float simulationBackSpeed = Math.Lerpf(_simulationRunBackSpeed, _simulationWalkBackSpeed, _desiredGait);


             
        }

        void DesiredGaitUpdate(ref float desiredGait, ref float desiredGaitVelocity, float dt, float gaitChangeHalflife = 0.1f)
        {
            //判断是否进入奔跑
            float target = InputManager.GetButtonDown("Action", "Run") ? 1.0f : 0.0f;

            Spring.SimpleSpringDamperExact(ref desiredGait, ref desiredGaitVelocity, target, gaitChangeHalflife, dt);
        }
    }
}
