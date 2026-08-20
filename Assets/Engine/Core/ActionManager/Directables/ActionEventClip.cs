using TrackEditor;
using UnityEngine;

namespace GameFramework.Action
{
    [Color(0.95f, 0.62f, 0.25f)]
    [Attachable(typeof(ActionEventTrack))]
    public class ActionEventClip : Clip
    {
        [SerializeField]
        [HideInInspector]
        private float length = 1f;

        [SerializeField]
        public int EventType;

        [SerializeField]
        public int EventParam0;

        [SerializeField]
        public int EventParam1;

        [SerializeField]
        public int EventParam2;

        public override float Length
        {
            get => length;
            set => length = value;
        }

        public override bool isValid => true;
    }
}
