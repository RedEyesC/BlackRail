using System.Collections.Generic;

namespace GameFramework.Event
{
    public delegate void EventFunc(params object[] args);

    public class Event
    {
        public object[] args;
        public int eventID;
    }

 
    internal class EventPool
    {
        private readonly List<Event> _eventCache = new List<Event>(100);
        private readonly Queue<Event> _events = new Queue<Event>();

        private readonly Dictionary<int, EventFunc> _funcMap = new Dictionary<int, EventFunc>();
        private readonly Dictionary<int, LinkedList<int>> _eventHandlers = new Dictionary<int, LinkedList<int>>();

        private readonly Dictionary<int, LinkedListNode<int>> _cachedNodes;

        public void Update(float elapseSeconds, float realElapseSeconds)
        {

            while (_events.Count > 0)
            {
                Event eventNode = _events.Dequeue();
                HandleEvent(eventNode.eventID, eventNode.args);
                ReturnToPool(eventNode);
            }

        }

        public void ClearAll()
        {
            ClearEvent();
            _eventHandlers.Clear();
            _funcMap.Clear();
            _cachedNodes.Clear();

        }

        public void ClearEvent()
        {
            _eventCache.Clear();
            _events.Clear();
        }

        public int BindEvent(int evId, EventFunc handler)
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


        public void UnBindEvent(int handlerId)
        {
            int evID = handlerId % 100;

            if(_cachedNodes.Count > 0)
            {
                LinkedListNode<int> nod;
                if (_cachedNodes.TryGetValue(evID,out nod))
                {
                    if(nod.Value == handlerId)
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

                if(range.Count == 0)
                {
                    _eventHandlers.Remove(evID);
                }
            }
        }

        public void Fire(int type, params object[] args)
        {
            Event ev = GetFromPool();
            ev.eventID = type;
            ev.args = args;
            _events.Enqueue(ev);
        }


        public void FireNow(int type, params object[] args)
        {
            HandleEvent(type, args);
        }


        private void HandleEvent(int type, params object[] args)
        {

            LinkedList<int> range;

            if (_eventHandlers.TryGetValue(type, out range))
            {
                LinkedListNode<int> current = range.First;
                while (current != null)
                {
                    _cachedNodes[type] = current.Next;
                    
                    EventFunc func = _funcMap[current.Value];
                    func(args);

                    current = _cachedNodes[type];
                }

                _cachedNodes.Remove(type);

            }

        }


        private Event GetFromPool()
        {
            Event ev;
            int cnt = _eventCache.Count;
            if (cnt > 0)
            {
                ev = _eventCache[cnt - 1];
                _eventCache.RemoveAt(cnt - 1);

            }
            else
            {
                ev = new Event();
            }

            return ev;
        }

        private void ReturnToPool(Event t)
        {
            t.eventID = 0;
            t.args = null;
            _eventCache.Add(t);
        }
    }
}
