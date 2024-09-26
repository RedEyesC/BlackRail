using UnityEngine.UI;

namespace GameFramework.UI
{
    public class GButton : GComponent
    {

        public GObject _titleObject;

        public string _title;

        public override string text
        {
            set
            {
                _title = value;
                SetTitle(value);
            }
            get { return _title; }
        }

        override public void ConstructUI()
        {
            base.ConstructUI();

            _titleObject = GetChild("txt");

        }

        public void AddClickCallback(ClickComponent.EventPosDelegate action)
        {
            if (obj.GetComponent<Graphic>() == null)
            {
                obj.gameObject.AddComponent<NoDrawingRayCastComponet>();
            }

            ClickComponent ev = ClickComponent.Get(obj.transform);
            if (ev != null)
            {
                ev.onClick = action;
            }
        }

        private void SetTitle(string txt)
        {
            if (this._titleObject != null)
            {
                this._titleObject.text = txt;
            }
        }

        override public void Destroy()
        {

        }
    }
}
