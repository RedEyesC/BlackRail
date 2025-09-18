namespace MotionMatching
{
    public struct TrajectoryPrediction
    {
        //public static MotionMatchingPrediction CreateFromDirection(
        //    ref MotionSynthesizer synthesizer,
        //    float3 desiredDirection,
        //    float desiredSpeed,
        //    Trajectory trajectory,
        //    float velocityFactor,
        //    float rotationFactor
        //)
        //{
        //    float3 desiredVelocity = desiredDirection * desiredSpeed;
        //    quaternion desiredRotation = MathEx.forRotation(MathEx.forward, desiredDirection);

        //    return Create(ref synthesizer, desiredVelocity, desiredRotation, trajectory, velocityFactor, rotationFactor);
        //}

        //public static MotionMatchingPrediction Create(
        //    ref MotionSynthesizer synthesizer,
        //    float3 desiredLinearVelocity,
        //    quaternion desiredRotation,
        //    Trajectory trajectory,
        //    float velocityFactor,
        //    float rotationFactor
        //)
        //{
        //    return MotionMatchingPrediction.Create(
        //        ref synthesizer,
        //        desiredLinearVelocity,
        //        desiredRotation,
        //        trajectory,
        //        velocityFactor,
        //        rotationFactor,
        //        synthesizer.CurrentVelocity
        //    );
        //}

        //public static MotionMatchingPrediction Create(
        //    ref MotionSynthesizer synthesizer,
        //    float3 desiredLinearVelocity,
        //    quaternion desiredRotation,
        //    Trajectory trajectory,
        //    float velocityFactor,
        //    float rotationFactor,
        //    float3 currentLinearVelocity
        //)
        //{
        //    ref var binary = ref synthesizer.Binary;

        //    synthesizer.ClearTrajectory(trajectory);

        //    float sampleRate = binary.SampleRate;

        //    var worldRootTransform = synthesizer.WorldRootTransform;

        //    // Desired velocity in m/s in character space
        //    desiredLinearVelocity = MathEx.rotateVector(MathEx.conjugate(worldRootTransform.q), desiredLinearVelocity);

        //    // Desired rotation in character space
        //    desiredRotation = math.mul(MathEx.conjugate(worldRootTransform.q), desiredRotation);

        //    return new MotionMatchingPrediction
        //    {
        //        sampleRate = sampleRate,

        //        velocityFactor = velocityFactor,
        //        rotationFactor = rotationFactor,

        //        desiredLinearVelocity = desiredLinearVelocity,
        //        desiredRotation = desiredRotation,

        //        forward = SmoothValue.Create(0.0f),
        //        linearVelocity = SmoothValue3.Create(currentLinearVelocity),

        //        rootTransform = AffineTransform.identity,

        //        currentIndex = 0,

        //        trajectory = trajectory,
        //    };
        //}
    }
}
