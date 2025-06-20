using GameFramework.Config;

namespace GameLogic
{
    public class Utils
    {
        public static ConfigBase _conf;

        public static string language = "zh";

        public static string Text(int key)
        {
            if (_conf  == null)
            {
                _conf = ConfigManager.Get("Text");
            }

            return _conf.GetValue<string>(key, language);

        }

    }
}
