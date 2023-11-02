using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spreadsheet {
	public partial class Spreadsheet {
		[ContextMenuItem(nameof(CopySelectionToClipboard), nameof(CopySelectionToClipboard))]
		public List<CellRange> selection = new List<CellRange>();
		private bool _selecting;
		private CellRange currentCellSelection = CellRange.Invalid;
		private Cell currentSelectedCell;
		private CellPosition currentSelectionPosition;

		public void CellPointerDown(Cell cell) {
			_selecting = true;
			SelectCell(cell, cell.position);
		}

		public void SelectCell(Cell cell, CellPosition position) {
			if (currentSelectedCell != null) {
				currentSelectedCell.SelectableComponent.OnDeselect(null);
			}
			if (cell == null) {
				cell = GetCellUi(position);
			}
			currentSelectedCell = cell;
			currentSelectionPosition = position;
			if (cell != null) {
				cell.Selected = true;
				if (cell.position != currentSelectionPosition) {
					throw new System.Exception("cell and position expected to match!");
				}
				if (cell.SelectableComponent != null) {
					cell.SelectableComponent.OnSelect(null);
				}
			}
			currentCellSelection = new CellRange(currentSelectionPosition);
			UpdateSelection();
		}

		public void CellPointerMove(Cell cell) {
			if (_selecting) {
				currentCellSelection.End = cell.position;
				UpdateSelection();
			}
		}

		private void UpdateSelection() {
			CellRange cellRange = GetVisibleCellRange();
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
			if (currentCellSelection.Area > 1) {
				currentSelectedCell.SelectableComponent.OnPointerUp(FakePointerEventData);
				currentSelectedCell.SelectableComponent.OnDeselect(null);
			}
		}

		public bool IsSelected(CellPosition cellPosition) {
			if (currentCellSelection.IsValid && currentCellSelection.Contains(cellPosition)) {
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
			UpdateSelection();
			_selecting = false;
		}
	}
}
