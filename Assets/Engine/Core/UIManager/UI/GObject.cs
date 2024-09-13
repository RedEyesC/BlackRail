using UnityEngine;

namespace GameFramework.UI
{
    public class GObject
    {
        public GameObject obj;

        virtual public string text
        {
            get { return null; }
            set { /*override in child*/}
        }

        public string name
        {
            get { return obj.name; }
        }


        virtual public void ConstructUI()
        {

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

        virtual public void Destroy()
        {
            UnityEngine.GameObject.Destroy(obj);
        }

    }
}
