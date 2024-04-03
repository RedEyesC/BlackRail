using UnityEngine;
using UnityEngine.UI;

namespace GameFramework.Runtime
{
    public class GButton : GComponent
    {
        public void AddClickEventListener(ClickComponent.EventPosDelegate action)
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
