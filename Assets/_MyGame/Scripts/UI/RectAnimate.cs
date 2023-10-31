using UnityEngine;

namespace Spreadsheet {
	public class RectAnimate : MonoBehaviour {
		[SerializeField] private RectTransform[] _states;
		[SerializeField] private float _duration = 3;
		[SerializeField] private bool _useStartAsZeroFrame = true;
		[SerializeField] private bool _animateSize;
		[SerializeField] private bool _animateRotation;
		[SerializeField] private bool _animateScale;
		[SerializeField] private bool _animateOffset;
		[SerializeField] private bool _animatePivot;
		[SerializeField] private bool _animateAnchor;
		private RectTransform rectTransform;
		private float _timer;
		private int _index, _lastIndex;

		public int Index {
			get => _index;
			set {
				_index = value;
				_timer = 0;
			}
		}

		public void AnimateToNext() {
			if (_useStartAsZeroFrame && Index == 0 && _lastIndex == _index) {
				RectTransform rt = _states[0];
				RectTransform self = GetComponent<RectTransform>();
				CopyRectTransform(self, rt);
			}
			++Index;
			if (Index >= _states.Length) {
				Index = 0;
			}
		}

		void Start() {
			rectTransform = GetComponent<RectTransform>();
			if (_useStartAsZeroFrame) {
				if (_states.Length < 0 || _states[0] != null) {
					System.Array.Resize(ref _states, _states.Length + 1);
					for (int i = _states.Length - 1; i > 0; i--) {
						_states[i] = _states[i - 1];
					}
				}
				GameObject go = new GameObject(name + "[0]");
				RectTransform rt = _states[0] = go.AddComponent<RectTransform>();
				RectTransform self = GetComponent<RectTransform>();
				rt.SetParent(self.parent, false);
				CopyRectTransform(self, rt);
			}
		}

		private static void CopyRectTransform(RectTransform source, RectTransform destination) {
			destination.anchorMin = source.anchorMin;
			destination.anchorMax = source.anchorMax;
			destination.pivot = source.pivot;
			destination.offsetMin = source.offsetMin;
			destination.offsetMax = source.offsetMax;
			destination.localScale = source.localScale;
			destination.localRotation = source.localRotation;
		}

		private void Lerp(RectTransform cursor, RectTransform a, RectTransform b, float progress) {
			if (_animateAnchor) {
				cursor.anchorMin = Vector2.Lerp(a.anchorMin, b.anchorMin, progress);
				cursor.anchorMax = Vector2.Lerp(a.anchorMax, b.anchorMax, progress);
			}
			if (_animatePivot) {
				cursor.pivot = Vector2.Lerp(a.pivot, b.pivot, progress);
			}
			if (_animateOffset) {
				cursor.offsetMin = Vector2.Lerp(a.offsetMin, b.offsetMin, progress);
				cursor.offsetMax = Vector2.Lerp(a.offsetMax, b.offsetMax, progress);
			}
			if (_animateOffset) {
				cursor.localScale = Vector3.Lerp(a.localScale, b.localScale, progress);
			}
			if (_animateRotation) {
				cursor.localRotation = Quaternion.Lerp(a.localRotation, b.localRotation, progress);
			}
			if (_animateSize) {
				cursor.sizeDelta = Vector2.Lerp(a.sizeDelta, b.sizeDelta, progress);
			}
		}

		void Update() {
			if (_index == _lastIndex) { return; }
			_timer += Time.deltaTime;
			float progress = _timer / _duration;
			if (progress < 0) {
				progress = 0;
			} else if(progress > 1) {
				progress = 1;
			}
			Lerp(rectTransform, _states[_lastIndex], _states[_index], progress);
			if (progress >= 1) {
				_lastIndex = _index;
				_timer = 0;
			}
		}
	}
}
