
using GameFramework.Common;
using System.Collections.Generic;

namespace GameFramework.Event
{

    public delegate void EventFunc(params object[] args);

    public class Event
    {
        public object[] args;
        public int eventID;

        public void Rest()
        {
            eventID = 0;
            args = null;
        }
    }

    public class EventManager : GameModule
    {

        public new int priority = 10;


        private static CollectPool<Event> _eventPool;
        private static readonly Queue<Event> _events = new Queue<Event>();

        private static readonly Dictionary<int, EventFunc> _funcMap = new Dictionary<int, EventFunc>();
        private static readonly Dictionary<int, LinkedList<int>> _eventHandlers = new Dictionary<int, LinkedList<int>>();
        private static readonly Dictionary<int, LinkedListNode<int>> _cachedNodes = new Dictionary<int, LinkedListNode<int>>();


        public override void Start()
        {
            _eventPool = new CollectPool<Event>(
            "eventPool",
            () =>
            {
                return new Event();
            },
            (Event obj) =>
            {

            },
            (Event obj) =>
            {
                obj.Rest();
            }, 100);
        }


        public override void Destroy()
        {
            _eventPool.Clear();
            _events.Clear();

            _eventHandlers.Clear();
            _funcMap.Clear();
            _cachedNodes.Clear();
        }


        public override void Update(float nowTime, float elapseSeconds)
        {
            while (_events.Count > 0)
            {
                Event eventNode = _events.Dequeue();
                HandleEvent(eventNode.eventID, eventNode.args);
                _eventPool.Free(eventNode);
            }

        }

        public static void ClearEvent()
        {
            _eventPool.Clear();
            _events.Clear();
        }


        public static void Fire(int evId, params object[] args)
        {
            Event ev = _eventPool.Create();
            ev.eventID = evId;
            ev.args = args;
            _events.Enqueue(ev);
        }


        public static void FireNow(int evId, params object[] args)
        {
            HandleEvent(evId, args);
        }


        public static int BindEvent(int evId, EventFunc handler)
        {
            int handlerId;
            LinkedList<int> list;
            if (_eventHandlers.TryGetValue(evId, out list))
            {

                handlerId = evId * 100 + list.Count;
                list.AddLast(handlerId);
            }
            else
            {
                handlerId = evId * 100;
                list = new LinkedList<int>();
                _eventHandlers.Add(evId, list);
            }

            _funcMap[handlerId] = handler;

            return handlerId;
        }


        public static void UnBindEvent(int handlerId)
        {
            int evID = handlerId % 100;

            if (_cachedNodes.Count > 0)
            {
                LinkedListNode<int> nod;
                if (_cachedNodes.TryGetValue(evID, out nod))
                {
                    if (nod.Value == handlerId)
                    {
                        _cachedNodes[evID] = nod.Next;
                    }
                }
            }


            LinkedList<int> range;
            if (_eventHandlers.TryGetValue(evID, out range))
            {
                for (LinkedListNode<int> current = range.First; current != null && current != range.Last; current = current.Next)
                {
                    if (current.Value.Equals(handlerId))
                    {
                        range.Remove(current);
                        _funcMap.Remove(handlerId);
                    }
                }

                if (range.Count == 0)
                {
                    _eventHandlers.Remove(evID);
                }
            }
        }

        private static void HandleEvent(int evId, params object[] args)
        {

            LinkedList<int> range;

            if (_eventHandlers.TryGetValue(evId, out range))
            {
                LinkedListNode<int> current = range.First;
                while (current != null)
                {
                    _cachedNodes[evId] = current.Next;

                    EventFunc func = _funcMap[current.Value];
                    func(args);

                    current = _cachedNodes[evId];
                }

                _cachedNodes.Remove(evId);

            }

        }
    }
}
