using UnityEngine;

namespace MyGame {
	public class MouseLook : MonoBehaviour {
		public Transform CameraTransform;
		public Vector2 MouseSensitivity = new Vector2(5, -5);
		protected Vector3 eulerRotation;
		public bool _hideMouse = true;
		public bool useMouseMotion = true;
		[HideInInspector] public Vector2 scriptedPitchYaw;

		public virtual Vector2 PitchYaw {
			get {
				if (useMouseMotion) {
					return new Vector2(
					Input.GetAxis("Mouse Y") * MouseSensitivity.y,
					Input.GetAxis("Mouse X") * MouseSensitivity.x);
				} else {
					return scriptedPitchYaw;
				}
			}
			set => SetPitchYaw(value);
		}

		public bool HideMouse {
			get => _hideMouse;
			set {
				_hideMouse = value;
				RefreshMouseVisibility();
			}
		}

		private void RefreshMouseVisibility() {
			if (_hideMouse) {
				DoHideMouse();
			} else {
				UnhideMouse();
			}
		}

		public void SetPitchYaw(Vector2 value) {
			scriptedPitchYaw = value;
		}

		private void Reset() {
			Camera cam = Camera.main;
			if (cam != null) {
				CameraTransform = cam.transform;
			}
		}

		private void OnValidate() {
			RefreshMouseVisibility();
		}

		protected virtual void OnEnable() {
			if (_hideMouse) {
				DoHideMouse();
			}
		}

		protected virtual void OnDisable() {
			if (_hideMouse) {
				UnhideMouse();
			}
		}

		public void DoHideMouse() {
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}

		public void UnhideMouse() {
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}

		private void Start() {
			RefreshRotation();
		}

		public void RefreshRotation() {
			eulerRotation = CameraTransform.rotation.eulerAngles;
		}

		protected virtual void LateUpdate() {
			Vector2 pitchYaw = PitchYaw;
			if (pitchYaw.x != 0 || pitchYaw.y != 0) {
				eulerRotation.x += pitchYaw.x;
				eulerRotation.y += pitchYaw.y;
				CameraTransform.rotation = Quaternion.Euler(eulerRotation);
			}
		}

		public void CopyTransformRotation(Transform t) {
			CameraTransform.rotation = t.rotation;
			eulerRotation = CameraTransform.rotation.eulerAngles;
		}
	}
}
