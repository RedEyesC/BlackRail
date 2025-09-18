namespace MotionMatching
{
    public partial struct Binary
    {
        internal static int s_CodeVersion = 4;

        private float timeHorizon;
        private float sampleRate;

        public float TimeHorizon
        {
            get { return timeHorizon; }
            internal set { timeHorizon = value; }
        }

        public float SampleRate
        {
            get { return sampleRate; }
            internal set { sampleRate = value; }
        }

        internal AnimationRig animationRig;

        internal BlobArray<Trait> traits;
        internal BlobArray<byte> payloads;

        internal BlobArray<Tag> tags;

        internal BlobArray<TagIndex> tagIndices;
        internal BlobArray<TagList> tagLists;

        internal BlobArray<Type> types;
        internal BlobArray<Interval> intervals;

        internal BlobArray<byte> stringBuffer;
        internal BlobArray<String> stringTable;
    }
}
