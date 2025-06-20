using LitJson;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameFramework.Common
{
    public static class JsonConverter
    {

        public static bool ParseJson(string jsonStr, ref Dictionary<string, string> dataMap)
        {
            bool result = true;

            string key = "";
            JsonReader reader = new JsonReader(jsonStr);
            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    if (reader.Token.ToString() == "PropertyName")
                    {
                        key = reader.Value.ToString();
                    }
                    else
                    {
                        if (key != "" && !dataMap.ContainsKey(key))
                            dataMap.Add(key, reader.Value.ToString());
                        else
                        {
                            Debug.LogErrorFormat("JsonConverter: Key {0} Error!", key);
                            result = false;
                        }   
                    }
                }
            }
            return result;
        }


        public static bool ParseJson(byte[] jsonData, ref Dictionary<string, string> dataMap)
        {
            if (jsonData == null)
                return false;

            return ParseJson(Encoding.UTF8.GetString(jsonData), ref dataMap);
        }


        public static string ToJsonString(ref Dictionary<string, string> dataMap)
        {
            StringBuilder sb = new StringBuilder();
            JsonWriter writer = new JsonWriter(sb);

            writer.WriteObjectStart();
            foreach (var item in dataMap)
            {
                writer.WritePropertyName(item.Key);
                writer.Write(item.Value);
            }
            writer.WriteObjectEnd();
            
            return sb.ToString();
        }

    }
}
