namespace TrackEditor
{
    public static class TimelineTime
    {
        public static double RoundForComparison(float seconds) => System.Math.Round(seconds, 4);

        public static bool AreEqual(float time, float other) => RoundForComparison(time) == RoundForComparison(other);

        public static bool IsAfter(float time, float other) => time > other;
        public static bool IsBefore(float time, float other) => time < other;
    }
}
