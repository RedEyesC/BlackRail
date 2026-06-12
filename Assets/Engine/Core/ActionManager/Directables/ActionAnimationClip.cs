using TrackEditor;
using UnityEditor;
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

        [MenuName("动画对象")]
        public string resPath = "";

        private UnityEngine.AnimationClip _animationClip;

        public UnityEngine.AnimationClip animationClip
        {
            get
            {
                if (string.IsNullOrEmpty(resPath))
                {
                    _animationClip = null;
                    return null;
                }

                if (_animationClip == null)
                {
#if UNITY_EDITOR
                    _animationClip = AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>(resPath);
#endif
                }

                return _animationClip;
            }
        }

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

        float ISubClipContainable.SubClipLength => animationClip != null ? animationClip.length : 0;

        float ISubClipContainable.SubClipSpeed => 1;

        public override bool isValid => animationClip != null;

        public override string info => isValid ? animationClip.name : base.info;
    }
}
