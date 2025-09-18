namespace MotionMatching
{
    public partial struct Binary
    {
        public struct Trait
        {
            public TypeIndex typeIndex;

            internal int payload;

            public bool IsType(TypeIndex typeIndex)
            {
                return this.typeIndex.Equals(typeIndex);
            }
        }

        public struct TraitIndex
        {
            internal int value;

            /// <summary>
            /// Determines whether two trait indices are equal.
            /// </summary>
            /// <param name="traitIndex">The index to compare against the current index.</param>
            /// <returns>True if the specified index is equal to the current index; otherwise, false.</returns>
            public bool Equals(TraitIndex traitIndex)
            {
                return value == traitIndex.value;
            }

            /// <summary>
            /// Implicit conversion from a trait index to an integer value.
            /// </summary>
            public static implicit operator int(TraitIndex traitIndex)
            {
                return traitIndex.value;
            }

            /// <summary>
            /// Implicit conversion from an integer value to a trait index.
            /// </summary>
            public static implicit operator TraitIndex(int traitIndex)
            {
                return Create(traitIndex);
            }

            internal static TraitIndex Create(int traitIndex)
            {
                return new TraitIndex { value = traitIndex };
            }

            /// <summary>
            /// Empty codebook index.
            /// </summary>
            public static TraitIndex Empty => -1;
        }

        public int numTraits => traits.Length;

        public TraitIndex GetTraitIndex<T>(T value)
            where T : struct
        {
            var typeIndex = GetTypeIndex<T>();

            if (typeIndex != TypeIndex.Invalid)
            {
                int numTraits = this.numTraits;

                for (int i = 0; i < numTraits; ++i)
                {
                    if (traits[i].IsType(typeIndex))
                    {
                        if (IsPayload(ref value, traits[i].payload))
                        {
                            return i;
                        }
                    }
                }
            }

            return TraitIndex.Empty;
        }
    }
}
