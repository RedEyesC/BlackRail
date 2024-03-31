using System.Collections.Generic;

namespace GameFramework.Runtime
{
    public class RequestQueue
    {
        private readonly List<Request> _processing = new List<Request>();
        private readonly Queue<Request> _queue = new Queue<Request>();
        public string key;
        public int priority { get; set; } = 1;
        public byte maxRequests { get; set; } = 10;
        public bool working => _processing.Count > 0 || _queue.Count > 0;

        public void Enqueue(Request request)
        {
            if (_processing.Contains(request) || _queue.Contains(request))
                return;
            _queue.Enqueue(request);
        }

        public bool Update()
        {
            while (_queue.Count > 0 && (_processing.Count < maxRequests || maxRequests == 0))
            {
                var item = _queue.Dequeue();
                _processing.Add(item);
                if (item.status == Request.Status.Wait) item.Start();
                if (AssetManager.busy) return false;
            }

            for (var index = 0; index < _processing.Count; index++)
            {
                var item = _processing[index];
                if (item.Update()) continue;
                _processing.RemoveAt(index);
                index--;
                item.Complete();
                if (AssetManager.busy) return false;
            }

            return true;
        }
    }
}