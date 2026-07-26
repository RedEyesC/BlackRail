using System;

namespace GameLogic
{
    [Serializable]
    public sealed class MovementSettings
    {
        public float inputDeadZone = 0.05f;
        public float runInputThreshold = 0.65f;
        public float walkSpeedScale = 0.55f;
        public float runSpeedScale = 1f;
        public float walkSpeed = 1.65f;
        public float runSpeed = 3.5f;
        public float maxAcceleration = 18f;
        public float maxBrakingDeceleration = 24f;
        public float locomotionDampTime = 0.12f;
        public float stateFadeTime = 0.12f;
        public float turnBackAngle = 135f;
        public bool rotateToMoveDirection = true;
    }
}
