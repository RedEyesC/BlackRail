namespace GameFramework.Common
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


        public abstract void Update(float nowTime, float elapseSeconds);


        public abstract void Destroy();
    }
}
