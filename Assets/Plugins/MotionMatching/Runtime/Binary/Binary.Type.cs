using Unity.Burst;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Assertions;

namespace MotionMatching
{
    public partial struct Binary
    {
        internal struct Type
        {
            public int nameIndex;
            public int hashCode;
            public int numBytes;
            public int fieldIndex;
            public int numFields;
        }

        public struct TypeIndex
        {
            internal int value;

            /// <summary>
            /// Determines if the given type index is valid or not.
            /// </summary>
            /// <returns>True if the type index is valid; false otherwise.</returns>
            public bool IsValid => value != Invalid;

            /// <summary>
            /// Determines whether two type indices are equal.
            /// </summary>
            /// <param name="typeIndex">The index to compare against the current index.</param>
            /// <returns>True if the specified index is equal to the current index; otherwise, false.</returns>
            public bool Equals(TypeIndex typeIndex)
            {
                return value == typeIndex.value;
            }

            /// <summary>
            /// Implicit conversion from a type index to an integer value.
            /// </summary>
            public static implicit operator int(TypeIndex typeIndex)
            {
                return typeIndex.value;
            }

            /// <summary>
            /// Implicit conversion from an integer value to a type index.
            /// </summary>
            public static implicit operator TypeIndex(int typeIndex)
            {
                return Create(typeIndex);
            }

            internal static TypeIndex Create(int typeIndex)
            {
                return new TypeIndex { value = typeIndex };
            }

            /// <summary>
            /// Invalid type index.
            /// </summary>
            public static TypeIndex Invalid => -1;
        }

        internal TypeIndex GetTypeIndex(int hashCode)
        {
            int numTypes = this.numTypes;

            for (int i = 0; i < numTypes; ++i)
            {
                if (GetType(i).hashCode == hashCode)
                {
                    return TypeIndex.Create(i);
                }
            }

            return TypeIndex.Invalid;
        }

        public TypeIndex GetTypeIndex<T>()
            where T : struct
        {
            return GetTypeIndex(BurstRuntime.GetHashCode32<T>());
        }

        internal int numTypes => types.Length;

        internal ref Type GetType(int index)
        {
            Assert.IsTrue(index < numTypes);
            return ref types[index];
        }

        public int numTags => tags.Length;

        public ref Tag GetTag(TagIndex tagIndex)
        {
            Assert.IsTrue(tagIndex < numTags);
            return ref tags[tagIndex];
        }
    }
}
