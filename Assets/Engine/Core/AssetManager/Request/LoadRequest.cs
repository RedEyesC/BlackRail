namespace GameFramework.Asset
{
    public abstract class LoadRequest : Request, IRecyclable
    {
        public int refCount { get; private set; }
        public string path { get; protected set; }

        protected override void OnCompleted()
        {
            waitForCompletion = false;
        }

        public void Retain()
        {
            refCount++;
        }


        public void Release()
        {
            if (refCount == 0)
            {
                return;
            }

            refCount--;
            if (refCount > 0) return;

            AssetManager.RecycleAsync(this);
        }

        protected abstract void OnDispose();

        public void WaitForCompletion()
        {
            if (isDone) return;

            if (status == Status.Wait) Start();

            OnWaitForCompletion();
        }

        protected virtual void OnWaitForCompletion()
        {
        }

        bool waitForCompletion;

        public void LoadAsync()
        {

            if (isDone && !waitForCompletion)
            {
                waitForCompletion = true;
                ActionRequest.CallAsync(Complete);
            }
            else
            {
                SendRequest();
            }

            Retain();
        }

        #region IRecyclable

        public void EndRecycle()
        {
            //Logger.D($"Unload {GetType().Name} {path}.");
            OnDispose();
        }

        public virtual bool CanRecycle()
        {
            return isDone;
        }

        public bool IsUnused()
        {
            return refCount == 0;
        }

        public virtual void RecycleAsync()
        {
        }

        public virtual bool Recycling()
        {
            return false;
        }

        #endregion
    }
}