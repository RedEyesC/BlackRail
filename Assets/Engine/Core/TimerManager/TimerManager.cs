using System;
using System.Collections.Generic;

namespace GameFramework.Runtime
{

    public class TimerManager : GameModule
    {
        private int _IdCounter = 0;
        private float _NowTime = 0;

        private List<TimerEvent> _TimerCache = new List<TimerEvent>();
        private Dictionary<int, TimerEvent> _TimerMap = new Dictionary<int, TimerEvent>();

        public override void Destroy()
        {
            ClearAllTimer();
        }

        public override void Start()
        {

        }

        public override void Update(float elapseSeconds, float realElapseSeconds)
        {
            _NowTime = elapseSeconds;

            foreach(KeyValuePair<int,TimerEvent> kvp in _TimerMap)
            {
                if (kvp.Value.CallBack != null)
                {
                    if (kvp.Value.TickTime < _NowTime)
                    {
                        kvp.Value.CallBack();

                        if (kvp.Value.Time > 0)
                        {
                            kvp.Value.TickTime = _NowTime + kvp.Value.Time;
                        }
                        else
                        {
                            ClearTimer(kvp.Key);
                        }
                    }

                }
            }

                      
        }

        public int SetTimeout(System.Action callback, float timeout)
        {
            TimerEvent info = PopOneTimerInfo();
            info.CallBack = callback;
            info.TickTime = _NowTime + timeout;

            _TimerMap.Add(info.Id, info);

            return info.Id;
        }

        public TimerEvent PopOneTimerInfo()
        {
            TimerEvent timerInfo = null;
            foreach (TimerEvent timerEvent in _TimerCache)
            {
                if (timerEvent.CallBack == null)
                {
                    timerInfo = timerEvent;
                }
            }

            _IdCounter++;

            if (timerInfo == null)
            {
                timerInfo = new TimerEvent(null, 0, 0, _IdCounter);
                _TimerCache.Add(timerInfo);
            }
            else
            {
                timerInfo.Id = _IdCounter;
            }

            return timerInfo;
        }

        public void ClearTimer(int Id) { 
            TimerEvent timerEvent = _TimerMap[Id];

            if (timerEvent != null)
            {
                _TimerMap.Remove(Id);
                timerEvent.CallBack = null;
            }   
        }

        public void ClearAllTimer()
        {
            foreach (KeyValuePair<int, TimerEvent> kvp in _TimerMap)
            { 
                kvp.Value.CallBack = null;
            
            }
            
            _TimerMap.Clear();
            _TimerCache.Clear();
            _IdCounter = 0;
        }
    }
}
