namespace GameFramework.Runtime
{
    public class AppLoopStart : StateBase
    {
        public override string GetID()
        {
            return "start";
        }

        public override void StateUpdate(float elapseSeconds, float realElapseSeconds)
        {
            GameCenter.Update(elapseSeconds, realElapseSeconds);

        }

        public override void StateEnter(params object[] paramList)
        {
            GameCenter.CreateInstance();
            GameCenter.Start();
        }
    }
}
