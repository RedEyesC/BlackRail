using UnityEngine;

namespace TrackEditor
{
    public static class AssetExtension
    {
        internal static float TimeToPos(this Rect rect, Vector2 range, float time)
        {
            return (time - range.x) / (range.y - range.x) * rect.width;
        }

        internal static float PosToTime(this Rect rect, Vector2 range, float pos)
        {
            return (pos - rect.x) / rect.width * (range.y - range.x) + range.x;
        }
    }
}
