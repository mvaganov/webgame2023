using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spreadsheet {
	public partial class Spreadsheet {
		private RectTransform _tooltipUiElement;
		private RectTransform _tooltipAnchor;
		public void SetPopup(Cell cell, string text) {
			if (_tooltipUiElement == null) {
				Cell popup = cellGenerator.MakeNewCell(cellGenerator.PopupUiTypeIndex).Set(this, CellPosition.Invalid);
				_tooltipUiElement = popup.RectTransform;
				_tooltipAnchor = new GameObject().AddComponent<RectTransform>();
				_tooltipAnchor.sizeDelta = Vector2.zero;
				_tooltipAnchor.anchorMin = _tooltipUiElement.anchorMin;
				_tooltipAnchor.anchorMax = _tooltipUiElement.anchorMax;
				FollowRectTransform followRect = _tooltipUiElement.gameObject.AddComponent<FollowRectTransform>();
				followRect.toFollow = _tooltipAnchor;
			} else {
				_tooltipUiElement.SetAsLastSibling();
			}
			RectTransform cellRectTransform = cell.GetComponent<RectTransform>();
			RectTransform popupRectTransform = _tooltipUiElement.GetComponent<RectTransform>();
			Ui.SetText(_tooltipUiElement, text);
			Ui.SetColor(_tooltipUiElement, _tooltipColor);
			popupRectTransform.SetParent(_transform.parent);
			_tooltipAnchor.SetParent(ContentArea);
			_tooltipAnchor.anchoredPosition = cellRectTransform.anchoredPosition
				+ new Vector2(0, -cellRectTransform.sizeDelta.y);
			_tooltipUiElement.gameObject.SetActive(true);
			Tooltip tooltip = _tooltipUiElement.GetComponentInChildren<Tooltip>();
			if (tooltip != null) {
				tooltip.RestartFade();
			}
		}
	}
}
