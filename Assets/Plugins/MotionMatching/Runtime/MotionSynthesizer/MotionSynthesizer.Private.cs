using System;
using Unity.Collections;

namespace MotionMatching
{
    public partial struct MotionSynthesizer : IDisposable
    {
        MotionSynthesizer(BlobAssetReference<Binary> binary, AffineTransform worldRootTransform, float blendDuration, Allocator allocator)
        {
            //TODO
            m_binary = binary;

            arrayMemory = ArrayMemory.Create();

            //ReserveTraitTypes(ref arrayMemory);
            //PoseGenerator.Reserve(ref arrayMemory, ref binary.Value);
            //TrajectoryModel.Reserve(ref arrayMemory, ref binary.Value);

            //arrayMemory.Allocate(allocator);

            // We basically copy statically available data into this instance
            // so that the burst compiler does not complain about accessing static data.
            //traitTypes = ConstructTraitTypes(ref arrayMemory, ref binary.Value);

            poseGenerator = new PoseGenerator(ref arrayMemory, ref binary.Value, blendDuration);

            trajectory = new TrajectoryModel(ref arrayMemory, ref binary.Value);

            rootTransform = worldRootTransform;
            rootDeltaTransform = AffineTransform.identity;

            //updateInProgress = false;

            //_deltaTime = 0.0f;

            //lastSamplingTime = TimeIndex.Invalid;

            samplingTime = SamplingTime.Invalid;

            //delayedPushTime = TimeIndex.Invalid;

            frameCount = -1;

            lastProcessedFrameCount = -1;

            isValid = true;

            deltaTime = 0;

            //isDebugging = false;
            //readDebugMemory = DebugMemory.Create(1024, allocator);
            //writeDebugMemory = DebugMemory.Create(1024, allocator);
        }

        public void Dispose()
        {
            if (isValid)
            {
                arrayMemory.Dispose();
                //TODO
                //readDebugMemory.Dispose();
                //writeDebugMemory.Dispose();

                isValid = false;
            }
        }

        PoseGenerator poseGenerator;

        private SamplingTime samplingTime;

        private BlobAssetReference<Binary> m_binary;

        public TrajectoryModel trajectory;

        internal AffineTransform rootTransform;

        internal AffineTransform rootDeltaTransform;

        private int frameCount;

        private float deltaTime;

        private int lastProcessedFrameCount;

        private bool isValid;

        private ArrayMemory arrayMemory;
    }
}
