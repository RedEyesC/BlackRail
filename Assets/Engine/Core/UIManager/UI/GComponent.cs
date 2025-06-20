using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.UI
{
    public class GComponent : GObject
    {

        internal List<GObject> _children = new List<GObject>();

        public int numChildren
        {
            get { return _children.Count; }
        }

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
                child.SetParent(this);
                _children.Add(child);
            }

        }

        public GObject AddChild(GObject child)
        {
            AddChildAt(child, _children.Count);
            return child;
        }

        virtual public GObject AddChildAt(GObject child, int index)
        {
            int numChildren = _children.Count;

            if (index >= 0 && index <= numChildren)
            {
                if (child.parent == this)
                {
                    int oldIndex = _children.IndexOf(child);

                    if (oldIndex != index)
                    {
                        _children.RemoveAt(oldIndex);
                        _children.Insert(index, child);
                    }
                }
                else
                {
                    child.RemoveFromParent();
                    child.SetParent(this);

                    _children.Insert(numChildren, child);

                }
                return child;
            }
            else
            {
                throw new Exception("Invalid child index: " + index + ">" + numChildren);
            }
        }

        public GObject RemoveChild(GObject child)
        {
            return RemoveChild(child, false);
        }


        public GObject RemoveChild(GObject child, bool dispose)
        {
            int childIndex = _children.IndexOf(child);
            if (childIndex != -1)
            {
                RemoveChildAt(childIndex, dispose);
            }
            return child;
        }

        virtual public GObject RemoveChildAt(int index, bool dispose)
        {
            if (index >= 0 && index < numChildren)
            {
                GObject child = _children[index];

                child.SetParent(null);


                _children.RemoveAt(index);


                if (dispose)
                {
                    child.Dispose();
                }


                return child;
            }
            else
            {
                throw new Exception("Invalid child index: " + index + ">" + numChildren);
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

        override public void Destroy()
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
