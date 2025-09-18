using System.Reflection;
using Unity.Mathematics;

namespace MotionMatching
{
    public partial struct Binary
    {
        internal AffineTransform GetTrajectoryTransformBetween(float sampleTimeInSeconds, float deltaTime)
        {
            return GetTrajectoryTransform(sampleTimeInSeconds).inverseTimes(GetTrajectoryTransform(sampleTimeInSeconds + deltaTime));
        }

        //public AffineTransform GetTrajectoryTransformBetween(SamplingTime samplingTime, float deltaTime)
        //{
        //    AffineTransform referenceTransform = GetTrajectoryTransform(samplingTime);

        //    return referenceTransform.inverseTimes(GetTrajectoryTransform(Advance(samplingTime, deltaTime)));
        //}

        public AffineTransform GetTrajectoryTransform(float sampleTimeInSeconds)
        {
            return GetJointTransform(0, sampleTimeInSeconds);
        }

        //public AffineTransform GetTrajectoryTransform(SamplingTime samplingTime)
        //{
        //    return GetJointTransform(0, samplingTime);
        //}

        internal AffineTransform GetJointTransform(int jointIndex, float sampleTimeInSeconds)
        {
            int sampleKeyFrame = MathEx.truncToInt(sampleTimeInSeconds * sampleRate);

            float fractionalKeyFrame = sampleTimeInSeconds * sampleRate;
            float theta = math.saturate(fractionalKeyFrame - sampleKeyFrame);

            // This is intentionally *not* Mathf.Epsilon
            if (theta <= MathEx.epsilon)
            {
                return GetJointTransform(jointIndex, sampleKeyFrame);
            }

            AffineTransform t0 = GetJointTransform(jointIndex, sampleKeyFrame + 0);
            AffineTransform t1 = GetJointTransform(jointIndex, sampleKeyFrame + 1);

            return MathEx.lerp(t0, t1, theta);
        }

        //internal AffineTransform GetJointTransform(int jointIndex, SamplingTime samplingTime)
        //{
        //    var frameIndex = GetFrameIndex(samplingTime.timeIndex);

        //    float theta = math.saturate(samplingTime.theta);

        //    var numFramesMinusOne = GetSegment(samplingTime.segmentIndex).destination.NumFrames - 1;

        //    if (samplingTime.frameIndex >= numFramesMinusOne)
        //    {
        //        return GetJointTransform(jointIndex, frameIndex);
        //    }
        //    else if (theta <= Missing.epsilon)
        //    {
        //        return GetJointTransform(jointIndex, frameIndex);
        //    }
        //    else if (theta >= 1.0f - Missing.epsilon)
        //    {
        //        return GetJointTransform(jointIndex, frameIndex + 1);
        //    }

        //    AffineTransform t0 = GetJointTransform(jointIndex, frameIndex + 0);
        //    AffineTransform t1 = GetJointTransform(jointIndex, frameIndex + 1);

        //    return MathEx.lerp(t0, t1, theta);
        //}

        internal void SamplePoseAt(SamplingTime samplingTime, ref TransformBuffer transformBuffer)
        {
            //TODO
        }
    }
}
