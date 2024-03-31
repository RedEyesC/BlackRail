

using System;

namespace GameFramework.Runtime
{
    public class TimerEvent
    {
        public float time;
        public int id;
        public float tickTime;
        public bool tick;
        public System.Action callBack;
        public TimerEvent(System.Action callBack, float time, float tickTime, bool tick, int id)
        {
            this.id = id;
            this.time = time;
            this.tickTime = tickTime;
            this.callBack = callBack;
            this.tick = tick;
        }

    }
}
