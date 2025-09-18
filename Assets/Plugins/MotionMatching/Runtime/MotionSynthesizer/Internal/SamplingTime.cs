namespace MotionMatching
{
    public struct SamplingTime
    {
        public TimeIndex timeIndex;

        public bool IsValid
        {
            get => timeIndex.IsValid;
        }

        public static SamplingTime Invalid
        {
            get => new SamplingTime { timeIndex = TimeIndex.Invalid };
        }
    }
}
