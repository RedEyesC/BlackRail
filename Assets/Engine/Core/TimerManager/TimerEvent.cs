

using System;

namespace GameFramework.Runtime
{
    public class TimerEvent
    {
        public int Time = 0;
        public int Id = 0;
        public float TickTime = 0;
        public System.Action CallBack;
        public TimerEvent(System.Action callback, int timeout, int id, float tickTime)
        {
            Id = id;
            Time = timeout;
            TickTime = tickTime;
            CallBack = callback;
        }

    }
}
