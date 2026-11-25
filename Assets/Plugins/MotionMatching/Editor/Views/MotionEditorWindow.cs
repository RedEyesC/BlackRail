using TrackEditor;

namespace MotionMatching
{
    public class MotionEditorWindow : TrackEditorWindow
    {
        protected override System.Type AssetType => typeof(MotionAsset);

        protected override string CreateWindowTitle()
        {
            return "Motion Editor";
        }
    }
}
