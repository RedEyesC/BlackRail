using UnityEngine;

namespace TrackEditor
{
    public class InspectorPreviewAsset : ScriptableObject
    {
        public DirectableAsset SelectedObject { get; set; }
        [SerializeField] internal string Snapshot;
    }
}
