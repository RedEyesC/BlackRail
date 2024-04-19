using UnityEngine;
using UnityEngine.UI;

namespace GameFramework.UI
{
    internal class UIObjectFactory
    {
        //想要更高效一点，可以考虑在生成ui的时候就挂载脚本记录组件
        public static GObject NewObject(GameObject go)
        {
            int childCount = go.transform.childCount;
            
            if (go.GetComponent<Button>() != null)
            {
                return new GButton();
            }
            else if (go.GetComponent<ScrollRect>() != null)
            {
                return new GList();
            }
            else if (go.GetComponent<Text>() != null && childCount == 0)
            {
                return new GText();
            }
            else if (go.GetComponent<Image>() != null && childCount == 0)
            {
                return new GImage();
            }
            else
            {
                return new GComponent();
            }

        }
    }
}
