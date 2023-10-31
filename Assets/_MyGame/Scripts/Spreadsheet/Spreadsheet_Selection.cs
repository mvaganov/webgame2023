using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spreadsheet {
	public partial class Spreadsheet {
		[ContextMenuItem(nameof(CopySelectionToClipboard), nameof(CopySelectionToClipboard))]
		public List<CellRange> selection = new List<CellRange>();
		private bool _selecting;
		private CellPosition currentCellPosition = CellPosition.Invalid;
		private CellRange currentCellSelection = CellRange.Invalid;
		private Cell selectedCell;

		public void CellPointerDown(Cell cell) {
			_selecting = true;
			cell.Selected = true;
			selectedCell = cell;
			if (cell != null && cell.SelectableComponent != null) {
				cell.SelectableComponent.OnSelect(null);
			}
			currentCellSelection = new CellRange(cell.position);
			UpdateSelection();
		}

		public void CellPointerMove(Cell cell) {
			if (_selecting) {
				currentCellSelection.End = cell.position;
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
			if (currentCellSelection.Area > 1) {
				selectedCell.SelectableComponent.OnPointerUp(FakePointerEventData);
				selectedCell.SelectableComponent.OnDeselect(null);
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
