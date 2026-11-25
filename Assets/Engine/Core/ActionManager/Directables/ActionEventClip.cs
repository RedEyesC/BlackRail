using TrackEditor;
using UnityEngine;

namespace GameFramework.Action
{
    [System.Serializable]
    [Color(0.95f, 0.62f, 0.25f)]
    [Attachable(typeof(ActionEventTrack))]
    public class ActionEventClip : ClipSignal
    {
        [SerializeField]
        public int EventType;

        [SerializeField]
        public int EventParam0;

        [SerializeField]
        public int EventParam1;

        [SerializeField]
        public int EventParam2;

        public override bool isValid => true;
    }
}
