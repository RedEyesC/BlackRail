using UnityEngine.Assertions;

namespace MotionMatching
{
    public partial struct Binary
    {
        public struct TagList
        {
            public int tagIndicesIndex;

            public int numIndices;
        }

        public struct TagListIndex
        {
            internal int value;

            public bool Equals(TagListIndex tagListIndex)
            {
                return value == tagListIndex.value;
            }

            public static implicit operator int(TagListIndex tagListIndex)
            {
                return tagListIndex.value;
            }

            public static implicit operator TagListIndex(int tagListIndex)
            {
                return Create(tagListIndex);
            }

            internal static TagListIndex Create(int tagListIndex)
            {
                return new TagListIndex { value = tagListIndex };
            }
        }

        public ref TagList GetTagList(TagListIndex tagListIndex)
        {
            return ref tagLists[tagListIndex];
        }

        public bool Contains(ref TagList tagList, TraitIndex traitIndex)
        {
            for (int i = 0; i < tagList.numIndices; ++i)
            {
                var tagIndex = tagIndices[tagList.tagIndicesIndex + i];

                if (GetTag(tagIndex).traitIndex == traitIndex)
                {
                    return true;
                }
            }

            return false;
        }

        public bool Contains(TagListIndex tagListIndex, TraitIndex traitIndex)
        {
            return Contains(ref GetTagList(tagListIndex), traitIndex);
        }
    }
}
