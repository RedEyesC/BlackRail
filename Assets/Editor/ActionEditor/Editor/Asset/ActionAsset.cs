using System;
using TrackEditor;
using UnityEngine;

namespace ActionEditor
{
    [Serializable]
    public class ActionAsset : Asset { }

    [Serializable]
    [Attachable(typeof(ActionAsset))]
    public class ActionJsonGroup : Group { }

    [Serializable]
    [Attachable(typeof(ActionJsonGroup))]
    [ShowIcon(typeof(AnimationClip))]
    public class ActionJsonTrack : Track { }

    [Serializable]
    [Attachable(typeof(ActionJsonTrack))]
    public class ActionJsonClip : ActionClip
    {
        [SerializeField]
        private float length = 1f;

        public override float Length
        {
            get => length;
            set => length = Mathf.Max(value, 0);
        }

        public override bool isValid => true;
    }
}
