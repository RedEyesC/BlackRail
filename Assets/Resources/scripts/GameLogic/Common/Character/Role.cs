using System.Collections.Generic;

namespace GameFramework.Runtime
{

    internal class Role : Obj
    {

        private Dictionary<int, ModelObj> _ModelList = new Dictionary<int, ModelObj>();

        public Role()
        {
            Init(); 
        }

        public void SetModelID(int modelType, int id)
        {

            if (! _ModelList.ContainsKey(modelType)){
                _ModelList.Add(modelType, new ModelObj());
            }


            ModelObj model = _ModelList[modelType];

            string path = Utils.GetRoleModelPath(id);

            model.ChangeModel(path, () =>
            {
                model.SetParent(_RootObj.transform);
            });

        }


        public void PlayAnim()
        {


        }
    }
}
