using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spreadsheet {
	public class FollowRectTransform : MonoBehaviour {
		public RectTransform toFollow;
		private RectTransform _self;
		public Vector3 targetPosition;
		private void Awake() {
			_self = GetComponent<RectTransform>();
		}
		private Vector3[] corners = new Vector3[4];
		private void Update() {
			//toFollow.GetWorldCorners(corners);
			_self.position = toFollow.position;// corners[0];
			//targetPosition = corners[0];
		}
	}
}
