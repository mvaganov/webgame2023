using UnityEngine;
using UnityEngine.EventSystems;

namespace Spreadsheet {
	public class Cell : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler {
		public CellPosition position;
		public Spreadsheet spreadsheet;

		public static void Set(GameObject gameObject, Spreadsheet speradsheet, int row, int column) {
			Cell cell = gameObject.GetComponent<Cell>();
			if (cell == null) {
				cell = gameObject.AddComponent<Cell>();
			}
			cell.spreadsheet = speradsheet;
			cell.position = new CellPosition(row, column);
		}

		public void AddToSelection() {
			spreadsheet.AddSelection(position);
		}

		public void OnBeginDrag(PointerEventData eventData) {
			spreadsheet.AddSelection(position);
		}

		public void OnDrag(PointerEventData eventData) {
			spreadsheet.AddSelection(position);
		}

		public void OnEndDrag(PointerEventData eventData) {
		}
	}
}
