namespace MotionMatching
{
    public partial struct Binary
    {
        internal struct AnimationRig
        {
            public struct Joint
            {
                public int nameIndex;
                public int parentIndex;
                public AffineTransform localTransform;
            }

            public int NumJoints
            {
                get { return bindPose.Length; }
            }

            public int GetParentJointIndex(int index)
            {
                return bindPose[index].parentIndex;
            }

            public int GetJointIndexForNameIndex(int nameIndex)
            {
                for (int i = 0; i < bindPose.Length; ++i)
                {
                    if (bindPose[i].nameIndex == nameIndex)
                    {
                        return i;
                    }
                }

                return -1;
            }

            public BlobArray<Joint> bindPose;
        }

        public int numJoints => animationRig.NumJoints;
    }
}
