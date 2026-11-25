using System;
using TrackEditor;
using UnityEngine;

namespace MotionMatching
{
    [Serializable]
    [Attachable(typeof(MotionAsset))]
    public class MotionGroup : Group
    {
        [TimelineAssetReference(typeof(AnimationClip))]
        public TimelineAssetReference animationClip;
    }
}
