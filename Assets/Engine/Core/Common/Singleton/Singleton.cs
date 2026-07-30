using System;

namespace GameFramework.Common
{
    public class Singleton<T> where T : class, new()
    {
        private static T _instance;

        public static void CreateInstance()
        {
            if (Singleton<T>._instance == null)
            {
                Singleton<T>._instance = Activator.CreateInstance<T>();
            }
        }

        public static void DestroyInstance()
        {
            if (Singleton<T>._instance != null)
            {
                Singleton<T>._instance = null;
            }
        }

        public static T instance
        {
            get
            {
                return Singleton<T>._instance;
            }
        }
    }

}