using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace MyGame {
	public class DragSensitive : MonoBehaviour {
		public EventTrigger triggerArea;
		public bool clamp = true;
		public UnityEvent_Vector2 OnPointerDown;
		public UnityEvent_Vector2 OnDrag;
		public UnityEvent_Vector2 OnLocalPositionChange;
		public UnityEvent_Vector2 OnScaledPositionChange;
		public UnityEvent_Vector2 OnPointerUp;
		private Vector2 valueDrag, valueLocalPosition, valueScaledPosition;
		private bool valueUpdating;
		private bool gaveFinalValue;
		private bool dragJustStarted;
		private RectTransform rectTransform;

		[System.Serializable] public class UnityEvent_Vector2 : UnityEvent<Vector2> { }

		private void OnValidate() {
			RefreshDragEventType();
		}

		void Start() {
			rectTransform = triggerArea.GetComponent<RectTransform>();
			RefreshDragEventType();
		}

		public void RefreshDragEventType() {
			if (triggerArea == null) {
				triggerArea = GetComponent<EventTrigger>();
				if (triggerArea == null) {
					triggerArea = gameObject.AddComponent<EventTrigger>();
				}
			}
			Populate(triggerArea);
		}

		private void ProcessValue(BaseEventData baseEventData) {
			PointerEventData pointerEvent = baseEventData as PointerEventData;
			valueDrag = pointerEvent.delta;// new Vector2(-drag.delta.y, drag.delta.x);

			valueLocalPosition = pointerEvent.position - (Vector2)rectTransform.position;
			if (clamp) {
				float minx = -rectTransform.pivot.x * rectTransform.sizeDelta.x;
				float maxx = (1-rectTransform.pivot.x) * rectTransform.sizeDelta.x;
				float miny = -rectTransform.pivot.y * rectTransform.sizeDelta.y;
				float maxy = (1 - rectTransform.pivot.y) * rectTransform.sizeDelta.y;
				valueLocalPosition.x = Mathf.Clamp(valueLocalPosition.x, minx, maxx);
				valueLocalPosition.y = Mathf.Clamp(valueLocalPosition.y, miny, maxy);
			}

			valueScaledPosition = new Vector2(
				valueLocalPosition.x / rectTransform.sizeDelta.x,
				valueLocalPosition.y / rectTransform.sizeDelta.y);
			if (clamp) {
				valueScaledPosition.x = Mathf.Clamp(valueScaledPosition.x, -1, 1);
				valueScaledPosition.y = Mathf.Clamp(valueScaledPosition.y, -1, 1);
			}
			valueUpdating = true;
		}

		private void PointerUp(BaseEventData baseEventData) {
			PointerEventData pointerEvent = baseEventData as PointerEventData;
			valueDrag = Vector2.zero;
			valueLocalPosition = Vector2.zero;
			valueScaledPosition = Vector2.zero;
			valueUpdating = gaveFinalValue = false;
			OnPointerUp.Invoke(pointerEvent.position);
		}

		private void PointerDown(BaseEventData baseEventData) {
			PointerEventData pointerEvent = baseEventData as PointerEventData;
			OnPointerDown.Invoke(pointerEvent.position);
			ProcessValue(baseEventData);
		}

		private void StartDrag(BaseEventData baseEventData) {
			//dragJustStarted = true;
		}

		public void Populate(EventTrigger trigerArea) {
			EventBind.SetEvent(trigerArea, EventTriggerType.PointerDown, PointerDown);
			EventBind.SetEvent(trigerArea, EventTriggerType.PointerUp, PointerUp);
			EventBind.SetEvent(trigerArea, EventTriggerType.BeginDrag, StartDrag); // TODO rename StartDrag to BeginDrag?
			EventBind.SetEvent(trigerArea, EventTriggerType.Drag, ProcessValue); // TODO rename ProcessValue to Drag?
			EventBind.SetEvent(trigerArea, EventTriggerType.EndDrag, PointerUp); // TODO is this needed?
		}

		public void Update() {
			if (!valueUpdating && gaveFinalValue) {
				return;
			}
			if (!dragJustStarted) {
				OnDrag.Invoke(valueDrag);
				valueDrag = Vector2.zero;
				dragJustStarted = false;
			}
			OnLocalPositionChange.Invoke(valueLocalPosition);
			OnScaledPositionChange.Invoke(valueScaledPosition);
			if (!valueUpdating && !gaveFinalValue) {
				gaveFinalValue = true;
			}
		}

		public void PrintValue(Vector2 value) {
			Debug.Log(name + ": " + value);
		}
	}
}
