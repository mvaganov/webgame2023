using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Spreadsheet {
	public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
		private bool _mouseHasHovered;
		private bool _mouseIsHovering;
		private bool _automaticallyFadeAway = true;
		private float _timer;
		[SerializeField] private float _fadeTime = 1;
		[SerializeField] private float _fadeTimeUnhovered = 8;
		[SerializeField] private AnimationCurve _animationCurve;
		[SerializeField] private UnityEngine.Object[] _imagesToFade;

		public bool AutomaticallyFadeAway {
			get => _automaticallyFadeAway;
			set {
				FadeTimer = 0;
				_automaticallyFadeAway = value;
			}
		}

		public float FadeTimer {
			get => _timer;
			set {
				_timer = value;
				SetTransparency(_timer);
			}
		}

		public void RestartFade() {
			FadeTimer = 0;
			_mouseHasHovered = _mouseIsHovering = false;
		}

		private void OnEnable() {
			RestartFade();
		}

		public void SetTransparency(float progress) {
			if (progress <= 0) {
				progress = 0;
			} else if (progress >= 1) {
				progress = 1;
			}
			progress = _animationCurve.Evaluate(progress);
			Array.ForEach(_imagesToFade, img => {
				if (!Ui.TryGetColor(img, out Color color)) {
					return;
				}
				color.a = progress;
				Ui.SetColor(img, color);
			});
		}

		public void OnPointerEnter(PointerEventData eventData) {
			_mouseIsHovering = _mouseHasHovered = true;
			FadeTimer = 0;
		}

		public void OnPointerExit(PointerEventData eventData) {
			_mouseIsHovering = false;
		}

		void Update() {
			if (_mouseIsHovering || !AutomaticallyFadeAway) { return; }
			_timer += Time.deltaTime;
			float timeLimit = _mouseHasHovered ? _fadeTime : _fadeTimeUnhovered;
			float progress = _timer / timeLimit;
			SetTransparency(progress);
			if (progress >= 1) {
				gameObject.SetActive(false);
			}
		}
	}
}
