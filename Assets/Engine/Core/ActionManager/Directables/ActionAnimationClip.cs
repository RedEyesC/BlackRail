using TrackEditor;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
using System.Linq;
#endif

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

        [MenuName("动画对象")]
        public string resPath = "";

        [Range(0.1f, 10f)]
        public float playbackSpeed = 1;

        public float clipOffset;

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

        float ISubClipContainable.SubClipOffset
        {
            get => clipOffset;
            set => clipOffset = value;
        }

        float ISubClipContainable.SubClipLength => 0;

        float ISubClipContainable.SubClipSpeed => 1;

        public override bool isValid => true;
    }
}
