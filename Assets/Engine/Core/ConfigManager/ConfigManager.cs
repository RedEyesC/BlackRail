using GameFramework.Asset;
using GameFramework.Common;
using System.Collections.Generic;

namespace GameFramework.Config
{

    public class ConfigManager : GameModule
    {

        private static readonly string bundName = "Config.ab";

        private static Dictionary<string, ConfigBase> configMap = new Dictionary<string, ConfigBase>();

        public new int priority = 7;

        public static ConfigBase Get(string key)
        {
            if (!configMap.ContainsKey(key))
            {
                ConfigBase instance = new ConfigBase(bundName, key);
                configMap[key] = instance;
            }

            return configMap[key];

        }


        public override void Destroy()
        {

        }

        public override void Start()
        {
            AssetManager.LoadAllAssetAsync(bundName);
        }

        public override void Update(float nowTime, float elapseSeconds)
        {

        }

        public static void LoadConfig()
        {

        }
    }
}