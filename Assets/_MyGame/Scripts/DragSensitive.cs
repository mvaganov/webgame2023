using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace MyGame {
	public class DragSensitive : MonoBehaviour {
		public EventTrigger triggerArea;
		public bool clamp = true;
		public UnityEvent_Vector2 OnDrag;
		public UnityEvent_Vector2 OnLocalPositionChange;
		public UnityEvent_Vector2 OnScaledPositionChange;
		private Vector2 valueDrag, valueLocalPosition, valueScaledPosition;
		private bool valueUpdating;
		private bool gaveFinalValue;
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
			Populate(triggerArea);
		}

		private void ProcessValue(BaseEventData baseEventData) {
			PointerEventData drag = baseEventData as PointerEventData;
			valueDrag = new Vector2(-drag.delta.y, drag.delta.x);

			valueLocalPosition = drag.position - (Vector2)rectTransform.position;
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

		private void StopProcessing(BaseEventData baseEventData) {
			valueDrag = Vector2.zero;
			valueLocalPosition = Vector2.zero;
			valueScaledPosition = Vector2.zero;
			valueUpdating = gaveFinalValue = false;
		}

		public void Populate(EventTrigger trigerArea) {
			CharacterUserInterface.SetEvent(trigerArea, EventTriggerType.PointerDown, ProcessValue);
			CharacterUserInterface.SetEvent(trigerArea, EventTriggerType.PointerUp, StopProcessing);
			CharacterUserInterface.SetEvent(trigerArea, EventTriggerType.Drag, ProcessValue);
			CharacterUserInterface.SetEvent(trigerArea, EventTriggerType.EndDrag, StopProcessing);
		}

		public void Update() {
			if (!valueUpdating && gaveFinalValue) {
				return;
			}
			OnDrag.Invoke(valueDrag);
			valueDrag = Vector2.zero;
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
