using System;

namespace GameFramework.Detour
{

    public class DetourUtility
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



        public static bool DtDistancePtPolyEdgesSqr(float[] pt, float[] verts, int nverts, float[] ed, float[] et)
        {
            int i, j;
            bool c = false;
            float[] vi = new float[3];
            float[] vj = new float[3];

            for (i = 0, j = nverts - 1; i < nverts; j = i++)
            {
                Array.Copy(verts, i * 3, vi, 0, 3);
                Array.Copy(verts, i * 3, vj, 0, 3);

                if (((vi[2] > pt[2]) != (vj[2] > pt[2])) &&
                    (pt[0] < (vj[0] - vi[0]) * (pt[2] - vi[2]) / (vj[2] - vi[2]) + vi[0]))
                    c = !c;
                ed[j] = DtDistancePtSegSqr2D(pt, vj, vi, ref et[j]);
            }
            return c;
        }


        //计算pt 到线段p-q的距离
        public static float DtDistancePtSegSqr2D(float[] pt, float[] p, float[] q, ref float t)
        {
            float pqx = q[0] - p[0];
            float pqz = q[2] - p[2];
            float dx = pt[0] - p[0];
            float dz = pt[2] - p[2];
            //计算p-q的长度
            float d = pqx * pqx + pqz * pqz;
            //计算pt-p 投影到p-q上的长度
            t = pqx * dx + pqz * dz;
            //限制投影的在[0,1]
            if (d > 0) t /= d;
            if (t < 0) t = 0;
            else if (t > 1) t = 1;
            //根据投影比例计算出p-q上最接近pt的点
            dx = p[0] + t * pqx - pt[0];
            dz = p[2] + t * pqz - pt[2];
            return dx * dx + dz * dz;
        }


        public static int DtNextPow2(int v)
        {
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;
            return v;
        }

        public static float DtVdist(float[] v1, float[] v2)
        {
            float dx = v2[0] - v1[0];
            float dy = v2[1] - v1[1];
            float dz = v2[2] - v1[2];
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public static bool DtPointInPolygon(float[] pt, float[] verts, int nverts)
        {
            int i, j;
            bool c = false;
            for (i = 0, j = nverts - 1; i < nverts; j = i++)
            {
                int vi = i * 3;
                int vj = j * 3;
                if (((verts[vi + 2] > pt[2]) != (verts[vj + 2] > pt[2])) &&
                    (pt[0] < (verts[vj] - verts[vi]) * (pt[2] - verts[vi + 2]) / (verts[vj + 2] - verts[vi + 2]) + verts[vi]))
                    c = !c;
            }
            return c;
        }

        public static float DtTriArea2D(float[] a, float[] b, float[] c)
        {

            float abx = b[0] - a[0];
            float abz = b[2] - a[2];
            float acx = c[0] - a[0];
            float acz = c[2] - a[2];
            return acx * abz - abx * acz;
        }

        public static bool DtVequal(float[] p0, float[] p1)
        {

            float thr = (float)Math.Sqrt(1.0f / 16384.0f);
            float d = DtVdistSqr(p0, p1);
            return d < thr;
        }

        public static float DtVdistSqr(float[] v1, float[] v2)
        {

            float dx = v2[0] - v1[0];
            float dy = v2[1] - v1[1];
            float dz = v2[2] - v1[2];
            return dx * dx + dy * dy + dz * dz;
        }

        public static void DtVmin(float[] mn, float[] v)
        {

            mn[0] = Math.Min(mn[0], v[0]);
            mn[1] = Math.Min(mn[1], v[1]);
            mn[2] = Math.Min(mn[2], v[2]);
        }

        public static void DtVmax(float[] mn, float[] v)
        {

            mn[0] = Math.Max(mn[0], v[0]);
            mn[1] = Math.Max(mn[1], v[1]);
            mn[2] = Math.Max(mn[2], v[2]);
        }

    }
}
