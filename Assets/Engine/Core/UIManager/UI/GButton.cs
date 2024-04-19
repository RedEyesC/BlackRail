using UnityEngine.UI;

namespace GameFramework.UI
{
    public class GButton : GComponent
    {
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
    }
}
