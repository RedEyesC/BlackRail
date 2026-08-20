using GameEditor.ActionEditor;
using TrackEditor;
using UnityEngine;

namespace GameFramework.Action
{
    [Name("事件轨道")]
    [ShowIcon(typeof(UnityEngine.EventSystems.EventSystem))]
    [Color(0.95f, 0.62f, 0.25f)]
    [Attachable(typeof(ActionGroup))]
    public class ActionEventTrack : Track { }
}
