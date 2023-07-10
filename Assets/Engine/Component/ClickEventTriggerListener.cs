using UnityEngine;
using UnityEngine.EventSystems;

public class ClickEventTriggerListener : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IMoveHandler, IPointerClickHandler
{
	private static readonly float DoubleClickInterval = 0.2f;
	private static readonly float LongPressDelayTime = 0.3f;
	private static readonly float LongPressInvokeRepeatTime = 0.1f;

	private float lastPointerDownTime = 0;
	private float lastClickTime = 0;
	private int longPressCounter = 0;
	private PointerEventData mCacheEventData;

	public delegate void EventPosDelegate(float x, float y);
	public delegate void EventVoidDelegate();
	public delegate void EventBoolDelegate(bool val);
	public delegate void EventIntDelegate(int val);
    public delegate void EventFloatDelegate(float val);

    public bool touchEnable = true;
	public EventPosDelegate onClick = null;
	public EventVoidDelegate onDoubleClick = null;
	public EventPosDelegate onTouchDown = null;
	public EventPosDelegate onTouchUp = null;
	public EventPosDelegate onMove = null;

	public EventIntDelegate onLongPress = null;
	public EventVoidDelegate onLongPressEnd = null;

	static public ClickEventTriggerListener Get(Transform t)
	{
		ClickEventTriggerListener listener = t.gameObject.GetComponent<ClickEventTriggerListener>();
		if (listener == null) listener = t.gameObject.AddComponent<ClickEventTriggerListener>();
		return listener;
	}

	public void SetTouchEnable(bool val)
	{
		touchEnable = val;
	}

	public virtual void OnPointerClick(PointerEventData eventData)
	{
		if (!touchEnable)
			return;

		if (onLongPress != null && Time.time - lastPointerDownTime > LongPressDelayTime)
			return;

		if (onDoubleClick != null)
		{
			if (Time.time - lastClickTime < DoubleClickInterval)
			{
				onDoubleClick();
				CancelInvoke("InvokeClick");
				lastClickTime = 0;
			}
			else
			{
				lastClickTime = Time.time;

				if (onClick != null)
				{
					mCacheEventData = eventData;
					Invoke("InvokeClick", DoubleClickInterval);
				}
			}

		}
		else if (onClick != null)
		{
			onClick(eventData.position.x, eventData.position.y);
		}
	}


	public virtual void OnPointerDown(PointerEventData eventData)
	{
		if (touchEnable && onTouchDown != null) onTouchDown(eventData.position.x, eventData.position.y);

		if (onLongPress != null) InvokeRepeating("InvokeLongPress", LongPressDelayTime, LongPressInvokeRepeatTime);

		lastPointerDownTime = Time.time;
	}

	public virtual void OnPointerUp(PointerEventData eventData)
	{
		if (touchEnable && onTouchUp != null) onTouchUp(eventData.position.x, eventData.position.y);

		if (onLongPress != null) CancelInvoke("InvokeLongPress");

		LongPressEnd();
	}

	public virtual void OnMove(AxisEventData eventData)
	{
		if (touchEnable && onMove != null) onMove(eventData.moveVector.x, eventData.moveVector.y);
	}

	protected virtual void InvokeClick()
	{
		onClick(mCacheEventData.position.x, mCacheEventData.position.y);
	}

	protected virtual void InvokeLongPress()
	{
		onLongPress(++longPressCounter);
	}

	protected virtual void LongPressEnd()
	{
		if (onLongPressEnd != null && longPressCounter > 0)
		{
			onLongPressEnd();
		}

		if (longPressCounter > 0)
			longPressCounter = 0;
	}
}