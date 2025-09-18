using System;
using Unity.Collections;
using IntervalIndex = MotionMatching.Binary.IntervalIndex;

namespace MotionMatching
{
    /// <summary>
    /// A query result is wrapper around a pose sequence and
    /// is the result of a semantic filtering query.
    /// </summary>
    /// <seealso cref="MotionSynthesizer.Query"/>
    /// <seealso cref="PoseSequence"/>
    public struct QueryResult : IDisposable
    {
        public NativeList<PoseSequence> sequences;

        public FixedString64Bytes debugName;

        public void Dispose()
        {
            sequences.Dispose();
        }

        public int length => sequences.Length;

        public PoseSequence this[int index] => sequences[index];

        public static QueryResult Create()
        {
            return new QueryResult { sequences = new NativeList<PoseSequence>(Allocator.Temp) };
        }

        public void Add(IntervalIndex intervalIndex, int firstFrame, int numFrames)
        {
            sequences.Add(PoseSequence.Create(intervalIndex, firstFrame, numFrames));
        }

        public static QueryResult Empty => Create();

        public static implicit operator QueryResult(QueryTraitExpression expression)
        {
            return expression.Execute();
        }
    }
}
