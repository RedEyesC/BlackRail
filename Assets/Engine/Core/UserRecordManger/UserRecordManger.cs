using System.Collections.Generic;
using GameFramework.Common;
using UnityEngine;

namespace GameFramework.UserRecord
{
    internal class UserRecordManger : GameModule
    {
        public override int priority => 9;

        private static Dictionary<string, string> _clientConfigMap = new Dictionary<string, string>();

        public override void Destroy() { }

        public override void Start()
        {
            string configPath = FileUtils.CombinePath(Application.streamingAssetsPath, "config.game");
            byte[] data = FileUtils.GetFileData(configPath);
            JsonConverter.ParseJson(data, ref _clientConfigMap);
        }

        public static string GetClientConfig(string key, string defaultValue = "")
        {
            string value = "";
            if (_clientConfigMap.TryGetValue(key, out value))
                return value;
            return defaultValue;
        }

        public static int GetClientConfigInt(string key, int defaultValue = 0)
        {
            string value = "";
            if (_clientConfigMap.TryGetValue(key, out value))
            {
                int outVal = 0;
                if (int.TryParse(value, out outVal))
                    return outVal;
            }
            return defaultValue;
        }

        public static bool GetClientConfigBool(string key, bool defaultValue = false)
        {
            string value = "";
            if (_clientConfigMap.TryGetValue(key, out value))
            {
                bool outVal = false;
                if (bool.TryParse(value, out outVal))
                    return outVal;
            }
            return defaultValue;
        }
    }
}
