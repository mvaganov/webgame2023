using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spreadsheet {
	public partial class Spreadsheet {
		[ContextMenuItem(nameof(CopySelectionToClipboard), nameof(CopySelectionToClipboard))]
		public List<CellRange> selection = new List<CellRange>();
		private bool _selecting;
		private CellRange currentCellSelectionRange = CellRange.Invalid;
		private Cell currentSelectedCell;
		private CellPosition currentSelectionPosition;

		public void CellPointerDown(Cell cell) {
			_selecting = true;
			SelectCell(cell, cell.position);
		}

		public void SelectCell(Cell cell, CellPosition position) {
			if (currentSelectedCell != null) {
				currentSelectedCell.SelectableComponent.OnDeselect(null);
				SetCellSize(currentSelectedCell, 1);
			}
			if (cell == null) {
				cell = GetCellUi(position);
			}
			bool newSelection = currentSelectedCell != cell;
			if (currentSelectedCell != null && newSelection) {
				currentSelectedCell.Interactable = false;
			}
			currentSelectedCell = cell;
			currentSelectionPosition = position;
			if (cell != null) {
				SetCellSize(currentSelectedCell, 1+1f/16);
				bool oneCellBeingSelected = cell.Selected && !cell.Interactable && currentCellSelectionRange.Area == 1;
				if (oneCellBeingSelected) {
					cell.Interactable = true;
				} else {
					cell.Selected = true;
					if (cell.position != currentSelectionPosition) {
						throw new System.Exception("cell and position expected to match!");
					}
					if (cell.SelectableComponent != null) {
						cell.SelectableComponent.OnSelect(null);
					}
				}
			}
			currentCellSelectionRange = new CellRange(currentSelectionPosition);
			UpdateSelection();
		}

		private void SetCellSize(Cell cell, float size) {
			RectTransform rt = cell.GetComponent<RectTransform>();
			rt.localScale = size * Vector3.one;
		}

		public void CellPointerMove(Cell cell) {
			if (_selecting) {
				currentCellSelectionRange.End = cell.position;
				UpdateSelection();
			}
		}

		private void UpdateSelection() {
			for (int r = 0; r < rows.Count; ++r) {
				Row row = rows[r];
				Cell[] cells = row.GetCellLookupTable(false);
				if (cells == null) {
					continue;
				}
				for (int c = 0; c < cells.Length; ++c) {
					Cell cell = cells[c];
					if (cell == null) { continue; }
					cell.Selected = IsSelected(cell.position);
				}
			}
			if (currentCellSelectionRange.Area > 1 && currentSelectedCell!= null && currentSelectedCell.SelectableComponent != null) {
				currentSelectedCell.SelectableComponent.OnPointerUp(FakePointerEventData);
				currentSelectedCell.SelectableComponent.OnDeselect(null);
			}
		}

		public bool IsSelected(CellPosition cellPosition) {
			if (currentCellSelectionRange.IsValid && currentCellSelectionRange.Contains(cellPosition)) {
				return true;
			}
			for (int i = 0; i < selection.Count; ++i) {
				if (selection[i].IsValid && selection[i].Contains(cellPosition)) {
					return true;
				}
			}
			return false;
		}

		public void CellPointerUp(Cell cell) {
			//UpdateSelection();
			_selecting = false;
		}
	}
}
