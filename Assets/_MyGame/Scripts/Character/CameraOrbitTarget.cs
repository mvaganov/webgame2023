using UnityEngine;

namespace MyGame {
	public class CameraOrbitTarget : MonoBehaviour {
		public Transform target;
		public Transform cameraTransform;
		public float distance = 10;
		public float mouseWheelSensitivity = 5;

		void LateUpdate() {
			if (mouseWheelSensitivity > 0) {
				distance += Input.GetAxis("Mouse ScrollWheel") * mouseWheelSensitivity;
				if (distance < 0) {
					distance = 0;
				}
			}
			Vector3 distanceFromTarget = cameraTransform.forward * distance;
			cameraTransform.position = target.position - distanceFromTarget;
		}
	}
}
