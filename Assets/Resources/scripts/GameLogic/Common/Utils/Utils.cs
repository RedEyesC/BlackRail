namespace GameFramework.Runtime
{
    internal class Utils
    {

        public static string GetUIPrefabPath(string pkgName, string comName)
        {
            return string.Format("UI/{0}/{1}", pkgName, comName);
        }

        public static string GetUIBundlePath(string pkgName)
        {
            return string.Format("UI/{0}.ab", pkgName);
        }


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
