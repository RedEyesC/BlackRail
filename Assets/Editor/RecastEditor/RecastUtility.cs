using UnityEngine;

namespace GameEditor.RecastEditor
{
    public enum AREATYPE
    {
        None = 0,
        Walke = 1,
    }

    public enum RcAxis
    {
        AXIS_X = 0,
        AXIS_Y = 1,
        AXIS_Z = 2
    };

    public enum VertIndex
    {
        In = 0,
        InRow = 7,
        P1 = 14,
        P2 = 21,
    };
    internal class RecastUtility
    {
        static Vector3 tempVec1 = new Vector3();
        static Vector3 tempVec2 = new Vector3();

        public static Vector3 CalcTriNormal(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            tempVec1 = v1 - v0;
            tempVec2 = v2 - v0;

            return Vector3.Normalize(Vector3.Cross(tempVec1, tempVec2));
        }

        public static void CalcBounds(Vector3[] verts, out Vector3 minBounds, out Vector3 maxBounds)
        {
            minBounds = verts[0];
            maxBounds = verts[0];
            for (int i = 1; i < verts.Length; i++)
            {
                minBounds = Vector3.Min(minBounds, verts[i]);
                maxBounds = Vector3.Max(maxBounds, verts[i]);
            }
        }

        public static void CalcGridSize(Vector3 minBounds, Vector3 maxBounds, float cellSize, out int sizeX, out int sizeZ)
        {
            sizeX = (int)((maxBounds[0] - minBounds[0]) / cellSize + 0.5f);
            sizeZ = (int)((maxBounds[2] - minBounds[2]) / cellSize + 0.5f);
        }

        //a的包围盒包含于b的包围盒 或者 a的包围盒与b的包围盒相交
        public static bool OverlapBounds(Vector3 aMin, Vector3 aMax, Vector3 bMin, Vector3 bMax)
        {
            return aMin[0] <= bMax[0] && aMax[0] >= bMin[0] &&
                aMin[1] <= bMax[1] && aMax[1] >= bMin[1] &&
                aMin[2] <= bMax[2] && aMax[2] >= bMin[2];
        }

        public static void DividePoly(Vector3[] VertList, int inVertsCount, float axisOffset, RcAxis axis, out int outVerts1Count, out int outVerts2Count, out Vector3[] outVertList1, out Vector3[] outVertList2)
        {

            Vector3[] outList1 = new Vector3[7];
            Vector3[] outList2 = new Vector3[7];
            float[] inVertAxisDelta = new float[12];
            //多边形顶点到切割线的距离
            for (int inVert = 0; inVert < inVertsCount; ++inVert)
            {
                inVertAxisDelta[inVert] = axisOffset - VertList[inVert][(int)axis];
            }


            int poly1Vert = 0;
            int poly2Vert = 0;
            for (int inVertA = 0, inVertB = inVertsCount - 1; inVertA < inVertsCount; inVertB = inVertA, ++inVertA)
            {
                // 通过与切割线的距离判断是否都是同一侧的
                bool sameSide = (inVertAxisDelta[inVertA] >= 0) == (inVertAxisDelta[inVertB] >= 0);

                if (!sameSide)
                {
                    // b的距离占a和b距离之和的比例 ，因为a，b不在同一侧，a，b距离之和等于b-a
                    float s = inVertAxisDelta[inVertB] / (inVertAxisDelta[inVertB] - inVertAxisDelta[inVertA]);

                    //计算出中间点坐标
                    outList1[poly1Vert] = VertList[inVertB] + (VertList[inVertA] - VertList[inVertB]) * s;
                    outList2[poly2Vert] = outList1[poly1Vert];

                    poly1Vert++;
                    poly2Vert++;


                    //根据a点距离把a点添加到划分好的三角形
                    if (inVertAxisDelta[inVertA] > 0)
                    {
                        outList1[poly1Vert] = VertList[inVertA];
                        poly1Vert++;
                    }
                    else if (inVertAxisDelta[inVertA] < 0)
                    {
                        outList2[poly2Vert] = VertList[inVertA];
                        poly2Vert++;
                    }
                }
                else
                {

                    if (inVertAxisDelta[inVertA] >= 0)
                    {
                        outList1[poly1Vert] = VertList[inVertA];
                        poly1Vert++;
                        if (inVertAxisDelta[inVertA] != 0)
                        {
                            continue;
                        }
                    }
                    outList2[poly2Vert] = VertList[inVertA];
                    poly2Vert++;
                }
            }

            outVerts1Count = poly1Vert;
            outVerts2Count = poly2Vert;

            outVertList1 = outList1;
            outVertList2 = outList2;

        }

        public static int RcGetDirOffsetX(int direction)
        {
            int[] offset = { -1, 0, 1, 0 };
            return offset[direction & 0x03];
        }

        public static int RcGetDirOffsetY(int direction)
        {
            int[] offset = { 0, 1, 0, -1 };
            return offset[direction & 0x03];
        }

        public static int RcGetHeightFieldSpanCount(RcHeightfield heightfield)
        {

            int numCols = heightfield.width * heightfield.height;
            int spanCount = 0;
            for (int columnIndex = 0; columnIndex < numCols; ++columnIndex)
            {
                for (RcSpan span = heightfield.spans[columnIndex]; span != null; span = span.next)
                {
                    if (span.areaID != AREATYPE.None)
                    {
                        spanCount++;
                    }
                }
            }
            return spanCount;
        }

        public static void RcSetCon(CompactSpan span, int dir, int i)
        {
            //每个方向用六位存放 
            int shift = dir * 6;
            int con = span.con;
            span.con = ((con & ~(0x3f << shift)) | (i & 0x3f) << shift);
        }


        public static int RcGetCon(CompactSpan span, int dir)
        {
            //每个方向用六位存放 
            int shift = dir * 6;
            return (span.con >> shift) & 0x3f;
        }

        public static void RcSwap<T>(T a, T b)
        {
            T t = a;
            a = b;
            b = t;
        }

        public static int Prev(int i, int n)
        {
            return i - 1 >= 0 ? i - 1 : n - 1;
        }

        public static int Next(int i, int n)
        {
            return i + 1 < n ? i + 1 : 0;
        }

        public static int Area2(int[] a, int[] b, int[] c)
        {
            //向量ab 叉乘向量 ac   a x b =（x1y2 - x2y1） = |a||b|sin(a,b)，叉乘结果小于0,则c在ab左侧
            return (b[0] - a[0]) * (c[2] - a[2]) - (c[0] - a[0]) * (b[2] - a[2]);
        }


        public static bool Left(int[] a, int[] b, int[] c)
        {
            return Area2(a, b, c) < 0;
        }

        public static bool LeftOn(int[] a, int[] b, int[] c)
        {
            return Area2(a, b, c) <= 0;
        }

        public static bool Collinear(int[] a, int[] b, int[] c)
        {
            return Area2(a, b, c) == 0;
        }

        public static bool Xorb(bool x, bool y)
        {
            return !x ^ !y;
        }

        public static bool IntersectProp(int[] a, int[] b, int[] c, int[] d)
        {

            if (Collinear(a, b, c) || Collinear(a, b, d) ||
                Collinear(c, d, a) || Collinear(c, d, b))
                return false;

            return Xorb(Left(a, b, c), Left(a, b, d)) && Xorb(Left(c, d, a), Left(c, d, b));
        }

        //当a.b.c 共线，且c在ab线段上时返回ture
        public static bool Between(int[] a, int[] b, int[] c)
        {
            if (!Collinear(a, b, c))
                return false;

            if (a[0] != b[0])
                return ((a[0] <= c[0]) && (c[0] <= b[0])) || ((a[0] >= c[0]) && (c[0] >= b[0]));
            else
                return ((a[2] <= c[2]) && (c[2] <= b[2])) || ((a[2] >= c[2]) && (c[2] >= b[2]));
        }

        //当线段ab和cd 相交 ，返回true。
        //loose 标记是否启用宽松判断条件
        public static bool Intersect(int[] a, int[] b, int[] c, int[] d, bool loose = false)
        {

            if (IntersectProp(a, b, c, d))
                return true;


            if (!loose && (Between(a, b, c) || Between(a, b, d) || Between(c, d, a) || Between(c, d, b)))
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        //判断pj是否在以pi为顶点 以pi->pin1 、pi->pi1边组成的锥形范围内 。
        //loose 标记是否启用宽松判断条件
        public static bool InCone(int[] pi, int[] pi1, int[] pin1, int[] pj, bool loose = false)
        {

            if (LeftOn(pin1, pi, pi1))
            {
                if (loose)
                {
                    return LeftOn(pi, pj, pin1) && LeftOn(pj, pi, pi1);
                }
                else
                {
                    return Left(pi, pj, pin1) && Left(pj, pi, pi1);
                }
            }

            return !(LeftOn(pi, pj, pi1) && LeftOn(pj, pi, pin1));
        }

    }
}
