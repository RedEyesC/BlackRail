using System.Collections.Generic;

namespace GameFramework.Asset
{
    public class ActionRequest : Request
    {
        private static readonly Queue<ActionRequest> Unused = new Queue<ActionRequest>();
        public System.Action action;

        protected override void OnStart()
        {
            action?.Invoke();
            SetResult(Result.Success);
        }

        protected override void OnCompleted()
        {
            Recycle(this);
        }

        public static ActionRequest CallAsync(System.Action action)
        {
            var request = Create();
            request.Reset();
            request.action = action;
            request.SendRequest();
            return request;
        }

        public static ActionRequest Create()
        {
            return Unused.Count > 0 ? Unused.Dequeue() : new ActionRequest();
        }

        public static void Recycle(ActionRequest request)
        {
            if (Unused.Contains(request))
                return;
            Unused.Enqueue(request);
        }
    }
}
