using UnityEngine;
using UnityEngine.UI;

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

    #region Touch Relative
    public static void AddClickEventListener(this Transform t, ClickEventTriggerListener.EventPosDelegate action)
    {
        if (t.GetComponent<Graphic>() == null)
            t.gameObject.AddComponent<NoDrawingRayCast>();

        ClickEventTriggerListener ev = ClickEventTriggerListener.Get(t);
        if (ev != null)
        {
            ev.onClick = action;
        }
    }

    public static void RemoveClickEventListener(this Transform t)
    {
        ClickEventTriggerListener listener = t.gameObject.GetComponent<ClickEventTriggerListener>();
        if (listener != null && listener.onClick != null)
        {
            listener.onClick = null;
        }
    }

    #endregion
}

