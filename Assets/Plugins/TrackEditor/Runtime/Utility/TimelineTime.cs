using UnityEngine;

namespace TrackEditor
{
    public static class TimelineTime
    {
        public static int ToUnits(float time)
        {
            if (Prefs.timeStepMode == Prefs.TimeStepMode.Frames)
            {
                return Mathf.RoundToInt(time * Mathf.Max(1, Prefs.frameRate));
            }

            return Mathf.RoundToInt(time * 1000f);
        }

        public static float FromUnits(int units)
        {
            if (Prefs.timeStepMode == Prefs.TimeStepMode.Frames)
            {
                return units / (float)Mathf.Max(1, Prefs.frameRate);
            }

            return units / 1000f;
        }

        public static float Snap(float time)
        {
            if (Prefs.timeStepMode == Prefs.TimeStepMode.Frames)
            {
                return FromUnits(ToUnits(time));
            }

            var snapped = Mathf.Round(time / Prefs.snapInterval) * Prefs.snapInterval;
            return FromUnits(Mathf.RoundToInt(snapped * 1000f));
        }

        public static bool IsAfter(float time, float other)
        {
            return ToUnits(time) > ToUnits(other);
        }

        public static bool IsBefore(float time, float other)
        {
            return ToUnits(time) < ToUnits(other);
        }
    }
}
