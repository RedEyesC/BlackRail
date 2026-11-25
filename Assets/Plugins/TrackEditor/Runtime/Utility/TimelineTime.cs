namespace TrackEditor
{
    public static class TimelineTime
    {
        public static bool IsAfter(float time, float other) => time > other;
        public static bool IsBefore(float time, float other) => time < other;
    }
}
