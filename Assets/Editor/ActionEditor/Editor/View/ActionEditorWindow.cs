using TrackEditor;

namespace ActionEditor
{
    public class ActionEditorWindow : TrackEditorWindow
    {
        public static void OpenDirectorWindow()
        {
            var window = GetWindow(typeof(ActionEditorWindow)) as ActionEditorWindow;
            if (window == null)
                return;
            window.InitializeAll();
            window.Show();
        }
    }
}
