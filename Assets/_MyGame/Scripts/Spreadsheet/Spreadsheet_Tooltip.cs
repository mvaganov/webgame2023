using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spreadsheet {
	public partial class Spreadsheet {
		public enum MetaDataKind { None, Error, Note }
		public class MetaData {
			public MetaDataKind kind;
			public string data;
			public MetaData(MetaDataKind metaKind, string data) {
				this.kind = metaKind; this.data = data;
			}
		}

		private RectTransform _tooltipUiElement;
		private RectTransform _tooltipAnchor;
		private Dictionary<CellPosition, List<MetaData>> cellMetaData = new Dictionary<CellPosition, List<MetaData>>();
		private Dictionary<CellPosition, RectTransform> cellMetaDataPopup = new Dictionary<CellPosition, RectTransform>();

		public void AddMetaData(CellPosition cellPosition, MetaData metaData) {
			if (!cellMetaData.TryGetValue(cellPosition, out var metaList)) {
				cellMetaData[cellPosition] = metaList = new List<MetaData> ();
			}
			metaList.Add(metaData);
		}

		private RectTransform GetMetaDataPopup(CellPosition position) {
			Cell cell = GetCellUi(position);
			if (cell == null) {
				return null;
			}
			if (!cellMetaDataPopup.TryGetValue(position, out RectTransform tooltipElement)) {
				Cell popup = cellGenerator.MakeNewCell(cellGenerator.PopupUiTypeIndex).Set(this, CellPosition.Invalid);
				tooltipElement = popup.RectTransform;
				RectTransform tooltipAnchor = new GameObject().AddComponent<RectTransform>();
				tooltipAnchor.sizeDelta = Vector2.zero;
				tooltipAnchor.anchorMin = tooltipElement.anchorMin;
				tooltipAnchor.anchorMax = tooltipElement.anchorMax;
				FollowRectTransform followRect = tooltipElement.gameObject.AddComponent<FollowRectTransform>();
				followRect.toFollow = tooltipAnchor;
			} else {
				tooltipElement.SetAsLastSibling();
			}

			return tooltipElement;
		}

		public void SetMetaData(Cell cell, MetaData metaData) {
			AddMetaData(cell.position, metaData);
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
			Ui.SetText(_tooltipUiElement, metaData.data);
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
