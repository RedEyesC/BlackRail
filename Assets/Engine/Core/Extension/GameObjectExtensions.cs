using UnityEngine;
public static class GameObjectExtensions
{
    public static void SetParent(this GameObject go, Transform parent, bool posStay)
    {
        go.transform.SetParent(parent, posStay);
    }

    public static void SetParent(this GameObject go, GameObject parentObj, bool posStay)
    {
        go.transform.SetParent(parentObj.transform, posStay);
    }

}