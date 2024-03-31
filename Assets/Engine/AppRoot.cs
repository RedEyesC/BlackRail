
using UnityEngine;

namespace GameFramework.Runtime
{
    public class AppRoot : MonoBehaviour
    {

        void Awake()
        {
            //GameObject.DontDestroyOnLoad(this);
        }
        // Start is called before the first frame update
        void Start()
        {

            InitInterface();

            GlobalCenter.CreateInstance();

            GlobalCenter.Start();
        }

        // Update is called once per frame
        void Update()
        {
            GlobalCenter.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }

        void OnDestroy()
        {
            GlobalCenter.Destroy();
        }

        void InitInterface()
        {
            AppInterface.StartCoroutine = StartCoroutine;
            AppInterface.StopCoroutine = StopCoroutine;
            AppInterface.AddComponent = gameObject.AddComponent;
        }
    }
}

