using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spreadsheet {
	public partial class Spreadsheet {
		private List<CellPosition> _removeDuringUpdate = new List<CellPosition>();
		private List<CellPosition> _addDuringUpdate = new List<CellPosition>();
		private int _updatingVisiblity;
		private CellRange? _rangeToUpdateAsap;
		private CellRange _lastRendered = CellRange.Invalid;
		/// <summary>
		/// Refreshes cells if <see cref="RefreshCells(CellRange)"/> was called while cells were refreshing
		/// </summary>

		private void OnDestroy() {
			RefreshCells(CellRange.Invalid);
		}

		private void UpdateRefreshCells() {
			if (_rangeToUpdateAsap == null || _updatingVisiblity > 0) {
				return;
			}
			RefreshCells(_rangeToUpdateAsap.Value);
		}

		public void RefreshCells(CellRange visibleRange) {
			if (_updatingVisiblity > 0) {
				_rangeToUpdateAsap = visibleRange;
				return;
			}
			_rangeToUpdateAsap = null;
			Debug.Log("refresh");
			StartCoroutine(RefreshCellsCoroutine(visibleRange));
		}

		private IEnumerator RefreshCellsCoroutine(CellRange visibleRange) {
			++_updatingVisiblity;
			if (_updatingVisiblity > 1) {
				Debug.LogWarning("we've done it again...");
			}
			MarkWhichCellsChangedVisibility(visibleRange, _removeDuringUpdate, _addDuringUpdate);
			RemoveLostCells(_removeDuringUpdate);
			if (visibleRange == CellRange.Invalid) {
				yield break;
			}
			yield return null;
			// create new cells after previous cells have probably been freed
			CreateNewCells(_addDuringUpdate);
			RefreshVisibleCells(visibleRange);
			_lastRendered = visibleRange;
			--_updatingVisiblity;
			if (_beNoisyAboutWeirdCornercaseRefreshBehaviorWhenScrollingFast) {
				List<CellPosition> missing = new List<CellPosition>();
				if (MissingVisibleCells(_lastRendered, missing)) {
					Debug.LogError($"Oh no! missing [{string.Join(", ", missing)}]\n" +
					$"newcells: [{string.Join(", ", _addDuringUpdate)}]\n" +
					$"oldCells: [{string.Join(", ", _removeDuringUpdate)}]");
					CreateNewCells(missing);
					RefreshVisibleCells(visibleRange);
				}
			}
		}

		private void MarkWhichCellsChangedVisibility(CellRange visibleRange, List<CellPosition> remove, List<CellPosition> add) {
			remove.Clear();
			add.Clear();
			if (_lastRendered == visibleRange) {
				return;
			}
			CellRange union = CellRange.Union(visibleRange, _lastRendered);
			CellRange intersection = CellRange.Intersection(visibleRange, _lastRendered);
			if (_showRowHeaders) {
				MarkRowHeaders(visibleRange.Start.Row, visibleRange.End.Row, add, false);
				MarkRowHeaders(union.Start.Row, intersection.Start.Row - 1, remove, true);
				MarkRowHeaders(intersection.End.Row + 1, union.End.Row, remove, true);
			}
			if (_showColumnHeaders) {
				MarkColumnHeaders(visibleRange.Start.Column, visibleRange.End.Column, add, false);
				MarkColumnHeaders(union.Start.Column, intersection.Start.Column - 1, remove, true);
				MarkColumnHeaders(intersection.End.Column + 1, union.End.Column, remove, true);
			}
			if (visibleRange != CellRange.Invalid) {
				union.ForEach(cellPosition => {
					// intersection of current and last visible cells may not have beed generated yet: RefreshCells can exit early.
					if (intersection.Contains(cellPosition)) {
						if (GetCellUi(cellPosition) == null) {
							add.Add(cellPosition);
						}
						return;
					}
					if (_lastRendered.Contains(cellPosition)) { remove.Add(cellPosition); }
					if (visibleRange.Contains(cellPosition)) { add.Add(cellPosition); }
				});
			} else {
				union.ForEach(cellPosition => remove.Add(cellPosition));
			}
		}

		private void MarkRowHeaders(int start, int endInclusive, List<CellPosition> mark, bool removing) {
			for (int r = start; r <= endInclusive; ++r) {
				if ((rows[r].headerCell == null) == removing) { continue; }
				mark.Add(new CellPosition(r, -1));
			}
		}

		private void MarkColumnHeaders(int start, int endInclusive, List<CellPosition> mark, bool removing) {
			for (int c = start; c <= endInclusive; ++c) {
				if ((columns[c].headerCell == null) == removing) { continue; }
				mark.Add(new CellPosition(-1, c));
			}
		}

		private bool MissingVisibleCells(CellRange visibleRange, List<CellPosition> missing) {
			missing.Clear();
			visibleRange.ForEach(cpos => {
				if (GetCellUi(cpos) != null) {
					return;
				}
				missing.Add(cpos);
			});
			return missing.Count > 0;
		}

		private void RemoveLostCells(List<CellPosition> toRemove) {
			toRemove.ForEach(cpos => cellGenerator.FreeCellUi(GetCellUi(cpos)));
		}

		public Cell GetCellUi(CellPosition cellPosition) {
			if (cellPosition.IsNormalPosition) {
				Cell[] cellUiRow = rows[cellPosition.Row].GetCellLookupTable(false);
				if (cellUiRow != null) {
					return cellUiRow[cellPosition.Column];
				}
			} else if (cellPosition.IsEntireColumn) {
				return columns[cellPosition.Column].headerCell;
			} else if (cellPosition.IsEntireRow) {
				return rows[cellPosition.Row].headerCell;
			}
			return null;
		}

		private void CreateNewCells(List<CellPosition> toAdd) {
			for (int i = 0; i < toAdd.Count; ++i) {
				CellPosition cpos = toAdd[i];
				if (cpos.IsNormalPosition) {
					CreateNormalCell(cpos);
				} else if (cpos.IsEntireColumn) {
					CreateColumnHeader(cpos.Column);
				} else if (cpos.IsEntireRow) {
					CreateRowHeader(cpos.Row);
				}
			}
		}

		private void CreateNormalCell(CellPosition cellPosition) {
			Cell cell = GetCellUi(cellPosition);
			if (cell != null) {
				if (cell.position != cellPosition) {
					Debug.LogError($"invalid cell position at cell {cellPosition}");
				}
				cell.Set(this, cellPosition);
			} else {
				cell = cellGenerator.MakeNewCell(columns[cellPosition.Column].cellType).Set(this, cellPosition);
			}
			PlaceCell(cell, GetCellDrawPosition(cellPosition));
		}

		private void CreateColumnHeader(int i) {
			RefreshCellPositionLookupTable();
			Column column = columns[i];
			if (column.headerCell != null) { return; }
			CellPosition cpos = new CellPosition(-1, i);
			Cell cell = cellGenerator.MakeNewCell(1).Set(this, cpos);
			RectTransform rect = cell.RectTransform;
			rect.SetParent(ColumnHeadersArea);
			rect.anchoredPosition = new Vector2(column.xPosition, 0);
			rect.sizeDelta = new Vector2(column.width, _columnRowHeaderSize.y);
			string label = column.label;
			Ui.SetText(rect, label);
			cell.name = label;
			column.headerCell = cell;
		}

		private void CreateRowHeader(int i) {
			RefreshCellPositionLookupTable();
			Row row = rows[i];
			if (row.headerCell != null) { return; }
			CellPosition cpos = new CellPosition(i, -1);
			Cell cell = cellGenerator.MakeNewCell(0).Set(this, cpos);
			RectTransform rect = cell.RectTransform;
			rect.SetParent(RowHeadersArea);
			rect.anchoredPosition = new Vector2(0, -row.yPosition);
			rect.sizeDelta = new Vector2(_columnRowHeaderSize.x, row.height);
			string label = row.label;
			Ui.SetText(rect, label);
			cell.name = label;
			row.headerCell = cell.GetComponent<Cell>();
			row.AssignHeaderSetFunction();
		}

		private void RefreshVisibleCells(CellRange visibleRange) {
			for (int r = visibleRange.Start.Row; r <= visibleRange.End.Row; ++r) {
				Row row = rows[r];
				row.Refresh(this, visibleRange.Start.Column, visibleRange.End.Column);
			}
		}

		public void AssignCell(CellPosition cellPosition, Cell cell) {
			Cell cellToFree = null;
			if (cellPosition.IsNormalPosition) {
				Cell[] cellUiRow = rows[cellPosition.Row].GetCellLookupTable(true);
				if (cell != null && cellUiRow[cellPosition.Column] != null) {
					if (_beNoisyAboutWeirdCornercaseRefreshBehaviorWhenScrollingFast) {
						Debug.LogError($"set cell @ {cellPosition}, one already here!");
					}
					cellToFree = cellUiRow[cellPosition.Column];
				}
				cellUiRow[cellPosition.Column] = cell;
			} else if (cellPosition.IsEntireColumn) {
				if (cellPosition.Column < 0 || cellPosition.Column >= columns.Count) {
					Debug.LogError($"bad cell position {cellPosition}");
				}
				if (cell != null && columns[cellPosition.Column].headerCell != null) {
					if (_beNoisyAboutWeirdCornercaseRefreshBehaviorWhenScrollingFast) {
						Debug.LogError($"set column header @ {cellPosition.Column}, one already here!");
					}
					cellToFree = columns[cellPosition.Column].headerCell;
				}
				columns[cellPosition.Column].headerCell = cell;
			} else if (cellPosition.IsEntireRow) {
				if (cell != null && rows[cellPosition.Row].headerCell != null) {
					if (_beNoisyAboutWeirdCornercaseRefreshBehaviorWhenScrollingFast) {
						Debug.LogError($"set row header @ {cellPosition.Row}, one already here!");
					}
					cellToFree = rows[cellPosition.Row].headerCell;
				}
				rows[cellPosition.Row].headerCell = cell;
			} else if (cellPosition.IsInvalid) {
				//Debug.LogWarning("adding non conforming cell");
				_nonConformingCells.Add(cell);
			}
			if (cellToFree != null) {
				if (cellToFree == cell) {
					if (_beNoisyAboutWeirdCornercaseRefreshBehaviorWhenScrollingFast) {
						Debug.Log("well this is strange. attempting to re assign the same cell to the same spot?");
					}
					return;
				}
				cellGenerator.FreeCellUi(cellToFree);
			}
		}

		public void ClearCells(RectTransform parent) {
			for (int i = parent.childCount - 1; i >= 0; --i) {
				DestroyFunction(parent.GetChild(i).gameObject);
			}
		}

		public void ClearColumnHeaders() {
			ClearCells(ColumnHeadersArea);
		}

		public void ClearRowHeaders() {
			for (int i = 0; i < rows.Count; ++i) {
				rows[i].ClearCellLookupTable();
			}
			ClearCells(RowHeadersArea);
		}
	}
}
