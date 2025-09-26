using UnityEngine;

namespace TrackEditor
{
    public enum WrapMode
    {
        Once,
        Loop,
    }

    public enum EditorPlaybackState
    {
        Stoped,
        PlayingForwards,
        PlayingBackwards,
    }

    internal struct GuideLine
    {
        public float time;
        public Color color;

        public GuideLine(float time, Color color)
        {
            this.time = time;
            this.color = color;
        }
    }
}
