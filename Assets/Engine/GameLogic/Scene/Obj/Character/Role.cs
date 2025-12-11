using GameFramework.Input;
using GameFramework.Scene;
using MotionMatching;
using Unity.Jobs;

namespace GameLogic
{
    internal class Role : Obj
    {
        private MotionMatcher _motionMatcher;
        private UpdateAnimationPoseJob job;

        public Role()
            : base(BodyType.Role) { }

        public override void Init(BodyType bodyType)
        {
            speed = 2f;
            //_motionMatcher = new MotionMatcher(root.transform, new BinaryReference());
            base.Init(bodyType);

            SetModelChangeCallback(
                (ModelObj modelObj) =>
                {
                    if (modelObj.modelType == 1)
                    {
                        //job = new UpdateAnimationPoseJob();
                        //job.Setup(modelObj.animator, modelObj.GetComponentsInChildrenTransform(), ref _motionMatcher.Synthesizer.Ref);

                        //modelObj.CreatePlayableGraph(job);
                    }
                }
            );
        }

        public override void EarlyUpdate()
        {
            //_motionMatcher.UpdateInputMove(
            //    InputManager.GetAxis("Action", "Horizontal"),
            //    InputManager.GetAxis("Action", "Vertical"),
            //    SceneManager.GetMainCameraForward()
            //);
        }

        public override void StateUpdate(float nowTime, float elapseSeconds)
        {
            //_motionMatcher.Update();
            //AddJobDependency(1, _motionMatcher.GetMotionMatchingJob().Schedule());
        }
    }
}
