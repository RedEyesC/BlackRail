
namespace GameFramework.Runtime
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


        public abstract void Update(float elapseSeconds, float realElapseSeconds);


        public abstract void Destroy();
    }
}
