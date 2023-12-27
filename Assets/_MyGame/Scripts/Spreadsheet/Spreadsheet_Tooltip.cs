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

		public class MetaDataUiPair {
			// TODO rename 'tooltip' to 'metadata'
			public RectTransform tooltipElement;
			public RectTransform tooltipAnchor;
			public void RestartFade() {
				Tooltip tooltip = tooltipElement.GetComponentInChildren<Tooltip>();
				if (tooltip != null) {
					tooltip.RestartFade();
				}
			}
		}

		private RectTransform _tooltipUiElement;
		private RectTransform _tooltipAnchor;
		private Dictionary<CellPosition, List<MetaData>> cellMetaData = new Dictionary<CellPosition, List<MetaData>>();
		private Dictionary<CellPosition, MetaDataUiPair> cellMetaDataUi = new Dictionary<CellPosition, MetaDataUiPair>();

		public MetaDataUiPair GetMetaDataUiPair(CellPosition cellPosition) {
			if (!cellMetaDataUi.TryGetValue(cellPosition, out MetaDataUiPair metaDataUiPair)) {
				return null;
			}
			return metaDataUiPair;
		}

		public MetaDataUiPair AddMetaData(CellPosition cellPosition, MetaData metaData) {
			if (!cellMetaData.TryGetValue(cellPosition, out var metaList)) {
				cellMetaData[cellPosition] = metaList = new List<MetaData> ();
			}
			metaList.Add(metaData);
			return UpdateMetaDataUi(cellPosition);
		}

		public bool ClearMetaDataUi(CellPosition position) {
			if (!cellMetaDataUi.TryGetValue(position, out MetaDataUiPair metaDataPair)) {
				return false;
			}
			DestroyFunction(metaDataPair.tooltipElement.gameObject);
			DestroyFunction(metaDataPair.tooltipAnchor.gameObject);
			cellMetaDataUi.Remove(position);
			return true;
		}

		public MetaDataUiPair UpdateMetaDataUi(CellPosition position) {
			Cell cell = GetCellUi(position);
			if (cell == null) {
				return null;
			}
			RectTransform cellRectTransform = cell.GetComponent<RectTransform>();
			if (!cellMetaDataUi.TryGetValue(position, out MetaDataUiPair metaDataPair)) {
				cellMetaDataUi[position] = metaDataPair = new MetaDataUiPair ();
				Cell popup = cellGenerator.MakeNewCell(cellGenerator.PopupUiTypeIndex).Set(this, CellPosition.Invalid);
				metaDataPair.tooltipElement = popup.RectTransform;
				metaDataPair.tooltipAnchor = new GameObject().AddComponent<RectTransform>();
				metaDataPair.tooltipAnchor.sizeDelta = Vector2.zero;
				metaDataPair.tooltipAnchor.anchorMin = metaDataPair.tooltipElement.anchorMin;
				metaDataPair.tooltipAnchor.anchorMax = metaDataPair.tooltipElement.anchorMax;
				FollowRectTransform followRect = metaDataPair.tooltipElement.gameObject.AddComponent<FollowRectTransform>();
				followRect.toFollow = metaDataPair.tooltipAnchor;
			} else {
				metaDataPair.tooltipElement.SetAsLastSibling();
			}
			RectTransform popupRectTransform = metaDataPair.tooltipElement.GetComponent<RectTransform>();
			cellMetaData.TryGetValue(position, out var metaList);
			// TODO create some mechanism in the popup that allows it to scroll between the different meta datas
			Ui.SetText(metaDataPair.tooltipElement, metaList[0].data);
			Ui.SetColor(metaDataPair.tooltipElement, _tooltipColor);
			popupRectTransform.SetParent(_transform.parent);
			metaDataPair.tooltipAnchor.SetParent(ContentArea);
			metaDataPair.tooltipAnchor.anchoredPosition = cellRectTransform.anchoredPosition
				+ new Vector2(0, -cellRectTransform.sizeDelta.y);
			metaDataPair.tooltipElement.gameObject.SetActive(true);
			return metaDataPair;
		}

		public void SetMetaData(Cell cell, MetaData metaData) {
			MetaDataUiPair metaDataUi = AddMetaData(cell.position, metaData);
			//RectTransform cellRectTransform = cell.GetComponent<RectTransform>();
			//if (_tooltipUiElement == null) {
			//	Cell popup = cellGenerator.MakeNewCell(cellGenerator.PopupUiTypeIndex).Set(this, CellPosition.Invalid);
			//	_tooltipUiElement = popup.RectTransform;
			//	_tooltipAnchor = new GameObject().AddComponent<RectTransform>();
			//	_tooltipAnchor.sizeDelta = Vector2.zero;
			//	_tooltipAnchor.anchorMin = _tooltipUiElement.anchorMin;
			//	_tooltipAnchor.anchorMax = _tooltipUiElement.anchorMax;
			//	FollowRectTransform followRect = _tooltipUiElement.gameObject.AddComponent<FollowRectTransform>();
			//	followRect.toFollow = _tooltipAnchor;
			//} else {
			//	_tooltipUiElement.SetAsLastSibling();
			//}
			//RectTransform popupRectTransform = _tooltipUiElement.GetComponent<RectTransform>();
			//Ui.SetText(_tooltipUiElement, metaData.data);
			//Ui.SetColor(_tooltipUiElement, _tooltipColor);
			//popupRectTransform.SetParent(_transform.parent);
			//_tooltipAnchor.SetParent(ContentArea);
			//_tooltipAnchor.anchoredPosition = cellRectTransform.anchoredPosition
			//	+ new Vector2(0, -cellRectTransform.sizeDelta.y);
			//_tooltipUiElement.gameObject.SetActive(true);
			metaDataUi.RestartFade();
			//Tooltip tooltip = _tooltipUiElement.GetComponentInChildren<Tooltip>();
			//if (tooltip != null) {
			//	tooltip.RestartFade();
			//}

			/// TODO add logic to clear the MetaDataUi correctly when it is scrolled out of view
		}
	}
}
