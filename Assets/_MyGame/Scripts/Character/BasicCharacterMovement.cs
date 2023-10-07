using UnityEngine;

namespace MyGame {
	public class BasicCharacterMovement : AxisMovement {
		protected Vector3 groundUpDirection = Vector3.up;
		[System.Serializable] public class JumpSettings {
			public KeyCode jumpKey = KeyCode.Space;
			public float jumpSpeed = 5;
			public Vector3 gravityDirection = Vector3.down;
			public bool jumpPressed;

			public bool UserWantsToJump() {
				return jumpPressed || UnityEngine.Input.GetKey(jumpKey);
			}
		}

		public JumpSettings jumpSettings = new JumpSettings();
		private Collider[] collisionChecks = new Collider[3];
		public Vector2 Input {
			get => new Vector2(axis[0].Value, axis[1].Value);
			set => SetInput(value);
		}

		public void SetInput(Vector2 value) {
			axis[0].Value = value.x;
			axis[1].Value = value.y;
		}

		private void OnValidate() {
			AssignSameCameraToOtherFriendlyScripts();
		}

		private void AssignSameCameraToOtherFriendlyScripts() {
			MouseLook mouseLook = GetComponent<MouseLook>();
			if (mouseLook != null) {
				mouseLook.CameraTransform = lookPerspective;
			}
			CameraOrbitTarget cameraOrbitTarget = GetComponent<CameraOrbitTarget>();
			if (cameraOrbitTarget != null) {
				cameraOrbitTarget.cameraTransform = lookPerspective;
			}
		}

		protected virtual void Awake() {
			base.Start();
			if (lookPerspective == null) {
				lookPerspective = Camera.main.transform;
				if (lookPerspective == null) {
					return;
				}
				Debug.LogWarning(nameof(lookPerspective) + " was not assigned. using " + lookPerspective.name);
				AssignSameCameraToOtherFriendlyScripts();
			}
		}

		public override void Update() {
			Vector3 localMoveDirection = new Vector3(axis[0].Value, axis[1].Value, axis[2].Value);
			Transform perspective = lookPerspective != null ? lookPerspective : body;
			float fallSpeed;
			if (rb.useGravity) {
				bool userWantsToJump = jumpSettings.UserWantsToJump();
				fallSpeed = CalculateFallOrJumpVelocity(userWantsToJump);
			} else {
				fallSpeed = 0;
			}
			Vector3 absoluteMoveDirection =	Vector3.zero;
			if (localMoveDirection != Vector3.zero) {
				absoluteMoveDirection = perspective.TransformDirection(localMoveDirection);
				if (perspective.forward == Vector3.forward || perspective.forward == Vector3.back) {
					UseUpAsForward(ref absoluteMoveDirection);
				}
				if (rb.useGravity) {
					absoluteMoveDirection = ClampMoveDirectionToGround(absoluteMoveDirection);
				}
			}
			if (absoluteMoveDirection != Vector3.zero) {
				if (rb != null) {
					rb.velocity = absoluteMoveDirection * speed;
				} else {
					body.transform.position += absoluteMoveDirection * (Time.deltaTime * speed);
				}
				body.rotation = Quaternion.LookRotation(absoluteMoveDirection, groundUpDirection);
			} else {
				rb.velocity = Vector3.zero;
			}
			if (rb.useGravity) {
				rb.velocity += jumpSettings.gravityDirection * fallSpeed;
			}
		}

		private void UseUpAsForward(ref Vector3 absoluteMoveDirection) {
			if (Mathf.Abs(absoluteMoveDirection.y) > Mathf.Abs(absoluteMoveDirection.z)) {
				float temp = absoluteMoveDirection.y;
				absoluteMoveDirection.y = absoluteMoveDirection.z;
				absoluteMoveDirection.z = temp;
			}
		}

		private float CalculateFallOrJumpVelocity(bool userWantsToJump) {
			float fallSpeed;
			if (userWantsToJump) {
				int countObjectsAtFoot = Physics.OverlapSphereNonAlloc(transform.position, .125f, collisionChecks);
				// one of the objects found will be the player itself. if there is another object, then we can jump.
				if (countObjectsAtFoot > 1) {
					fallSpeed = -jumpSettings.jumpSpeed;
					return fallSpeed;
				}
			}
			fallSpeed = Vector3.Dot(jumpSettings.gravityDirection, rb.velocity);
			return fallSpeed;
		}

		private Vector3 ClampMoveDirectionToGround(Vector3 moveDirection) {
			float dirAmount = moveDirection.magnitude;
			moveDirection = Vector3.ProjectOnPlane(moveDirection, groundUpDirection);
			if (dirAmount > 1f/32) {
				moveDirection.Normalize();
				moveDirection *= dirAmount;
			} else {
				moveDirection = Vector3.zero;
			}
			return moveDirection;
		}
	}
}
