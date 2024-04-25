namespace GameFramework.Common
{
    internal class Utils
    {

        public static string GetRoleModelPath(int id)
        {
            return string.Format("model/role/{0}/{0}", 1000+id);
        }

    }
}
