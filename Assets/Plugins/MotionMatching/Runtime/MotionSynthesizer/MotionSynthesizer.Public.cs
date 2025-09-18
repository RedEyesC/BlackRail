namespace MotionMatching
{
    public partial struct MotionSynthesizer
    {
        public bool IsValid => isValid;

        public float DeltaTime => deltaTime;

        public Query Query
        {
            get => Query.Create(ref Binary);
        }

        internal void UpdateFrameCount(int frameCount, float deltaTime)
        {
            this.frameCount = frameCount;
            this.deltaTime = deltaTime;
        }

        internal TransformBuffer LocalSpaceTransformBuffer => poseGenerator.LocalSpaceTransformBuffer;
    }
}
