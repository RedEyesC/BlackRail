using Unity.Collections;

namespace MotionMatching
{
    public struct Query
    {
        MemoryRef<Binary> binary;

        internal static Query Create(ref Binary binary)
        {
            return new Query() { binary = MemoryRef<Binary>.Create(ref binary) };
        }

        public QueryTraitExpression Where<T>(T value)
            where T : struct
        {
            return QueryTraitExpression.Create(ref binary.Ref).And(value);
        }

        public QueryTraitExpression Where<T>(FixedString64Bytes debugName, T value)
            where T : struct
        {
            return QueryTraitExpression.Create(ref binary.Ref, debugName).And(value);
        }

        public QueryMarkerExpression At<T>()
            where T : struct
        {
            return QueryMarkerExpression.Create(ref binary.Ref, QueryResult.Empty).At<T>();
        }

        public QueryMarkerExpression Before<T>()
            where T : struct
        {
            return QueryMarkerExpression.Create(ref binary.Ref, QueryResult.Empty).Before<T>();
        }

        public QueryMarkerExpression After<T>()
            where T : struct
        {
            return QueryMarkerExpression.Create(ref binary.Ref, QueryResult.Empty).After<T>();
        }
    }
}
