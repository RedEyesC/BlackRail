using System;

namespace GameLogic
{
    [Serializable]
    public sealed class MovementSettings
    {
        public float inputDeadZone = 0.05f;
        public float inputResponsePower = 1.6f;
        public float runInputThreshold = 0.65f;
        public float walkForwardSpeed = 1.75f;
        public float walkSideSpeed = 1.5f;
        public float runForwardSpeed = 4f;
        public float runSideSpeed = 3f;
        public float velocityHalflife = 0.27f;
        public float rotationHalflife = 0.27f;
        public float locomotionDampTime = 0.12f;
        public float stateFadeTime = 0.12f;
        public float turnBackAngle = 135f;
        public bool rotateToMoveDirection = true;
    }
}
