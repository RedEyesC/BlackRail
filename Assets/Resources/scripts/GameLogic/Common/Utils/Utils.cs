namespace GameFramework.Runtime
{
    internal class Utils
    {

        public static string GetUIPrefabPath(string pkgName ,string comName)
        {
            return  string.Format("ui/{0}/{1}",pkgName, comName);
        }

    }
}
