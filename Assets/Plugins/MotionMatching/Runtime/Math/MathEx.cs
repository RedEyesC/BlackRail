using MotionMatching;

namespace Unity.Mathematics
{
    public static class MathEx
    {
        internal const float epsilon = 1.192092896e-07f;

        public static float3 forward
        {
            get { return new float3(0.0f, 0.0f, 1.0f); }
        }

        public static float3 rotateVector(quaternion q, float3 v)
        {
            return math.mul(q, v);
        }

        public static quaternion forRotation(float3 v1, float3 v2)
        {
            float k = math.sqrt(math.lengthsq(v1) * math.lengthsq(v2));

            float d = math.clamp(math.dot(v1, v2), -k, k);

            if (k < epsilon)
            {
                // Test for zero-length input, to avoid infinite loop.  Return identity (not much else to do)
                return quaternion.identity;
            }
            else if (math.abs(d + k) < epsilon * k)
            {
                // Test if v1 and v2 were antiparallel, to avoid singularity
                float3 m = orthogonal(v1);
                quaternion q1 = forRotation(m, v2);
                quaternion q2 = forRotation(v1, m);
                return math.mul(q1, q2);
            }
            else
            {
                // This means that xyz is k.sin(theta),
                // where a is the unit axis of rotation,
                // which equals 2k.sin(theta/2)cos(theta/2).
                float3 v = math.cross(v1, v2);

                // We then put 2kcos^2(theta/2) =
                // dot+k into the w part of the
                // quaternion and normalize.
                return math.normalize(new quaternion(v.x, v.y, v.z, d + k));
            }
        }

        internal static float3 orthogonal(float3 v)
        {
            float3 vn = math.normalize(v);

            if (vn.z < 0.5f && vn.z > -0.5f)
            {
                return math.normalize(new float3(-vn.y, vn.x, 0));
            }
            else
            {
                return math.normalize(new float3(-vn.z, 0, vn.x));
            }
        }

        public static int truncToInt(float value)
        {
            return (int)value;
        }

        public static quaternion conjugate(quaternion q)
        {
            return new quaternion(-q.value.x, -q.value.y, -q.value.z, q.value.w);
        }

        internal static quaternion negate(quaternion q)
        {
            return new quaternion(-q.value.x, -q.value.y, -q.value.z, -q.value.w);
        }

        internal static float squared(float value)
        {
            return value * value;
        }

        public static float3 axisAngle(quaternion q, out float angle)
        {
            float3 v = new float3(q.value.x, q.value.y, q.value.z);
            float sinHalfAngle = math.length(v);
            if (sinHalfAngle < epsilon)
            {
                angle = 0.0f;
                return new float3(1.0f, 0.0f, 0.0f);
            }
            else
            {
                angle = 2.0f * math.atan2(sinHalfAngle, q.value.w);
                return v * (1.0f / sinHalfAngle);
            }
        }

        public static AffineTransform lerp(AffineTransform lhs, AffineTransform rhs, float theta)
        {
            return new AffineTransform(math.lerp(lhs.t, rhs.t, theta), math.slerp(lhs.q, rhs.q, theta));
        }
    }
}
