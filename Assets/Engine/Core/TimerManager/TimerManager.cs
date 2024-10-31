using GameFramework.Common;
using System.Collections.Generic;

namespace GameFramework.Timers
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

    public class TimerManager : GameModule
    {
        private static int _idCounter = 0;

        private static readonly List<TimerEvent> _timerCache = new List<TimerEvent>(100);
        private static readonly Dictionary<int, TimerEvent> _timerMap = new Dictionary<int, TimerEvent>();

        private static readonly List<TimerEvent> _toRemove = new List<TimerEvent>();
        private static readonly Dictionary<int, TimerEvent> _toAdd = new Dictionary<int, TimerEvent>();

        public new int priority = 10;

        public override void Destroy()
        {
            ClearAllTimer();
        }

        public override void Start()
        {

        }

        public override void Update(float nowTime, float elapseSeconds)
        {

            Dictionary<int, TimerEvent>.Enumerator iter;

            if (_timerMap.Count > 0)
            {
                iter = _timerMap.GetEnumerator();
                while (iter.MoveNext())
                {
                    TimerEvent i = iter.Current.Value;

                    if (i.deleted)
                    {
                        _toRemove.Add(i);
                        continue;
                    }

                    i.elapsed += elapseSeconds;
                    if (i.elapsed < i.interval)
                        continue;

                    i.elapsed -= i.interval;
                    if (i.elapsed < 0 || i.elapsed > 0.03f)
                        i.elapsed = 0;

                    if (i.repeat > 0)
                    {
                        i.repeat--;
                        if (i.repeat == 0)
                        {
                            i.deleted = true;
                            _toRemove.Add(i);
                        }
                    }

                    i.callBack?.Invoke();
                }

                iter.Dispose();
            }


            int len = _toRemove.Count;
            if (len > 0)
            {

                for (int k = 0; k < len; k++)
                {
                    TimerEvent i = _toRemove[k];
                    _timerMap.Remove(i.id);
                    ReturnToPool(i);
                }
                _toRemove.Clear();
            }


            if (_toAdd.Count > 0)
            {
                iter = _toAdd.GetEnumerator();
                while (iter.MoveNext())
                {
                    _timerMap.Add(iter.Current.Key, iter.Current.Value);
                }
                    
                iter.Dispose();
                _toAdd.Clear();
            }

        }

        public static int Add(System.Action callback, float interval, int repeat)
        {
            TimerEvent info = GetFromPool();
            info.repeat = repeat;
            info.callBack = callback;
            info.interval = interval;
            info.deleted = false;

            _toAdd[info.id] = info;

            return info.id;
        }


        public static int SetTimeout(System.Action callback, float timeout)
        {
            return Add(callback, timeout, 1);
        }

        public static int SetInterval(System.Action callback, float interval)
        {
            return Add(callback, interval, 0);
        }

        public static TimerEvent GetFromPool()
        {
            TimerEvent timerInfo;
            int cnt = _timerCache.Count;
            if (cnt > 0)
            {
                timerInfo = _timerCache[cnt - 1];
                _timerCache.RemoveAt(cnt - 1);

                timerInfo.deleted = false;
                timerInfo.elapsed = 0;
            }
            else
            {
                timerInfo = new TimerEvent();
            }

            timerInfo.id = ++_idCounter;

            return timerInfo;
        }

        private static void ReturnToPool(TimerEvent t)
        {
            t.callBack = null;
            _timerCache.Add(t);
        }

        public static void ClearTimer(int id)
        {
            TimerEvent t;
            if (_toAdd.TryGetValue(id,out t))
            {
                _toAdd.Remove(id);
                ReturnToPool(t);
                return;
            }

            if (_timerMap.TryGetValue(id, out t))
            {
                t.deleted = true;
            }
                
        }

        public static void ClearAllTimer()
        {
            foreach (KeyValuePair<int, TimerEvent> kvp in _timerMap)
            {
                kvp.Value.callBack = null;

            }

            foreach (KeyValuePair<int, TimerEvent> kvp in _toAdd)
            {
                kvp.Value.callBack = null;

            }

            _timerMap.Clear();
            _timerCache.Clear();

            _toAdd.Clear();
            _toRemove.Clear();

            _idCounter = 0;
        }

    }
}
