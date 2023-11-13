using UnityEngine;
using UnityEngine.UI;

public static class TransformExtensions
{
    static Vector3 tempVec3 = new Vector3();
    public static bool AddChild(this Transform t, Transform obj)
    {
        obj.SetParent(t, false);
        return true;
    }

    public static bool AddChild(this Transform t, Transform obj, string name)
    {
        Transform child = SearchChild(t, name);
        if (child)
        {
            obj.SetParent(child, false);
            return true;
        }
        return false;
    }

    public static Transform SearchChild(this Transform trans, string goName)
    {
        Transform child = trans.Find(goName);
        if (child != null)
            return child;

        Transform go = null;
        for (int i = 0; i < trans.childCount; i++)
        {
            child = trans.GetChild(i);
            go = SearchChild(child, goName);
            if (go != null)
                return go;
        }
        return null;
    }

    public static void SetLookDir(this Transform t, float x, float y, float z)
    {
        if (x < Mathf.Epsilon && x > -Mathf.Epsilon
            && y < Mathf.Epsilon && y > -Mathf.Epsilon
            && z < Mathf.Epsilon && z > -Mathf.Epsilon)
            return;

        tempVec3.Set(x, y, z);
        t.localRotation = Quaternion.LookRotation(tempVec3);
    }



    #region Touch Relative
    public static void AddClickEventListener(this Transform t, ClickComponent.EventPosDelegate action)
    {
        if (t.GetComponent<Graphic>() == null)
            t.gameObject.AddComponent<NoDrawingRayCast>();

        ClickComponent ev = ClickComponent.Get(t);
        if (ev != null)
        {
            ev.onClick = action;
        }
    }

    public static void RemoveClickEventListener(this Transform t)
    {
        ClickComponent listener = t.gameObject.GetComponent<ClickComponent>();
        if (listener != null && listener.onClick != null)
        {
            listener.onClick = null;
        }
    }

    #endregion


    #region Raycast
    public static float GetHeightByRaycast(this Transform t, float x, float z, int layerMask)
    {
        tempVec3.Set(x, 1000, z);
        RaycastHit hit;
        Ray ray = new Ray(tempVec3, Vector3.down);
        if (Physics.Raycast(ray, out hit, 1500f, layerMask))
        {
            return hit.point.y;
        }
        return -9999f;
    }
    #endregion
}

