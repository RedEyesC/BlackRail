using System;

namespace GameLogic
{
    [Serializable]
    public sealed class MovementAnimationNames
    {
        public string locomotion = "Locomotion";
        public string walkStart = "WalkStart";
        public string runStart = "RunStart";
        public string walk = "Walk";
        public string run = "Run";
        public string walkEnd = "WalkEnd";
        public string runEnd = "RunEnd";
        public string turnBack = "TurnBack";
        public string idle = "Idle";
    }
}
