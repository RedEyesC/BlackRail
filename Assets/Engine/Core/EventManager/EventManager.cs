
using GameFramework.Common;

namespace GameFramework.Event
{

    public class EventManager : GameModule
    {

        private static EventPool _eventPool;
        public new int priority = 10;
        public override void Start()
        {
            _eventPool = new EventPool();
        }


        public override void Destroy()
        {
            _eventPool.ClearAll();
        }


        public override void Update(float elapseSeconds, float realElapseSeconds)
        {
            _eventPool.Update(elapseSeconds, realElapseSeconds);
        
        }

        public static void ClearEvent()
        {
            _eventPool.ClearEvent();
        }


        public static void Fire(EventType type, params object[] args)
        {
            _eventPool.Fire((int)type, args);
        }


        public static void FireNow(EventType type, params object[] args)
        {
            _eventPool.FireNow((int)type, args);
        }


        public static int BindEvent(EventType type, EventFunc handler)
        {
            return _eventPool.BindEvent((int)type, handler);
        }


        public static void UnBindEvent(EventType type)
        {
            _eventPool.UnBindEvent((int)type);
        }
    }
}
