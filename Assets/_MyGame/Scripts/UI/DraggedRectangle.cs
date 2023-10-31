using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MyGame {
	public class DraggedRectangle : MonoBehaviour {
		public RectTransform rectTransform;
		public float borderSize = 4;
		public Region region;
		private Vector3[] worldCorners = new Vector3[4];
		[SerializeField] private bool _dragCornersToResize = true;

		public void ToggleCornerDragToResize() => _dragCornersToResize = !_dragCornersToResize;

		[Flags]
		public enum Region {
			None = 0,
			TopLeft = 5, Top = 1, TopRight = 9,
			Left = 4, Center = 15, Right = 8,
			BottomLeft = 6, Bottom = 2, BottomRight = 10,
		}

		private void Start() {
			if (rectTransform == null) {
				rectTransform = GetComponent<RectTransform>();
			}
		}

		public void MoveDragged(Vector2 dragDelta) {
			switch (region) {
				case Region.None:
				case Region.Center:
					rectTransform.anchoredPosition += dragDelta;
					break;
				default:
					Vector2 sizeDelta = rectTransform.sizeDelta;
					Vector3 position = rectTransform.position;

					bool xChange = false;
					float pivot = 0;
					if (region.HasFlag(Region.Left)) { sizeDelta.x -= dragDelta.x; xChange = true; pivot = 1 - rectTransform.pivot.x; }
					if (region.HasFlag(Region.Right)) { sizeDelta.x += dragDelta.x; xChange = true; pivot = rectTransform.pivot.x; }
					if (xChange) { position += new Vector3(dragDelta.x * pivot, 0); }

					bool yChange = false;
					if (region.HasFlag(Region.Top)) { sizeDelta.y += dragDelta.y; yChange = true; pivot = rectTransform.pivot.y; }
					if (region.HasFlag(Region.Bottom)) { sizeDelta.y -= dragDelta.y; yChange = true; pivot = 1 - rectTransform.pivot.y; }
					if (yChange) { position += new Vector3(0, dragDelta.y * pivot); }

					rectTransform.position = position;
					rectTransform.sizeDelta = sizeDelta;
					break;
			}
		}

		public Region GetRegion(Vector3 position) {
			Region region = Region.None;
			rectTransform.GetWorldCorners(worldCorners);
			float top = worldCorners[1].y;
			float left = worldCorners[0].x;
			float right = worldCorners[2].x;
			float bottom = worldCorners[0].y;
			if (position.y <= top && position.y >= top - borderSize) { region |= Region.Top; }
			if (position.y >= bottom && position.y <= bottom + borderSize) { region |= Region.Bottom; }
			if (position.x <= right && position.x >= right - borderSize) { region |= Region.Right; }
			if (position.x >= left && position.x <= left + borderSize) { region |= Region.Left; }
			return region;
		}

		public void PointerDown(BaseEventData baseEventData) {
			PointerEventData pointerEvent = baseEventData as PointerEventData;
			if (_dragCornersToResize) {
				region = GetRegion(pointerEvent.position);
			} else {
				region = Region.Center;
			}
		}

		public void PointerUp() {
			region = Region.None;
		}
	}
}
