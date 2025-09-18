using System.Reflection;
using Unity.Mathematics;

namespace MotionMatching
{
    public struct AffineTransform
    {
        public float3 t;
        public quaternion q;

        public AffineTransform(float3 t_, quaternion q_)
        {
            t = t_;
            q = q_;
        }

        public static AffineTransform Create(float3 t, quaternion q)
        {
            return new AffineTransform(t, q);
        }

        public static AffineTransform identity
        {
            get { return new AffineTransform(new float3(0.0f, 0.0f, 0.0f), quaternion.identity); }
        }

        public static AffineTransform operator *(AffineTransform lhs, AffineTransform rhs)
        {
            return new AffineTransform(lhs.transform(rhs.t), math.mul(lhs.q, rhs.q));
        }

        public float3 transform(float3 p)
        {
            return MathEx.rotateVector(q, p) + t;
        }

        public AffineTransform inverse()
        {
            quaternion inverseQ = MathEx.conjugate(q);
            return new AffineTransform(MathEx.rotateVector(inverseQ, -t), inverseQ);
        }

        public AffineTransform inverseTimes(AffineTransform rhs)
        {
            quaternion inverseQ = MathEx.conjugate(q);
            return new AffineTransform(MathEx.rotateVector(inverseQ, rhs.t - t), math.mul(inverseQ, rhs.q));
        }
    }
}
