namespace MotionMatching
{
    public partial struct Binary
    {
        public struct Tag
        {
            public TraitIndex traitIndex;

            //TODO
        }

        public struct TagIndex
        {
            internal int value;

            public bool IsValid => value != Invalid;

            public bool Equals(TagIndex tagIndex)
            {
                return value == tagIndex.value;
            }

            public static implicit operator int(TagIndex tagIndex)
            {
                return tagIndex.value;
            }

            public static implicit operator TagIndex(int tagIndex)
            {
                return Create(tagIndex);
            }

            internal static TagIndex Create(int tagIndex)
            {
                return new TagIndex { value = tagIndex };
            }

            public static TagIndex Invalid => -1;
        }
    }
}
