using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spreadsheet {
	public partial class Spreadsheet {
		public Vector2 columnRowHeaderSize = new Vector2(100, 40);
		public Vector2 cellPadding = new Vector2(2, 1);
		private List<CellPosition> _removeDuringUpdate = new List<CellPosition>();
		private List<CellPosition> _addDuringUpdate = new List<CellPosition>();
		private int _updatingVisiblity;
		private CellRange? _rangeToUpdateAsap;
		private CellRange _lastRendered = CellRange.Invalid;
		//private Vector3[] _viewportCorners = new Vector3[4];
		//private Vector3[] _contentCorners = new Vector3[4];
		private bool _refreshRowPositions = true, _refreshColumnPositions = true;
		[SerializeField] private bool _showRowHeaders = true, _showColumnHeaders = true;
		private static bool _beNoisyAboutWeirdCornercaseRefreshBehaviorWhenScrollingFast = false;
		/// <summary>
		/// Cached cell range. prevents O(Log(N)) algorithm to find where a cell range is
		/// </summary>
		private CellRange _currentCellRange;
		/// <summary>
		/// Source data used to calculate of cached <see cref="_currentCellRange"/> is invalidated
		/// </summary>
		private Vector2 _cellVisibleStart, _cellVisibleEnd;

		/// <summary>
		/// Refreshes cells if <see cref="RefreshCells(CellRange)"/> was called while cells were refreshing
		/// </summary>
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
			StartCoroutine(RefreshCellsCoroutine(visibleRange));
		}

		private IEnumerator RefreshCellsCoroutine(CellRange visibleRange) {
			++_updatingVisiblity;
			if (_updatingVisiblity > 1) {
				Debug.LogWarning("we've done it again...");
			}
			MarkWhichCellsChangedVisibility(visibleRange, _removeDuringUpdate, _addDuringUpdate);
			RemoveLostCells(_removeDuringUpdate);
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
		}

		private void MarkRowHeaders(int start, int endInclusive, List<CellPosition> mark, bool skipNull) {
			for (int r = start; r <= endInclusive; ++r) {
				if ((rows[r].headerCell == null) == skipNull) { continue; }
				mark.Add(new CellPosition(r, -1));
			}
		}

		private void MarkColumnHeaders(int start, int endInclusive, List<CellPosition> mark, bool skipNull) {
			for (int c = start; c <= endInclusive; ++c) {
				if ((columns[c].headerCell == null) == skipNull) { continue; }
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

		private void CreateNormalCell(CellPosition cpos) {
			Cell cell = GetCellUi(cpos);
			if (cell != null) {
				if (cell.position != cpos) {
					Debug.LogError($"invalid cell position at cell {cpos}");
				}
				cell.Set(this, cpos);
			} else {
				cell = cellGenerator.MakeNewCell(columns[cpos.Column].cellType).Set(this, cpos);
			}
			PlaceCell(cell, GetCellDrawPosition(cpos));
		}

		public Vector2 GetCellDrawPosition(CellPosition cellPosition) {
			RefreshCellPositionLookupTable();
			float x = cellPosition.Column >= 0 ? columns[cellPosition.Column].xPosition : 0;
			float y = cellPosition.Row >= 0 ? -rows[cellPosition.Row].yPosition : 0;
			return new Vector2(x, y);
		}

		private RectTransform PlaceCell(Cell cell, Vector3 cursor) {
			RectTransform rect = cell.RectTransform;
			rect.SetParent(ContentArea);
			rect.anchoredPosition = cursor;
			int r = cell.position.Row;
			int c = cell.position.Column;
			rect.sizeDelta = new Vector2(columns[c].width, rows[r].height);
			cell.spreadsheet = this;
			cell.AssignSetFunction(columns[c].SetData);
			rect.name = cell.position.ToString();
			return rect;
		}

		public CellRange GetVisibleCellRange() {
			RefreshCellPositionLookupTable();
			Vector2 start = ScrollView.content.anchoredPosition;
			start.x *= -1;
			Vector2 end = start + ScrollView.viewport.rect.size;
			if (start == _cellVisibleStart && end == _cellVisibleEnd) {
				return _currentCellRange;
			}
			_currentCellRange.Start.Row = BinarySearchLookupTable(rows, start.y, r => r.yPosition);
			_currentCellRange.Start.Column = BinarySearchLookupTable(columns, start.x, c => c.xPosition);
			_currentCellRange.End.Row = BinarySearchLookupTable(rows, end.y, r => r.yPosition);
			_currentCellRange.End.Column = BinarySearchLookupTable(columns, end.x, c => c.xPosition);
			_currentCellRange.ExcludeToIntersection(AllRange);
			start = _cellVisibleStart;
			end = _cellVisibleEnd;
			return _currentCellRange;
		}

		private void RefreshCellPositionLookupTable() {
			if (_refreshRowPositions) {
				CalculateRowPositions();
				_refreshRowPositions = false;
			}
			if (_refreshColumnPositions) {
				CalculateColumnPositions();
				_refreshColumnPositions = false;
			}
			Vector2 sizeDelta = ContentArea.sizeDelta;
			Row lastRow = rows[rows.Count - 1];
			Column lastColumn = columns[columns.Count - 1];
			sizeDelta.y = lastRow.yPosition + lastRow.height;
			sizeDelta.x = lastColumn.xPosition + lastColumn.width;
			ContentArea.sizeDelta = sizeDelta;
		}

		private void CalculateRowPositions() {
			float cursor = 0;
			for (int r = 0; r < rows.Count; ++r) {
				rows[r].yPosition = cursor;
				cursor += rows[r].height + cellPadding.y;
			}
		}

		private void CalculateColumnPositions() {
			float cursor = 0;
			for (int c = 0; c < columns.Count; ++c) {
				columns[c].xPosition = cursor;
				cursor += columns[c].width + cellPadding.x;
			}
		}

		/// <summary>
		/// Used to find the best row/column index for the given x/y value
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="value"></param>
		/// <param name="getNum"></param>
		/// <returns></returns>
		private static int BinarySearchLookupTable<T>(IList<T> list, float value, System.Func<T, float> getNum) {
			int left = 0, right = list.Count - 1;
			while (left <= right) {
				int middle = (left + right) / 2;
				int comparison = getNum.Invoke(list[middle]).CompareTo(value);
				if (comparison == 0) {
					return middle;
				} else if (comparison < 0) {
					left = middle + 1;
				} else {
					right = middle - 1;
				}
			}
			return left - 1;
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

		public void InitializeColumnHeaders() {
			ClearColumnHeaders();
			for (int i = 0; i < columns.Count; ++i) {
				CreateColumnHeader(i);
			}
		}

		public void InitializeRowHeaders() {
			ClearRowHeaders();
			for (int i = 0; i < rows.Count; ++i) {
				CreateRowHeader(i);
			}
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
			rect.sizeDelta = new Vector2(column.width, columnRowHeaderSize.y);
			string label = column.label;
			Ui.SetText(rect, label);
			cell.name = label;
			column.headerCell = cell;
		}

		private void FreeRowHeader(int i) {
			cellGenerator.FreeCellUi(rows[i].headerCell);
			rows[i].headerCell = null;
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
			rect.sizeDelta = new Vector2(columnRowHeaderSize.x, row.height);
			string label = row.label;
			Ui.SetText(rect, label);
			cell.name = label;
			row.headerCell = cell.GetComponent<Cell>();
			row.AssignHeaderSetFunction();
		}

		//public void GenerateCells() {
		//	Vector2 cursor = Vector2.zero;
		//	for (int r = 0; r < rows.Count; ++r) {
		//		Row row = rows[r];
		//		cursor.x = 0;
		//		Cell[] rowLookupTable = row.GetCellLookupTable(true);
		//		if (row.Cells != rowLookupTable) {
		//			throw new System.Exception("we have a problem... cells lookup table is not happening?");
		//		}
		//		for (int c = 0; c < row.output.Length; ++c) {
		//			CellPosition cpos = new CellPosition(r, c);
		//			Cell cell = cellGenerator.MakeNewCell(columns[c].cellType).Set(this, cpos);
		//			rowLookupTable[c] = cell;
		//			RectTransform rect = PlaceCell(cell, cursor);
		//			Ui.SetText(rect, row.output[c]);
		//			cursor.x += columns[c].width + cellPadding.x;
		//		}
		//		cursor.y -= row.height + cellPadding.y;
		//	}
		//	cursor.y *= -1;
		//	cursor -= cellPadding;
		//	ContentArea.sizeDelta = cursor;
		//	_lastRendered = AllRange;
		//}
	}
}
