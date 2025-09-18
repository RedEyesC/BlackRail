using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MotionMatching
{
    public partial struct MotionSynthesizer
    {
        public AffineTransform WorldRootTransform
        {
            get { return rootTransform; }
            set { rootTransform = value; }
        }

        public NativeSlice<AffineTransform> TrajectoryArray => trajectory.Array;

        //public float3 CurrentVelocity
        //{
        //    get
        //    {
        //        float sampleRate = Binary.SampleRate;
        //        float inverseSampleRate = 1.0f / sampleRate;
        //        AffineTransform transform = GetTrajectoryDeltaTransform(inverseSampleRate);
        //        return transform.t * sampleRate;
        //    }
        //}

        //public AffineTransform GetTrajectoryDeltaTransform(float deltaTime)
        //{
        //    if (samplingTime.IsValid)
        //    {
        //        return Binary.GetTrajectoryTransformBetween(Time, deltaTime);
        //    }

        //    return AffineTransform.identity;
        //}

        public void ClearTrajectory(NativeSlice<AffineTransform> trajectory)
        {
            trajectory.CopyFrom(TrajectoryArray);

            int halfTrajectoryLength = TrajectoryArray.Length / 2;
            for (int i = halfTrajectoryLength; i < trajectory.Length; ++i)
            {
                trajectory[i] = AffineTransform.identity;
            }
        }
    }
}
