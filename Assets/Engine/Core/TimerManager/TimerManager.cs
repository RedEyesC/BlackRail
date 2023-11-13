using System;
using System.Collections.Generic;

namespace GameFramework.Runtime
{

    public class TimerManager : GameModule
    {
        private int _idCounter = 0;
        private float _nowTime = 0;

        private List<TimerEvent> _timerCache = new List<TimerEvent>();
        private Dictionary<int, TimerEvent> _timerMap = new Dictionary<int, TimerEvent>();

        public override void Destroy()
        {
            ClearAllTimer();
        }

        public override void Start()
        {

        }

        public override void Update(float elapseSeconds, float realElapseSeconds)
        {
            _nowTime = elapseSeconds;

            foreach(KeyValuePair<int,TimerEvent> kvp in _timerMap)
            {
                if (kvp.Value.callBack != null)
                {
                    if (kvp.Value.tickTime < _nowTime)
                    {
                        kvp.Value.callBack();

                        if (kvp.Value.time > 0)
                        {
                            kvp.Value.tickTime = _nowTime + kvp.Value.time;
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
            info.callBack = callback;
            info.tickTime = _nowTime + timeout;

            _timerMap.Add(info.id, info);

            return info.id;
        }

        public TimerEvent PopOneTimerInfo()
        {
            TimerEvent timerInfo = null;
            foreach (TimerEvent timerEvent in _timerCache)
            {
                if (timerEvent.callBack == null)
                {
                    timerInfo = timerEvent;
                }
            }

            _idCounter++;

            if (timerInfo == null)
            {
                timerInfo = new TimerEvent(null, 0, 0, _idCounter);
                _timerCache.Add(timerInfo);
            }
            else
            {
                timerInfo.id = _idCounter;
            }

            return timerInfo;
        }

        public void ClearTimer(int Id) { 
            TimerEvent timerEvent = _timerMap[Id];

            if (timerEvent != null)
            {
                _timerMap.Remove(Id);
                timerEvent.callBack = null;
            }   
        }

        public void ClearAllTimer()
        {
            foreach (KeyValuePair<int, TimerEvent> kvp in _timerMap)
            { 
                kvp.Value.callBack = null;
            
            }
            
            _timerMap.Clear();
            _timerCache.Clear();
            _idCounter = 0;
        }
    }
}
