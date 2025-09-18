using Unity.Collections.LowLevel.Unsafe;

namespace MotionMatching
{
    public partial struct Binary
    {
        public unsafe bool IsPayload<T>(ref T value, int payload)
            where T : struct
        {
            void* src = (byte*)payloads.GetUnsafePtr() + payload;
            void* dst = UnsafeUtility.AddressOf(ref value);
            long size = UnsafeUtility.SizeOf<T>();

            return UnsafeUtility.MemCmp(src, dst, size) == 0;
        }
    }
}
