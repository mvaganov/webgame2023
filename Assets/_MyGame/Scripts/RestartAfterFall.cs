using UnityEngine;

namespace MyGame {
	public class RestartAfterFall : MonoBehaviour {
		private Vector3 startPosition;
		private Quaternion startRotation;
		public float minimumY = -20;

		void Start() {
			startPosition = transform.position;
			startRotation = transform.rotation;
		}

		void Update() {
			if (transform.position.y < minimumY) {
				Restart();
			}
		}

		public void Restart() {
			transform.position = startPosition;
			transform.rotation = startRotation;
			Rigidbody rb = GetComponent<Rigidbody>();
			if (rb != null) {
				rb.velocity = Vector3.zero;
				rb.angularVelocity = Vector3.zero;
			}
		}
	}
}