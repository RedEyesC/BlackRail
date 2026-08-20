using TrackEditor;
using UnityEngine;

namespace GameFramework.Action
{
    [Color(0.48f, 0.71f, 0.84f)]
    [Attachable(typeof(ActionAnimationTrack))]
    public class ActionAnimationClip : Clip, ISubClipContainable
    {
        [SerializeField]
        [HideInInspector]
        private float length = 1f;

        [SerializeField]
        [HideInInspector]
        private float blendIn = 0.25f;

        [SerializeField]
        [HideInInspector]
        private float blendOut = 0.25f;

        public string resPath = "";

        [Range(0.1f, 10f)]
        public float playbackSpeed = 1;

        public float clipOffset;

        public float subClipLength;

        public override float Length
        {
            get => length;
            set => length = value;
        }

        public override float BlendIn
        {
            get => blendIn;
            set => blendIn = value;
        }

        public override float BlendOut
        {
            get => blendOut;
            set => blendOut = value;
        }

        public virtual float SubClipOffset
        {
            get => clipOffset;
            set => clipOffset = value;
        }

        public virtual float SubClipLength
        {
            get => subClipLength;
            set => subClipLength = Mathf.Max(0, value);
        }

        public virtual float SubClipSpeed
        {
            get => playbackSpeed;
            set => playbackSpeed = Mathf.Max(0.0001f, value);
        }

        public override bool isValid => true;
    }
}
