using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameFramework.Common
{
    public static class Spring
    {
        public static readonly float PIf = 3.14159265358979323846f;
        public static readonly float LN2f = 0.69314718056f;

        public static void SimpleSpringDamperExact(ref float x, ref float v, float xGoal, float halflife, float dt)
        {
            float y = HalflifeToDamping(halflife) / 2.0f;
            float j0 = x - xGoal;
            float j1 = v + j0 * y;
            float eydt = FastNegexp(y * dt);

            x = eydt * (j0 + j1 * dt) + xGoal;
            v = eydt * (v - j1 * y * dt);
        }

        private static float HalflifeToDamping(float halflife, float eps = 1e-5f)
        {
            return (4.0f * LN2f) / (halflife + eps);
        }

        private static float FastNegexp(float x)
        {
            return 1.0f / (1.0f + x + 0.48f * x * x + 0.235f * x * x * x);
        }
    }
}
