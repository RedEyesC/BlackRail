using UnityEngine;

public static class TransformExtensions
{
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
}

