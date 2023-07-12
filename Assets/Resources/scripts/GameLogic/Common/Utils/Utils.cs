namespace GameFramework.Runtime
{
    internal class Utils
    {

        public static string GetUIPrefabPath(string pkgName ,string comName)
        {
            return  string.Format("ui/{0}/{1}",pkgName, comName);
        }

        public static string GetMapPath(int mapId)
        {
            return string.Format("Resources/map/{0}/{0}", mapId);
        }

    }
}
