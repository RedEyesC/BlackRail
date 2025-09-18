using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;

namespace MotionMatching
{
    [BurstCompile]
    public struct UpdateAnimationPoseJob : IAnimationJob, System.IDisposable
    {
        NativeArray<TransformStreamHandle> transforms;
        NativeArray<bool> boundJoints;

        MemoryRef<MotionSynthesizer> synthesizer;

        bool transformBufferValid;

        public bool Setup(Animator animator, Transform[] transforms, ref MotionSynthesizer synthesizer)
        {
            this.synthesizer = MemoryRef<MotionSynthesizer>.Create(ref synthesizer);
            int numJoints = synthesizer.Binary.numJoints;
            this.transforms = new NativeArray<TransformStreamHandle>(
                numJoints,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory
            );

            boundJoints = new NativeArray<bool>(numJoints, Allocator.Persistent, NativeArrayOptions.ClearMemory);

            this.transforms[0] = animator.BindStreamTransform(transforms[0]);
            boundJoints[0] = true;

            for (int i = 1; i < transforms.Length; i++)
            {
                int jointNameIndex = synthesizer.Binary.GetStringIndex(transforms[i].name);
                int jointIndex = (jointNameIndex >= 0) ? synthesizer.Binary.animationRig.GetJointIndexForNameIndex(jointNameIndex) : -1;
                if (jointIndex >= 0)
                {
                    this.transforms[jointIndex] = animator.BindStreamTransform(transforms[i]);
                    boundJoints[jointIndex] = true;
                }
            }

            return true;
        }

        public void Dispose()
        {
            transforms.Dispose();
            boundJoints.Dispose();
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
            ref MotionSynthesizer synthesizer = ref this.synthesizer.Ref;
            float dt = synthesizer.DeltaTime;

            var previousTransform = synthesizer.WorldRootTransform;
            transformBufferValid = synthesizer.Update(dt);

            //convert the delta transform to root motion in the stream
            if (transformBufferValid && dt > 0.0f)
            {
                WriteRootMotion(stream, dt, previousTransform, synthesizer.WorldRootTransform);
            }
        }

        public void ProcessAnimation(AnimationStream stream)
        {
            ref MotionSynthesizer synthesizer = ref this.synthesizer.Ref;

            if (transformBufferValid)
            {
                int numTransforms = synthesizer.LocalSpaceTransformBuffer.Length;
                for (int i = 1; i < numTransforms; ++i)
                {
                    if (!boundJoints[i])
                    {
                        continue;
                    }

                    transforms[i].SetLocalPosition(stream, synthesizer.LocalSpaceTransformBuffer[i].t);
                    transforms[i].SetLocalRotation(stream, synthesizer.LocalSpaceTransformBuffer[i].q);
                }
            }
        }

        void WriteRootMotion(AnimationStream stream, float deltaTime, AffineTransform previousTransform, AffineTransform updatedTransform)
        {
            float invDeltaTime = 1.0f / deltaTime;
            AffineTransform rootMotion = previousTransform.inverse() * updatedTransform;

            Vector3 rootMotionAngles = ((Quaternion)rootMotion.q).eulerAngles;
            rootMotionAngles = math.radians(rootMotionAngles);

            stream.velocity = invDeltaTime * rootMotion.t;
            stream.angularVelocity = invDeltaTime * rootMotionAngles;
        }
    }
}
