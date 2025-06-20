using System;
using System.Collections;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;

namespace GameFramework.Common
{
    public static class FileUtils
    {


        public static string CombinePath(string p1, string p2)
        {
            string path = Path.Combine(p1, p2);
            return path.Replace('\\', '/');
        }


        public static byte[] GetFileData(string path)
        {

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
            path = "file:///" + path;
#elif UNITY_IPHONE
            path = "file://" + path;
#endif

            using (UnityWebRequest www = UnityWebRequest.Get(path))
            {
                www.SendWebRequest();

                while (!www.isDone) ;
                return www.downloadHandler.data;
                
            }
        }

        public static void DeletePath(string _path)
        {
            if (Directory.Exists(_path))
            {
                Directory.Delete(_path, true);
            }
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }
    }
}
