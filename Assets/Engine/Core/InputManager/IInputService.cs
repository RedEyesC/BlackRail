namespace GameFramework.Input
{
    public interface IInputService
    {
        void Startup();
        void Shutdown();
        void OnBeforeUpdate();
        void OnAfterUpdate();
    }
}
