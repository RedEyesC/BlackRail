using Unity.Mathematics;

namespace MotionMatching
{

    public class MotionMatchingController
    {

        public float desiredSpeedSlow = 3.9f;
        public float desiredSpeedFast = 5.5f;
        public float velocityPercentage = 1.0f;
        public float forwardPercentage = 1.0f;

        float3 movementDirection = new float3(0.0f, 0.0f, 1.0f);
        float moveIntensity = 0.0f;

        public void UpdateInputMove(float horizontal,float vertical, float3 cameraForward)
        {

            float3 analogInput = Utility.GetAnalogInput(horizontal, vertical);

            moveIntensity = math.length(analogInput);

            if (moveIntensity <= 0.1f)
            {
                moveIntensity = 0.0f;
            }
            else
            {
                movementDirection = Utility.GetDesiredForwardDirection(analogInput, movementDirection,cameraForward);
            }

        }

    }


}

