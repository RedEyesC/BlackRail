using TrackEditor;

namespace MotionMatching
{
    public class MotionEditorWindow : TrackEditorWindow
    {
        public static void OpenDirectorWindow()
        {
            var window = GetWindow(typeof(MotionEditorWindow)) as MotionEditorWindow;
            if (window == null)
                return;
            window.InitializeAll();
            window.Show();
        }
    }
}
