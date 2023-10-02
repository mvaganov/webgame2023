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
			EventBind.SetEvent(up, EventTriggerType.PointerDown, (BaseEventData) => character.axis[Y].inputValue = 1);
			EventBind.SetEvent(left, EventTriggerType.PointerDown, (BaseEventData) => character.axis[X].inputValue = -1);
			EventBind.SetEvent(down, EventTriggerType.PointerDown, (BaseEventData) => character.axis[Y].inputValue = -1);
			EventBind.SetEvent(right, EventTriggerType.PointerDown, (BaseEventData) => character.axis[X].inputValue = 1);
			EventBind.SetEvent(jump, EventTriggerType.PointerDown, (BaseEventData) => character.jumpSettings.jumpPressed = true);

			EventBind.SetEvent(up, EventTriggerType.PointerUp, (BaseEventData) => character.axis[Y].inputValue = 0);
			EventBind.SetEvent(left, EventTriggerType.PointerUp, (BaseEventData) => character.axis[X].inputValue = 0);
			EventBind.SetEvent(down, EventTriggerType.PointerUp, (BaseEventData) => character.axis[Y].inputValue = 0);
			EventBind.SetEvent(right, EventTriggerType.PointerUp, (BaseEventData) => character.axis[X].inputValue = 0);
			EventBind.SetEvent(jump, EventTriggerType.PointerUp, (BaseEventData) => character.jumpSettings.jumpPressed = false);

			if (cameraDragRotationArea != null) {
				MouseLook mouseLook = character.GetComponent<MouseLook>();
				cameraDragRotationArea.OnDrag.AddListener(mouseLook.SetYawPitch);
				mouseLook.useMouseMotion = false;
			}
			joystick.OnScaledPositionChange.AddListener(character.SetInput);
		}
	}
}
