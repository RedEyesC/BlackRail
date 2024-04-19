using UnityEngine;

namespace GameFramework.UI
{
    public class GObject
    {
        public GameObject obj;

        public void SetActive(bool var)
        {
            obj.SetActive(var);
        }


        virtual public void ConstructUI()
        {

        }

        public void SetLayer(int layer)
        {
            obj.layer = layer;
        }

        public string name
        {
            get { return obj.name; }

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
