using System;

namespace GameEditor.DetourEditor
{
    internal class DetourUtility
    {
        public static void DtVsub(float[] dest, float[] v1, float[] v2)
        {

            dest[0] = v1[0] - v2[0];
            dest[1] = v1[1] - v2[1];
            dest[2] = v1[2] - v2[2];
        }


        public static void DtVadd(float[] dest, float[] v1, float[] v2)
        {

            dest[0] = v1[0] + v2[0];
            dest[1] = v1[1] + v2[1];
            dest[2] = v1[2] + v2[2];
        }


        public static void DtVcopy(float[] a, float[] b)
        {
            a[0] = b[0];
            a[1] = b[1];
            a[2] = b[2];
        }

        public static float DtVlenSqr(float[] v)
        {
            return v[0] * v[0] + v[1] * v[1] + v[2] * v[2];
        }

        public static void DtVlerp(float[] dest, float[] v1, float[] v2, float t)
        {

            dest[0] = v1[0] + (v2[0] - v1[0]) * t;
            dest[1] = v1[1] + (v2[1] - v1[1]) * t;
            dest[2] = v1[2] + (v2[2] - v1[2]) * t;
        }

        public static bool DtClosestHeightPointTriangle(float[] p, float[] a, float[] b, float[] c, ref float h)
        {
            const float EPS = 1e-6f;
            float[] v0 = new float[3];
            float[] v1 = new float[3];
            float[] v2 = new float[3];

            DtVsub(v0, c, a);
            DtVsub(v1, b, a);
            DtVsub(v2, p, a);

            // Compute scaled barycentric coordinates
            float denom = v0[0] * v1[2] - v0[2] * v1[0];
            if (Math.Abs(denom) < EPS)
                return false;

            float u = v1[2] * v2[0] - v1[0] * v2[2];
            float v = v0[0] * v2[2] - v0[2] * v2[0];

            if (denom < 0)
            {
                denom = -denom;
                u = -u;
                v = -v;
            }

            // If point lies inside the triangle, return interpolated ycoord.
            if (u >= 0.0f && v >= 0.0f && (u + v) <= denom)
            {
                h = a[1] + (v0[1] * u + v1[1] * v) / denom;
                return true;
            }
            return false;
        }

        public static float DtDistancePtSegSqr2D(float[] pt, float[] p, float[] q, ref float t)
        {
            float pqx = q[0] - p[0];
            float pqz = q[2] - p[2];
            float dx = pt[0] - p[0];
            float dz = pt[2] - p[2];
            float d = pqx * pqx + pqz * pqz;
            t = pqx * dx + pqz * dz;
            if (d > 0) t /= d;
            if (t < 0) t = 0;
            else if (t > 1) t = 1;
            dx = p[0] + t * pqx - pt[0];
            dz = p[2] + t * pqz - pt[2];
            return dx * dx + dz * dz;
        }
    }
}
