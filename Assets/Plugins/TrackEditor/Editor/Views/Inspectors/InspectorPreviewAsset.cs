using UnityEngine;

namespace TrackEditor
{
    public class InspectorPreviewAsset : ScriptableObject
    {
        internal event System.Action RepaintRequested;

        private DirectableAsset selectedObject;
        [SerializeField] internal string Snapshot;

        public DirectableAsset SelectedObject
        {
            get => selectedObject;
            set
            {
                if (ReferenceEquals(selectedObject, value))
                {
                    return;
                }

                selectedObject = value;
                RequestRepaint();
            }
        }

        internal void RequestRepaint() => RepaintRequested?.Invoke();
    }
}
