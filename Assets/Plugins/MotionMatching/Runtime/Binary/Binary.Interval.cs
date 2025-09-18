using UnityEngine.Assertions;

namespace MotionMatching
{
    public partial struct Binary
    {
        public struct Interval
        {
            //TODO
            public TagListIndex tagListIndex;

            public int firstFrame;

            public int numFrames;
        }

        public struct IntervalIndex
        {
            internal int value;

            public bool Equals(IntervalIndex intervalIndex)
            {
                return value == intervalIndex.value;
            }

            public static implicit operator int(IntervalIndex intervalIndex)
            {
                return intervalIndex.value;
            }

            public static implicit operator IntervalIndex(int intervalIndex)
            {
                return Create(intervalIndex);
            }

            internal static IntervalIndex Create(int intervalIndex)
            {
                return new IntervalIndex { value = intervalIndex };
            }

            public static IntervalIndex Empty => -1;
        }

        public int numIntervals => intervals.Length;

        public ref Interval GetInterval(IntervalIndex intervalIndex)
        {
            return ref intervals[intervalIndex];
        }
    }
}
