namespace TrackEditor.Design.Editor.Inspectors
{
    public abstract class GroupInspector<T> : ClipInspector where T : Group
    {
        protected T action => (T)target;
    }

    [CustomInspectors(typeof(Group), true)]
    public class GroupInspector : InspectorsBase
    {
        public override void OnInspectorGUI()
        {
            ShowCommonInspector();
        }

        protected void ShowCommonInspector(bool showBaseInspector = true)
        {
            if (showBaseInspector)
            {
                base.OnInspectorGUI();
            }
        }
    }
}
