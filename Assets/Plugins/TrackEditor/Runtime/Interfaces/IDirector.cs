namespace TrackEditor
{
    public interface IDirector : IData
    {
        float Length { get; }
        void Validate();
    }
}
