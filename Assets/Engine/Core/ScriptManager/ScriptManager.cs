


namespace GameFramework.Runtime
{
    public class ScriptManager : GameModule
    {
     
        public override void Destroy()
        {
            
        }

        public override void Start()
        {
            GameCenter.CreateInstance();
        }

        public override void Update(float elapseSeconds, float realElapseSeconds)
        {
            GameCenter.Update(elapseSeconds, realElapseSeconds);
            
        }

        public void CallGameStart()
        {
            GameCenter.Start();
        }
    }

}
