namespace GameFramework.Common
{
    public class Math
    {

        public static float Lerpf(float x, float y, float a)
        {
            return (1.0f - a) * x + a * y;
        }

    }
}
