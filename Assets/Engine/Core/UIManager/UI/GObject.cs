using UnityEngine;

namespace GameFramework.Runtime
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

    }
}
