using System;
using TrackEditor;
using UnityEngine;

namespace MotionMatching
{
    [Serializable]
    [Attachable(typeof(MotionAsset))]
    public class MotionGroup : Group
    {
        public AnimationClip animationClip;
    }
}
