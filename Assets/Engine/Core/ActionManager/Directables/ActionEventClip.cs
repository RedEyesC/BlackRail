using System;
using TrackEditor;
using UnityEngine;

namespace GameFramework.Action
{
    [Name("事件片段")]
    [System.Serializable]
    [Color(0.95f, 0.62f, 0.25f)]
    [Attachable(typeof(ActionEventTrack))]
    public class ActionEventClip : Clip
    {
        [HideInInspector]
        public int EventType;

        [HideInInspector]
        public int EventParam0;

        [HideInInspector]
        public int EventParam1;

        [HideInInspector]
        public int EventParam2;

        [SerializeField]
        private int[] extraParameters;

        public ActionEventClip()
        {
            Type = ClipType.Signal;
        }

        public int GetParameter(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            switch (index)
            {
                case 0: return EventParam0;
                case 1: return EventParam1;
                case 2: return EventParam2;
                default:
                    var extraIndex = index - 3;
                    return extraParameters != null && extraIndex < extraParameters.Length
                        ? extraParameters[extraIndex]
                        : 0;
            }
        }

        public void SetParameter(int index, int value)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            switch (index)
            {
                case 0: EventParam0 = value; break;
                case 1: EventParam1 = value; break;
                case 2: EventParam2 = value; break;
                default:
                    var extraIndex = index - 3;
                    if (extraParameters == null || extraIndex >= extraParameters.Length)
                    {
                        Array.Resize(ref extraParameters, extraIndex + 1);
                    }

                    extraParameters[extraIndex] = value;
                    break;
            }
        }

        public override bool isValid => true;
    }
}
