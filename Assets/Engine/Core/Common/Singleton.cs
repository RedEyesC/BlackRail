using System;

namespace GameFramework.Runtime
{
    public class Singleton<T> where T : class, new()
    {
        private static T _Instance;

        public static void CreateInstance()
        {
            if (Singleton<T>._Instance == null)
            {
                Singleton<T>._Instance = Activator.CreateInstance<T>();
            }
        }

        public static void DestroyInstance()
        {
            if (Singleton<T>._Instance != null)
            {
                Singleton<T>._Instance = null;
            }
        }

        public static T Instance
        {
            get
            {
                return Singleton<T>._Instance;
            }
        }
    }

}