using System;
using Unity.Collections;
using UnityEngine.Assertions;
using TagListIndex = MotionMatching.Binary.TagListIndex;
using TraitIndex = MotionMatching.Binary.TraitIndex;

namespace MotionMatching
{
    public struct QueryTraitExpression : IDisposable
    {
        MemoryRef<Binary> binary;

        struct Constraint
        {
            public int chainLength;

            public bool include;

            public TraitIndex traitIndex;

            public static Constraint Create(TraitIndex traitIndex, bool include = true)
            {
                return new Constraint { traitIndex = traitIndex, include = include };
            }
        }

        [DeallocateOnJobCompletion]
        NativeList<Constraint> constraints;

        FixedString64Bytes debugName;

        int chainIndex;

        public void Dispose()
        {
            constraints.Dispose();
        }

        public QueryTraitExpression And<T>(T value)
            where T : struct
        {
            var traitIndex = binary.Ref.GetTraitIndex(value);

            Insert(Constraint.Create(traitIndex));

            return this;
        }

        public QueryTraitExpression Except<T>(T value)
            where T : struct
        {
            var traitIndex = binary.Ref.GetTraitIndex(value);

            Insert(Constraint.Create(traitIndex, false));

            return this;
        }

        /// <summary>
        /// Appends a tag trait constraint to the existing tag trait expression.
        /// </summary>
        /// <remarks>
        /// An "or" clause matches all intervals from the motion library
        /// that contain the trait passed as argument or any constraint
        /// that has been previously specified as part of the tag trait expression.
        /// <example>
        /// <code>
        /// synthesizer.Query.Where(
        ///     Locomotion.Default).Or(Locomotion.Crouching));
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="MotionSynthesizer.Query"/>
        public QueryTraitExpression Or<T>(T value)
            where T : struct
        {
            var traitIndex = binary.Ref.GetTraitIndex(value);

            Append(Constraint.Create(traitIndex));

            return this;
        }

        /// <summary>
        /// Introduces a marker trait expression that selects based on an "at" clause.
        /// </summary>
        /// <remarks>
        /// An "at" clause matches all single poses from the motion library
        /// that contain the marker type passed as argument.
        /// <example>
        /// <code>
        /// synthesizer.Query.At<Loop>();
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="MotionSynthesizer.Query"/>
        public QueryMarkerExpression At<T>()
            where T : struct
        {
            return marker.At<T>();
        }

        /// <summary>
        /// Introduces a marker trait expression that selects based on a "before" clause.
        /// </summary>
        /// <remarks>
        /// A "before" clause matches all poses from the motion library
        /// that come "before" the specified marker type. The result is most
        /// likely a partial interval, assuming that the marker has been
        /// not been placed at the very beginning or end of an interval.
        /// <example>
        /// <code>
        /// synthesizer.Query.At<Loop>();
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="MotionSynthesizer.Query"/>
        public QueryMarkerExpression Before<T>()
            where T : struct
        {
            return marker.Before<T>();
        }

        /// <summary>
        /// Introduces a marker trait expression that selects based on an "after" clause.
        /// </summary>
        /// <remarks>
        /// An "after" clause matches all poses from the motion library
        /// that come "after" the specified marker type. The result is most
        /// likely a partial interval, assuming that the marker has been
        /// not been placed at the very beginning or end of an interval.
        /// <example>
        /// <code>
        /// synthesizer.Query.At<Loop>();
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="MotionSynthesizer.Query"/>
        public QueryMarkerExpression After<T>()
            where T : struct
        {
            return marker.After<T>();
        }

        QueryMarkerExpression marker
        {
            get { return QueryMarkerExpression.Create(ref binary.Ref, Execute()); }
        }

        internal static QueryTraitExpression Create(ref Binary binary, FixedString64Bytes debugName = default(FixedString64Bytes))
        {
            var constraints = new NativeList<Constraint>(Allocator.Temp);

            return new QueryTraitExpression()
            {
                binary = MemoryRef<Binary>.Create(ref binary),
                constraints = constraints,
                debugName = debugName,
            };
        }

        ref Binary Binary => ref binary.Ref;

        bool MatchesConstraints(TagListIndex tagListIndex)
        {
            ref Binary binary = ref Binary;

            int numConstraints = constraints.Length;

            int readIndex = 0;

            while (readIndex < numConstraints)
            {
                int chainLength = constraints[readIndex].chainLength;

                int matchIndex = 0;

                while (matchIndex < chainLength)
                {
                    var constraint = constraints[readIndex + matchIndex];

                    var contains = binary.Contains(tagListIndex, constraint.traitIndex);

                    if (constraint.include != contains)
                    {
                        break;
                    }

                    matchIndex++;
                }

                if (matchIndex == chainLength)
                {
                    return true;
                }

                readIndex += chainLength;
            }

            return false;
        }

        public QueryResult Execute()
        {
            var queryResult = QueryResult.Create();
            queryResult.debugName = debugName;

            ref Binary binary = ref Binary;

            int numIntervals = binary.numIntervals;

            for (int i = 0; i < numIntervals; ++i)
            {
                ref var interval = ref binary.GetInterval(i);

                if (MatchesConstraints(interval.tagListIndex))
                {
                    queryResult.Add(i, interval.firstFrame, interval.numFrames);
                }
            }

            constraints.Dispose();

            return queryResult;
        }

        void Insert(Constraint constraint)
        {
            if (constraints.Length > 0)
            {
                var link = constraints[chainIndex];

                constraints.Add(link);

                constraint.chainLength = link.chainLength + 1;

                constraints[chainIndex] = constraint;
            }
            else
            {
                constraint.chainLength = 1;

                constraints.Add(constraint);
            }
        }

        void Append(Constraint constraint)
        {
            Assert.IsTrue(constraints.Length > 0);

            constraint.chainLength = 1;

            chainIndex += constraints[chainIndex].chainLength;

            Assert.IsTrue(chainIndex == constraints.Length);

            constraints.Add(constraint);
        }
    }
}
