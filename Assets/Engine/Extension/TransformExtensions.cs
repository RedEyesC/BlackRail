using UnityEngine;

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

    public static void RemoveClickEventListener(this Transform t)
    {
        ClickComponent listener = t.gameObject.GetComponent<ClickComponent>();
        if (listener != null && listener.onClick != null)
        {
            listener.onClick = null;
        }
    }

    #endregion
}

