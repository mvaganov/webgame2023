using System.Collections.Generic;
using UnityEngine;

namespace Spreadsheet {
	public class CellGenerator : MonoBehaviour {
		[System.Serializable]
		public class CellType {
			public string name;
			public RectTransform prefab;
		}
		private Transform _transform;
		public List<CellType> cellTypes = new List<CellType>();
		private static List<List<Cell>> s_preallocatedCellsByType = new List<List<Cell>>();
		private int _popupUiIndex;

		public int PopupUiTypeIndex => _popupUiIndex;

		private void Awake() {
			_transform = transform;
			SetupCellTypes();
		}

		public void SetupCellTypes() {
			_popupUiIndex = 0;
			for (int i = 0; i < cellTypes.Count; i++) {
				cellTypes[i].prefab.gameObject.SetActive(false);
				if (cellTypes[i].name.ToLower() == "popup") {
					_popupUiIndex = i;
				}
			}
		}

		public Cell MakeNewCell(string type) {
			int index = cellTypes.FindIndex(ct => ct.name == type);
			if (index >= 0) {
				return MakeNewCell(index);
			}
			return null;
		}

		public Cell MakeNewCell(int typeIndex) {
			while (typeIndex >= s_preallocatedCellsByType.Count) {
				s_preallocatedCellsByType.Add(new List<Cell>());
			}
			List<Cell> cellBucket = s_preallocatedCellsByType[typeIndex];
			Cell cell;
			if (cellBucket.Count > 0) {
				int last = cellBucket.Count - 1;
				cell = cellBucket[last];
				cellBucket.RemoveAt(last);
			} else {
				RectTransform rect = Instantiate(cellTypes[typeIndex].prefab.gameObject).GetComponent<RectTransform>();
				cell = rect.GetComponent<Cell>();
				if (cell == null) {
					cell = rect.gameObject.AddComponent<Cell>();
				}
				cell.SetCellTypeIndex(typeIndex);
			}
			Ui.TrySetTextInputInteractable(cell.transform, false);
			cell.gameObject.SetActive(true);
			return cell;
		}

		public void FreeCellUi(Cell cell) {
			if (cell == null) { return; }
			CellPosition cellPosition = cell.position;
			RectTransform rect = cell.RectTransform;
			cell.gameObject.SetActive(false);
			Ui.SetText(rect, "");
			//if (rect.parent != ContentArea) {
			//	Debug.LogError($"is {cell} beign double-freed? parented to {rect.parent.name}, not {ContentArea.name}");
			//}
			if (_transform == null) {
				_transform = transform;
			}
			rect.SetParent(_transform);
			if (cell.spreadsheet != null) {
				cell.spreadsheet.AssignCell(cellPosition, null);
				cell.spreadsheet = null;
			}
			s_preallocatedCellsByType[cell.CellTypeIndex].Add(cell);
		}
	}
}
