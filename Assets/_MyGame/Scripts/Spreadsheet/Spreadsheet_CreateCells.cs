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
		private Vector3[] _viewportCorners = new Vector3[4];
		private Vector3[] _contentCorners = new Vector3[4];
		private bool _refreshRowPositions = true, _refreshColumnPositions = true;

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
			MarkWhichCellsChangedVisibility(visibleRange);
			RemoveLostCells();
			//Debug.Log($"old: {_lastRendered}  new: {visibleRange}\n" +
			//	$"newcells: [{string.Join(", ", _toAddDuringUpdate)}]\n" +
			//	$"oldCells: [{string.Join(", ", _toRemoveDuringUpdate)}]");
			yield return null;
			CreateNewCells();
			RefreshVisibleCells(visibleRange);
			--_updatingVisiblity;
		}

		private void MarkWhichCellsChangedVisibility(CellRange visibleRange) {
			_removeDuringUpdate.Clear();
			_addDuringUpdate.Clear();
			if (_lastRendered == visibleRange) {
				return;
			}
			//PredictOneMoreCell(ref visibleRange);
			CellRange union = visibleRange;
			CellRange intersection = visibleRange;
			union.Union(_lastRendered);
			intersection.Intersection(_lastRendered);
			union.ForEach(cpos => {
				if (intersection.Contains(cpos)) { return; }
				if (_lastRendered.Contains(cpos)) { _removeDuringUpdate.Add(cpos); }
				if (visibleRange.Contains(cpos)) { _addDuringUpdate.Add(cpos); }
			});
		}

		/// <summary>
		/// TODO finish implementing this method, to add extra cells to be displayed
		/// </summary>
		/// <param name="visibleRange"></param>
		private void PredictOneMoreCell(ref CellRange visibleRange) {
			CellPosition deltaStart = visibleRange.Start - _lastRendered.Start;
			CellPosition deltaEnd = visibleRange.End - _lastRendered.End;
			CellPosition delta = deltaStart + deltaEnd;
			if (System.Math.Abs(delta.Row) < visibleRange.Height / 2) {
				if (delta.Row < 0) { visibleRange.Start.Row += delta.Row; }
				if (delta.Row > 0) { visibleRange.End.Row += delta.Row; }
			}
			if (System.Math.Abs(delta.Column) < visibleRange.Width / 2) {
				if (delta.Column < 0) { visibleRange.Start.Column += delta.Column; }
				if (delta.Column > 0) { visibleRange.End.Row += delta.Column; }
			}
			visibleRange.Intersection(AllRange);
		}

		private void RemoveLostCells() {
			for (int i = 0; i < _removeDuringUpdate.Count; ++i) {
				CellPosition cpos = _removeDuringUpdate[i];
				cellGenerator.FreeCellUi(GetCellUi(cpos));
			}
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

		private void CreateNewCells() {
			for (int i = 0; i < _addDuringUpdate.Count; ++i) {
				CellPosition cpos = _addDuringUpdate[i];
				//Vector2 cursor = GetCellDrawPosition(cpos);
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
			CellRange range = new CellRange();
			RefreshCellPositionLookupTable();
			Vector2 start = ScrollView.content.anchoredPosition;
			start.x *= -1;
			Vector2 end = start + ScrollView.viewport.rect.size;
			range.Start.Row = BinarySearchLookupTable(rows, start.y, r => r.yPosition);
			range.Start.Column = BinarySearchLookupTable(columns, start.x, c => c.xPosition);
			range.End.Row = BinarySearchLookupTable(rows, end.y, r => r.yPosition);
			range.End.Column = BinarySearchLookupTable(columns, end.x, c => c.xPosition);
			range.Intersection(AllRange);
			return range;
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
			_lastRendered = visibleRange;
		}

		public void AssignCell(CellPosition cellPosition, Cell cell) {
			Cell cellToFree = null;
			if (cellPosition.IsNormalPosition) {
				Cell[] cellUiRow = rows[cellPosition.Row].GetCellLookupTable(true);
				if (cell != null && cellUiRow[cellPosition.Column] != null) {
					Debug.LogError($"set cell @ {cellPosition}, one already here!");
					cellToFree = cellUiRow[cellPosition.Column];
				}
				cellUiRow[cellPosition.Column] = cell;
			} else if (cellPosition.IsEntireColumn) {
				if (cell != null && columns[cellPosition.Column].headerCell != null) {
					Debug.LogError($"set column header @ {cellPosition.Column}, one already here!");
					cellToFree = columns[cellPosition.Column].headerCell;
				}
				columns[cellPosition.Column].headerCell = cell;
			} else if (cellPosition.IsEntireRow) {
				if (cell != null && rows[cellPosition.Row].headerCell != null) {
					Debug.LogError($"set row header @ {cellPosition.Row}, one already here!");
					cellToFree = rows[cellPosition.Row].headerCell;
				}
				rows[cellPosition.Row].headerCell = cell;
			}
			if (cellToFree != null) {
				if (cellToFree == cell && cell != null) {
					Debug.Log("well this is strange. attempting to re assign the same cell to the same spot?");
					cell.gameObject.SetActive(true);
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

		public void GenerateColumnHeaders() {
			ClearColumnHeaders();
			// TODO use GetCellDrawPosition(cpos) instead of calculating the cursor value manually in the loop.
			float cursor = 0;
			for (int i = 0; i < columns.Count; ++i) {
				CellPosition cpos = new CellPosition(-1, i);
				Cell cell = cellGenerator.MakeNewCell(1).Set(this, cpos);
				RectTransform rect = cell.RectTransform;
				rect.SetParent(ColumnHeadersArea);
				rect.anchoredPosition = new Vector2(cursor, 0);
				rect.sizeDelta = new Vector2(columns[i].width, columnRowHeaderSize.y);
				cursor += columns[i].width + cellPadding.x;
				string label = columns[i].label;
				Ui.SetText(rect, label);
				cell.name = label;
			}
			cursor -= cellPadding.x;
			ColumnHeadersArea.sizeDelta = new Vector2(cursor, columnRowHeaderSize.y);
		}

		public void GenerateRowHeaders() {
			ClearRowHeaders();
			float cursor = 0;
			for (int i = 0; i < rows.Count; ++i) {
				Row row = rows[i];
				CellPosition cpos = new CellPosition(i, -1);
				Cell cell = cellGenerator.MakeNewCell(0).Set(this, cpos);
				RectTransform rect = cell.RectTransform;
				rect.SetParent(RowHeadersArea);
				rect.anchoredPosition = new Vector2(0, -cursor);
				rect.sizeDelta = new Vector2(columnRowHeaderSize.x, row.height);
				cursor += row.height + cellPadding.y;
				string label = row.label;
				Ui.SetText(rect, label);
				cell.name = label;
				row.headerCell = cell.GetComponent<Cell>();
				row.AssignHeaderSetFunction();
			}
			cursor -= cellPadding.y;
			RowHeadersArea.sizeDelta = new Vector2(columnRowHeaderSize.x, cursor);
		}

		public void GenerateCells() {
			Vector2 cursor = Vector2.zero;
			for (int r = 0; r < rows.Count; ++r) {
				Row row = rows[r];
				cursor.x = 0;
				Cell[] rowLookupTable = row.GetCellLookupTable(true);
				if (row.Cells != rowLookupTable) {
					throw new System.Exception("we have a problem... cells lookup table is not happening?");
				}
				for (int c = 0; c < row.output.Length; ++c) {
					CellPosition cpos = new CellPosition(r, c);
					Cell cell = cellGenerator.MakeNewCell(columns[c].cellType).Set(this, cpos);
					rowLookupTable[c] = cell;
					RectTransform rect = PlaceCell(cell, cursor);
					Ui.SetText(rect, row.output[c]);
					cursor.x += columns[c].width + cellPadding.x;
				}
				cursor.y -= row.height + cellPadding.y;
			}
			cursor.y *= -1;
			cursor -= cellPadding;
			ContentArea.sizeDelta = cursor;
			_lastRendered = AllRange;
		}
	}
}
