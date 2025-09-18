namespace MotionMatching
{
    public struct TimeIndex
    {
        public short segmentIndex;

        public short frameIndex;

        public bool IsValid
        {
            get => segmentIndex >= 0;
        }

        public static TimeIndex Invalid
        {
            get => new TimeIndex { segmentIndex = -1 };
        }
    }
}
