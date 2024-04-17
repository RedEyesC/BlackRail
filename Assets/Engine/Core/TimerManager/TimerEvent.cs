

using System;

namespace GameFramework.Runtime
{
    public class TimerEvent
    {
        public float interval;
        public int repeat;

        public int id;

        public float elapsed;
        public bool deleted;
        
        public System.Action callBack;
    }
}
