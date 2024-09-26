using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.UI
{
    public class GComponent : GObject
    {

        internal List<GObject> _children = new List<GObject>();

        public GComponent()
        {

        }

        override public void ConstructUI()
        {
            GObject child;

            int childCount = obj.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                GameObject childObj = obj.transform.GetChild(i).gameObject;
                child = UIObjectFactory.NewObject(childObj);
                child.obj = childObj;
                child.ConstructUI();
                _children.Add(child);
            }

        }

        public GObject GetChild(string name)
        {
            int cnt = _children.Count;
            for (int i = 0; i < cnt; ++i)
            {
                if (_children[i].name == name)
                    return _children[i];
            }
            return null;
        }

        override  public void Destroy()
        {
            if (_children.Count > 0)
            {
                int cnt = _children.Count;
                for (int i = cnt - 1; i >= 0; --i)
                {
                    var obj = _children[i];
                    obj.Destroy();
                }
            }

            _children = null;

            base.Destroy();
        }
    }
}
