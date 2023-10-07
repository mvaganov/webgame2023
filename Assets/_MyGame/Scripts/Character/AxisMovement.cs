using UnityEngine;
using UnityEngine.Events;

namespace MyGame {
	public class AxisMovement : MonoBehaviour {
		public float speed = 5;
		public Transform lookPerspective;
		public Transform body;
		public KeyAxis[] axis = new KeyAxis[] {
			new KeyAxis("[x] Right/Left",
				new KeyCode[] { KeyCode.D, KeyCode.RightArrow },
				new KeyCode[] { KeyCode.A, KeyCode.LeftArrow },
				1),
			new KeyAxis("[y] Up/Down",
				new KeyCode[] { KeyCode.Q, KeyCode.PageUp },
				new KeyCode[] { KeyCode.E, KeyCode.PageDown },
				1),
			new KeyAxis("[x] Forward/Backward",
				new KeyCode[] { KeyCode.W, KeyCode.UpArrow },
				new KeyCode[] { KeyCode.S, KeyCode.DownArrow },
				1),
		};
		protected Rigidbody rb;

		[System.Serializable] public class Events {
			public UnityEvent OnMove;
		}

		[System.Serializable]
		public struct KeyAxis {
			public string name;
			public KeyCode[] increase, decrease;
			public float baseValue;
			[HideInInspector] public float inputValue;
			public KeyAxis(string name, KeyCode[] increase, KeyCode[] decrease, float baseValue) {
				this.name = name;
				this.baseValue = baseValue;
				this.increase = increase;
				this.decrease = decrease;
				inputValue = 0;
			}
			public float Value {
				get {
					float value = inputValue * baseValue;
					if (value == 0) {
						if (OneOfTheseIsPressed(increase)) { value += baseValue; }
						if (OneOfTheseIsPressed(decrease)) { value -= baseValue; }
					}
					return value;
				}
				set {
					inputValue = value;
				}
			}
			private static bool OneOfTheseIsPressed(KeyCode[] keyCodes) {
				for (int i = 0; i < keyCodes.Length; ++i) {
					if (Input.GetKey(keyCodes[i])) {
						return true;
					}
				}
				return false;
			}
		}

		protected virtual void Start() {
			rb = body.GetComponent<Rigidbody>();
			if (body == null) {
				Debug.LogError($"{this} needs {nameof(body)} value set");
			}
		}

		public virtual void Update() {
			Vector3 moveDirection = new Vector3(axis[0].Value, axis[1].Value, axis[2].Value);
			ClearInputValue();
			Transform perspective = lookPerspective != null ? lookPerspective : body;
			if (moveDirection != Vector3.zero) {
				moveDirection = perspective.TransformDirection(moveDirection);
				if (rb != null) {
					rb.velocity = moveDirection * speed;
				} else {
					body.transform.position += moveDirection * (Time.deltaTime * speed);
				}
			}
		}

		public void ClearInputValue() {
			for (int i = 0; i < axis.Length; ++i) {
				axis[i].inputValue = 0;
			}
		}

		public void MoveDirection(int axisIndex, int direction, bool pressed) {
			Debug.Log($"dir[{axisIndex}] += {direction}");
			if (pressed) {
				axis[axisIndex].inputValue = direction * axis[axisIndex].baseValue;
			} else {
				axis[axisIndex].inputValue = 0;
			}
		}

		public void MoveDirection0(int direction) => MoveDirection(0, direction, true);
		public void MoveDirection1(int direction) => MoveDirection(1, direction, true);
		public void MoveDirection2(int direction) => MoveDirection(2, direction, true);
		public void StopDirection0(int direction) => MoveDirection(0, direction, false);
		public void StopDirection1(int direction) => MoveDirection(1, direction, false);
		public void StopDirection2(int direction) => MoveDirection(2, direction, false);
		public void TurnYaw(int degrees) {
			rb.transform.Rotate(0, degrees, 0);
		}
	}
}
