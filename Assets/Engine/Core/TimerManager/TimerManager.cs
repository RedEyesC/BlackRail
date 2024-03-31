using System;
using System.Collections.Generic;

namespace GameFramework.Runtime
{

    public class TimerManager : GameModule
    {
        private static int _idCounter = 0;
        private static float _nowTime = 0;

        private static List<TimerEvent> _timerCache = new List<TimerEvent>();
        private static Dictionary<int, TimerEvent> _timerMap = new Dictionary<int, TimerEvent>();

        private static List<int> _keysToRemove = new List<int>();
        public override void Destroy()
        {
            ClearAllTimer();
        }

        public override void Start()
        {

        }

        public override void Update(float elapseSeconds, float realElapseSeconds)
        {
            _nowTime += elapseSeconds;

            //c# 无法实现在遍历字典的同时删除字典的值，额外存放一份字典的key用于遍历
            _keysToRemove = new List<int>(_timerMap.Keys);

            foreach (int key in _keysToRemove)
            {

                TimerEvent info = _timerMap[key];

                if (info.callBack != null)
                {

                    if (_nowTime > info.tickTime)
                    {
                        System.Action callBack = info.callBack;
                        if (info.time > 0)
                        {
                            info.tickTime = _nowTime + info.time;
                        }
                        else
                        {
                            ClearTimer(key);
                        }

                        if (info.tick)
                        {
                            info.tick = false;
                            callBack();
                            info.tick = true;
                        }

                    }

                }
            }

        }

        public static int SetTimeout(System.Action callback, float timeout)
        {
            TimerEvent info = PopOneTimerInfo();
            info.time = 0;
            info.callBack = callback;
            info.tickTime = _nowTime + timeout;

            _timerMap.Add(info.id, info);

            return info.id;
        }

        public static int SetInterval(System.Action callback, float interval)
        {
            TimerEvent info = PopOneTimerInfo();
            info.time = interval;
            info.callBack = callback;
            info.tickTime = _nowTime + interval;

            _timerMap.Add(info.id, info);

            return info.id;
        }

        public static TimerEvent PopOneTimerInfo()
        {
            TimerEvent timerInfo = null;
            foreach (TimerEvent timerEvent in _timerCache)
            {
                if (timerEvent.callBack == null)
                {
                    timerInfo = timerEvent;
                    break;
                }
            }

            _idCounter++;

            if (timerInfo == null)
            {
                timerInfo = new TimerEvent(null, 0, 0, true, _idCounter);
                _timerCache.Add(timerInfo);
            }
            else
            {
                timerInfo.id = _idCounter;
                timerInfo.tick = true;
            }

            return timerInfo;
        }

        public static void ClearTimer(int Id)
        {
            if (_timerMap.ContainsKey(Id))
            {
                TimerEvent timerEvent = _timerMap[Id];

                if (timerEvent != null)
                {
                    _timerMap.Remove(Id);
                    timerEvent.callBack = null;
                }

            }

        }

        public static void ClearAllTimer()
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
