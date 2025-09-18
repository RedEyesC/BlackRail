using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace MotionMatching
{
    public static unsafe class Extensions
    {
        public static ref T At<T>(this NativeList<T> nativeList, int index)
            where T : unmanaged
        {
            return ref UnsafeUtility.ArrayElementAsRef<T>(NativeListUnsafeUtility.GetUnsafePtr(nativeList), index);
        }
    }
}
