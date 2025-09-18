namespace MotionMatching
{
    public enum MatchOptions
    {
        None = 0,
        DontMatchIfCandidateIsPlaying = 1 << 0,
        LoopSegment = 1 << 1,
    };

    public partial struct MotionSynthesizer
    {
        public bool MatchPose(
            PoseSet candidates,
            SamplingTime samplingTime,
            MatchOptions options = MatchOptions.None,
            float maxPoseDeviation = -1.0f
        )
        {
            //TODO
            return false;
        }

        public bool MatchPose(PoseSet candidates, SamplingTime samplingTime)
        {
            //TODO
            return false;
        }

        public bool MatchPoseAndTrajectory(
            PoseSet candidates,
            SamplingTime samplingTime,
            Trajectory trajectory,
            MatchOptions options = MatchOptions.None,
            float trajectoryWeight = 0.6f,
            float minTrajectoryDeviation = 0.03f,
            float maxTotalDeviation = -1.0f
        )
        {
            //TODO
            return false;
        }
    }
}
