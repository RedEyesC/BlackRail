using UnityEngine;

namespace GameLogic
{
    internal class Monster : Obj
    {
        private int _monsterId;
        private float nextTime = 0;

        public float cd = 0.1f;
        public int attackRange = 9;

        public Vector2 _tempVector = new Vector2();

        public int monsterId
        {
            get { return _monsterId; }
        }

        public Monster()
            : base(BodyType.Monster) { }

        public void InitModel(int monsterId) { }
    }
}
