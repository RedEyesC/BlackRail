using Unity.Mathematics;

namespace MotionMatching
{
    public static class Utility
    {
        public static float3 GetAnalogInput(float x, float y)
        {
            var analogInput = new float3(x, 0.0f, y);

            if (math.length(analogInput) > 1.0f)
            {
                analogInput = math.normalize(analogInput);
            }

            return analogInput;
        }

        public static float3 GetDesiredForwardDirection(float3 absoluteLinearVelocity, float3 forwardDirection, float3 cameraForward)
        {
            var relativeDesiredVelocity = GetRelativeLinearVelocity(absoluteLinearVelocity, cameraForward);

            return math.normalizesafe(relativeDesiredVelocity, forwardDirection);
        }

        public static float3 GetRelativeLinearVelocity(float3 absoluteLinearVelocity, float3 normalizedViewDirection)
        {
            float3 forward2d = math.normalize(new float3(normalizedViewDirection.x, 0.0f, normalizedViewDirection.z));

            quaternion cameraRotation = MathEx.forRotation(MathEx.forward, forward2d);

            return MathEx.rotateVector(cameraRotation, absoluteLinearVelocity);
        }
    }
}
