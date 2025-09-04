namespace GameFramework
{

    public abstract class GameModule
    {
        public virtual int priority
        {
            get
            {
                return 0;
            }
        }

        public abstract void Start();

        public abstract void Destroy();

        public virtual void EarlyUpdate() { }

        public virtual void Update(float nowTime, float elapseSeconds) { }
        
        public virtual void PostLateUpdate() { }




    }
}
