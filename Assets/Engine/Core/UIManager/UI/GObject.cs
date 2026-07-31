using UnityEngine;

namespace GameFramework.UI
{
    public class GObject
    {
        public GameObject obj;

        public GComponent parent;

        public virtual string text
        {
            get { return null; }
            set
            { /*override in child*/
            }
        }

        public string name
        {
            get { return obj.name; }
        }

        public virtual void ConstructUI() { }

        internal void SetParent(GComponent value)
        {
            parent = value;
        }

        public void SetActive(bool var)
        {
            obj.SetActive(var);
        }

        public void SetLayer(int layer)
        {
            obj.layer = layer;
        }

        public void SetParent(GameObject parentObj, bool posStay)
        {
            obj.SetParent(parentObj, posStay);
        }

        public void RemoveFromParent()
        {
            if (parent != null)
                parent.RemoveChild(this);
        }

        public virtual void Destroy()
        {
            if (obj != null)
            {
                UnityEngine.GameObject.Destroy(obj);
            }
        }

        public virtual void Dispose()
        {
            RemoveFromParent();
            Destroy();
        }
    }
}
