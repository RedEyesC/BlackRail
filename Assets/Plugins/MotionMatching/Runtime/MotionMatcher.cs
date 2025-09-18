using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MotionMatching
{
    [BurstCompile(CompileSynchronously = true)]
    public struct MotionMatchingJob : IJob
    {
        public MemoryRef<MotionSynthesizer> synthesizer;

        public PoseSet idleCandidates;

        public PoseSet locomotionCandidates;

        public Trajectory trajectory;

        public bool idle;

        ref MotionSynthesizer Synthesizer => ref synthesizer.Ref;

        public void Execute()
        {
            if (
                idle
                && Synthesizer.MatchPose(
                    idleCandidates,
                    Synthesizer.Time,
                    MatchOptions.DontMatchIfCandidateIsPlaying | MatchOptions.LoopSegment,
                    0.01f
                )
            )
            {
                return;
            }

            Synthesizer.MatchPoseAndTrajectory(locomotionCandidates, Synthesizer.Time, trajectory);
        }
    }

    public class MotionMatcher
    {
        public float desiredSpeedSlow = 3.9f;
        public float desiredSpeedFast = 5.5f;
        public float velocityPercentage = 1.0f;
        public float forwardPercentage = 1.0f;

        public float blendDuration = 0.25f;

        float3 movementDirection = new float3(0.0f, 0.0f, 1.0f);
        float moveIntensity = 0.0f;

        MotionSynthesizer synthesizer;

        PoseSet idleCandidates;
        PoseSet locomotionCandidates;
        Trajectory trajectory;

        public MotionMatcher(Transform transform, BinaryReference resource)
        {
            synthesizer = MotionSynthesizer.Create(
                resource,
                AffineTransform.Create(transform.position, transform.rotation),
                blendDuration,
                Allocator.Persistent
            );

            idleCandidates = synthesizer.Query.Where("Idle", Locomotion.Default).And(Idle.Default);
            locomotionCandidates = synthesizer.Query.Where("Locomotion", Locomotion.Default).Except(Idle.Default);

            trajectory = Trajectory.Create(synthesizer.TrajectoryArray.Length, Allocator.Persistent);
        }

        public MemoryRef<MotionSynthesizer> Synthesizer
        {
            get { return MemoryRef<MotionSynthesizer>.Create(ref synthesizer); }
        }

        public void UpdateInputMove(float horizontal, float vertical, float3 cameraForward)
        {
            float3 analogInput = Utility.GetAnalogInput(horizontal, vertical);

            moveIntensity = math.length(analogInput);

            if (moveIntensity <= 0.1f)
            {
                moveIntensity = 0.0f;
            }
            else
            {
                movementDirection = Utility.GetDesiredForwardDirection(analogInput, movementDirection, cameraForward);
            }
        }

        public void Update()
        {
            if (synthesizer.IsValid)
            {
                synthesizer.UpdateFrameCount(Time.frameCount, Time.deltaTime);
            }
        }

        public MotionMatchingJob GetMotionMatchingJob()
        {
            float desiredSpeed = moveIntensity * desiredSpeedFast;

            //TODO
            //MotionMatchingPrediction
            //    .CreateFromDirection(
            //        ref Synthesizer.Ref,
            //        movementDirection,
            //        desiredSpeed,
            //        trajectory,
            //        velocityPercentage,
            //        forwardPercentage
            //    )
            //    .Generate();

            return new MotionMatchingJob()
            {
                synthesizer = Synthesizer,
                idleCandidates = idleCandidates,
                locomotionCandidates = locomotionCandidates,
                trajectory = trajectory,
                idle = moveIntensity == 0.0f,
            };
        }

        public void OnDisable()
        {
            synthesizer.Dispose();
            trajectory.Dispose();
        }
    }
}
