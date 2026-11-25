namespace TrackEditor
{
    public abstract class TrackInspector<T> : TrackInspector where T : Track
    {
        protected T action => (T)target;
    }

    [CustomInspectors(typeof(Track), true)]
    public class TrackInspector : InspectorsBase
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
