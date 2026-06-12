using GameEditor.ActionEditor;
using TrackEditor;
using UnityEngine;

namespace GameFramework.Action
{
    [Name("动画轨道")]
    [ShowIcon(typeof(UnityEngine.AnimationClip))]
    [Color(0.48f, 0.71f, 0.84f)]
    [Attachable(typeof(ActionGroup))]
    public class ActionAnimationTrack : Track { }
}
