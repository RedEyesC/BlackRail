namespace GameFramework.Common
{
    internal class Utils
    {

        public static string GetMapPath(int mapId)
        {
            return string.Format("Assets/Resources/map/{0}/{0}.unity", mapId);
        }

        public static string GetRoleModelPath(int id)
        {
            return string.Format("model/role/{0}/{0}", 1000+id);
        }

    }
}
