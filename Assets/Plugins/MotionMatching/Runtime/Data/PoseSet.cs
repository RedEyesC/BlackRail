using System;
using Unity.Collections;

namespace MotionMatching
{
    public struct PoseSet : IDisposable
    {
        public NativeArray<PoseSequence> sequences;
        public Allocator allocator;

        public FixedString64Bytes debugName;

        //public DebugIdentifier debugIdentifier { get; set; }

        public static PoseSet Create(QueryResult result, Allocator allocator)
        {
            return new PoseSet()
            {
                sequences = result.sequences.ToArray(allocator),
                allocator = allocator,
                debugName = result.debugName,
                //debugIdentifier = DebugIdentifier.Invalid
            };
        }

        public void Dispose()
        {
            if (allocator != Allocator.Invalid)
            {
                sequences.Dispose();
            }
        }

        public static implicit operator PoseSet(QueryResult result)
        {
            return Create(result, Allocator.Persistent);
        }

        public static implicit operator PoseSet(QueryTraitExpression expression)
        {
            return expression.Execute();
        }

        //public void WriteToStream(Buffer buffer)
        //{
        //    buffer.WriteNativeArray(sequences, allocator);
        //    buffer.Write(debugName);
        //}

        //public void ReadFromStream(Buffer buffer)
        //{
        //    sequences = buffer.ReadNativeArray<PoseSequence>(out allocator);
        //    debugName = buffer.ReadNativeString64();
        //}
    }
}
