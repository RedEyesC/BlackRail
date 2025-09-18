using Unity.Collections;
using Unity.Mathematics;

namespace MotionMatching
{
    public struct TrajectoryModel
    {
        private NativeSlice<AffineTransform> trajectory;

        private MemoryRef<Binary> binary;

        internal TrajectoryModel(ref ArrayMemory memory, ref Binary binary)
        {
            //
            // Initialize attributes
            //

            this.binary = MemoryRef<Binary>.Create(ref binary);

            float sampleRate = binary.SampleRate;
            float timeHorizon = binary.TimeHorizon;

            int trajectoryLength = DurationToFrames(timeHorizon * 2.0f, sampleRate);

            trajectory = memory.CreateSlice<AffineTransform>(trajectoryLength);
            for (int i = 0; i < trajectoryLength; ++i)
            {
                trajectory[i] = AffineTransform.identity;
            }

            //var accumulatedIdentity =
            //    AccumulatedTransform.Create(
            //        AffineTransform.identity, math.rcp(binary.SampleRate));

            //deltaSpaceTrajectory = memory.CreateSlice<AccumulatedTransform>(trajectoryLength * 2);
            //for (int i = 0; i < trajectoryLength * 2; ++i)
            //{
            //    deltaSpaceTrajectory[i] = accumulatedIdentity;
            //}

            //Assert.IsTrue(trajectoryLength == TrajectoryLength);
        }

        public NativeSlice<AffineTransform> Array
        {
            get { return trajectory; }
        }

        internal static int DurationToFrames(float durationInSeconds, float samplesPerSecond)
        {
            return MathEx.truncToInt(durationInSeconds * samplesPerSecond) + 1;
        }
    }
}
