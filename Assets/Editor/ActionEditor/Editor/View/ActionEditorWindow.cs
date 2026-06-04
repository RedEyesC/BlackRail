using TrackEditor;

namespace ActionEditor
{
    public class ActionEditorWindow : TrackEditorWindow
    {
        protected override IAssetStorage CreateAssetStorage()
        {
            return new ActionAssetStorage();
        }

        protected override string CreateWindowTitle()
        {
            return "Action Editor";
        }
    }
}
