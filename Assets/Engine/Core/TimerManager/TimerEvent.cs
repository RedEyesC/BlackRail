

using System;

namespace GameFramework.Runtime
{
    public class TimerEvent
    {
        public int time = 0;
        public int id = 0;
        public float tickTime = 0;
        public System.Action callBack;
        public TimerEvent(System.Action callBack, int time, int id, float tickTime)
        {
            this.id = id;
            this.time = time;
            this.tickTime = tickTime;
            this.callBack = callBack;
        }

    }
}
