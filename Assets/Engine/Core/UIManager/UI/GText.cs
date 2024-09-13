using UnityEngine.UI;

namespace GameFramework.UI
{
    public class GText:GObject
    {
        public string _text;

        public override string text
        {
            set
            {
                _text = value;
                SetText();
            }
            get { return _text; }
        }

        public  void SetText()
        {
            obj.GetComponent<Text>().text = _text;
        }
    }
}
