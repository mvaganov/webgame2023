using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace MyGame {
	public class CharacterUserInterface : MonoBehaviour
	{
		public EventTrigger up, left, down, right, jump, viewDrag;
		public DragSensitive joystick;
		public DragSensitive cameraDragRotationArea;
		public BasicCharacterMovement character;

		private void Start() {
			Populate(character);
		}

		public void Populate(BasicCharacterMovement character) {
			const int X = 0, Y = 1;
			SetEvent(up, EventTriggerType.PointerDown, (BaseEventData) => character.axis[Y].inputValue = 1);
			SetEvent(left, EventTriggerType.PointerDown, (BaseEventData) => character.axis[X].inputValue = -1);
			SetEvent(down, EventTriggerType.PointerDown, (BaseEventData) => character.axis[Y].inputValue = -1);
			SetEvent(right, EventTriggerType.PointerDown, (BaseEventData) => character.axis[X].inputValue = 1);
			SetEvent(jump, EventTriggerType.PointerDown, (BaseEventData) => character.jumpSettings.jumpPressed = true);

			SetEvent(up, EventTriggerType.PointerUp, (BaseEventData) => character.axis[Y].inputValue = 0);
			SetEvent(left, EventTriggerType.PointerUp, (BaseEventData) => character.axis[X].inputValue = 0);
			SetEvent(down, EventTriggerType.PointerUp, (BaseEventData) => character.axis[Y].inputValue = 0);
			SetEvent(right, EventTriggerType.PointerUp, (BaseEventData) => character.axis[X].inputValue = 0);
			SetEvent(jump, EventTriggerType.PointerUp, (BaseEventData) => character.jumpSettings.jumpPressed = false);

			if (cameraDragRotationArea != null) {
				MouseLook mouseLook = character.GetComponent<MouseLook>();
				cameraDragRotationArea.OnDrag.AddListener(mouseLook.SetYawPitch);
				mouseLook.useMouseMotion = false;
			}
			joystick.OnScaledPositionChange.AddListener(character.SetInput);
		}

		public static EventTrigger.Entry GetEventEntry(EventTrigger eventTrigger, EventTriggerType type) {
			for(int i = 0; i < eventTrigger.triggers.Count; ++i) {
				if (eventTrigger.triggers[i].eventID == type) {
					return eventTrigger.triggers[i];
				}
			}
			return null;
		}

		public static void SetEvent(EventTrigger eventTrigger, EventTriggerType type, UnityAction<BaseEventData> action) {
			EventTrigger.Entry entry = GetEventEntry(eventTrigger, type);
			if (entry == null) {
				entry = new EventTrigger.Entry() {
					eventID = type,
				};
				eventTrigger.triggers.Add(entry);
			}
			entry.callback.RemoveListener(action);
			entry.callback.AddListener(action);
		}
	}
}
