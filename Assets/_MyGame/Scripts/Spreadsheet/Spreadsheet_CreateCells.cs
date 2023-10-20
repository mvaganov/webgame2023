using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spreadsheet {
	public partial class Spreadsheet {
		public Vector2 columnRowHeaderSize = new Vector2(100, 40);
		public Vector2 cellPadding = new Vector2(2, 1);
		private List<CellPosition> _removeDuringUpdate = new List<CellPosition>();
		private List<CellPosition> _addDuringUpdate = new List<CellPosition>();
		private bool _updatingVisiblity;
		private CellRange? _rangeToUpdateAsap;
		private CellRange _lastRendered = CellRange.Invalid;
		private Vector3[] _viewportCorners = new Vector3[4];
		private Vector3[] _contentCorners = new Vector3[4];
		private bool _refreshRowPositions = true, _refreshColumnPositions = true;

		/// <summary>
		/// Refreshes cells if <see cref="RefreshCells(CellRange)"/> was called while cells were refreshing
		/// </summary>
		private void UpdateRefreshCells() {
			if (_rangeToUpdateAsap == null || _updatingVisiblity) {
				return;
			}
			RefreshCells(_rangeToUpdateAsap.Value);
		}

		public void RefreshCells(CellRange visibleRange) {
			if (_updatingVisiblity) {
				_rangeToUpdateAsap = visibleRange;
				return;
			}
			_rangeToUpdateAsap = null;
			StartCoroutine(UpdateCells(visibleRange));
		}

		private IEnumerator UpdateCells(CellRange visibleRange) {
			_updatingVisiblity = true;
			MarkWhichCellsChangedVisibility(visibleRange);
			RemoveLostCells();
			//Debug.Log($"old: {_lastRendered}  new: {visibleRange}\n" +
			//	$"newcells: [{string.Join(", ", _toAddDuringUpdate)}]\n" +
			//	$"oldCells: [{string.Join(", ", _toRemoveDuringUpdate)}]");
			yield return null;
			CreateNewCells();
			RefreshVisibleCells(visibleRange);
			_updatingVisiblity = false;
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
				Vector2 cursor = GetCellDrawPosition(cpos);
				Cell cell = cellGenerator.MakeNewCell(columns[cpos.Column].cellType).Set(this, cpos);
				PlaceCell(cell, cursor);
			}
		}

		public Vector2 GetCellDrawPosition(CellPosition cellPosition) {
			Vector2 cursor = Vector2.zero;
			for (int r = 0; r < cellPosition.Row; ++r) {
				cursor.y -= rows[r].height + cellPadding.y;
			}
			for (int c = 0; c < cellPosition.Column; ++c) {
				cursor.x += columns[c].width + cellPadding.x;
			}
			return cursor;
			//return new Vector2(columns[cellPosition.Column].xPosition, rows[cellPosition.Row].yPosition);
		}

		private RectTransform PlaceCell(Cell cell, Vector3 cursor) {
			RectTransform rect = cell.RectTransform;
			rect.SetParent(ContentArea);
			rect.anchoredPosition = cursor;
			int r = cell.position.Row;
			int c = cell.position.Column;
			rect.sizeDelta = new Vector2(columns[c].width, rows[r].height);
			cell.AssignSetFunction(columns[c].SetData);
			rect.name = cell.position.ToString();
			return rect;
		}

		public CellRange GetVisibleCellRange() {
			CellRange range = new CellRange();
			ScrollView.viewport.GetWorldCorners(_viewportCorners);
			ScrollView.content.GetWorldCorners(_contentCorners);
			float viewportHeight = _viewportCorners[1].y - _viewportCorners[0].y;
			float viewportWidth = _viewportCorners[2].x - _viewportCorners[0].x;
			//float contentHeight = contentCorners[1].y - contentCorners[0].y;
			float left = _contentCorners[0].x - _viewportCorners[0].x;
			//float right = contentCorners[2].x - viewportCorners[0].x;
			// need to slip vertical, since Unity likes 0,0 at the lower left, and we want 0,0 at the top left
			float top = viewportHeight - (_contentCorners[1].y - _viewportCorners[0].y);
			//float bottom = viewportHeight - (contentCorners[0].y - viewportCorners[0].y);
			Vector2 cursor = new Vector2(left, top);
			Row row = new Row(null, null, 0);
			row.yPosition = 0;
			if (_refreshRowPositions) {
				CalculateRowPositions();
				_refreshRowPositions = false;
				//float cursory = 0;
				//for (int r = 0; r < rows.Count; ++r) {
				//	rows[r].yPosition = cursory;
				//	cursory += rows[r].height + cellPadding.y;
				//	range.Start.Row = r;
				//	if (cursory >= 0) {
				//		break;
				//	}
				//}
			}
			if (_refreshColumnPositions) {
				CalculateColumnPositions();
				_refreshColumnPositions = false;
				//float cursorx = 0;
				//for (int c = 0; c < columns.Count; ++c) {
				//	columns[c].xPosition = cursorx;
				//	cursorx += columns[c].width + cellPadding.x;
				//	range.Start.Column = c;
				//	if (cursorx >= 0) {
				//		break;
				//	}
				//}
			}
			
			range.Start.Row = BinarySearchRows(rows, 0, r => r.yPosition, true);
			range.Start.Column = BinarySearchRows(columns, 0, c => c.xPosition, true);

			// TODO the binary search for the end of the visible area should work too... when I am less sleepy.
			range.End.Row = BinarySearchRows(rows, viewportHeight, r => r.yPosition, true);
			range.End.Column = BinarySearchRows(columns, viewportWidth, c => c.xPosition, true);

			range.End = range.Start;
			//Debug.Log($"top {top}, left {left}\n{cursor} vs ({viewportWidth}, {viewportHeight})");
			if (cursor.y < viewportHeight) {
				for (int r = range.Start.Row + 1; r < rows.Count; ++r) {
					cursor.y += rows[r].height + cellPadding.y;
					range.End.Row = r;
					if (cursor.y >= viewportHeight) {
						break;
					}
				}
			}
			if (cursor.x < viewportWidth) {
				for (int c = range.Start.Column + 1; c < columns.Count; ++c) {
					cursor.x += columns[c].width + cellPadding.x;
					range.End.Column = c;
					if (cursor.x >= viewportWidth) {
						break;
					}
				}
			}
			//Debug.Log(range);
			return range;
		}

		private void CalculateRowPositions() {
			float cursor = 0;
			for (int r = 0; r < rows.Count; ++r) {
				rows[r].yPosition = cursor;
				cursor += rows[r].height + cellPadding.y;
				//range.Start.Row = r;
				//if (cursor.y >= 0) {
				//	break;
				//}
			}
		}

		private void CalculateColumnPositions() {
			float cursor = 0;
			for (int c = 0; c < columns.Count; ++c) {
				columns[c].xPosition = cursor;
				cursor += columns[c].width + cellPadding.x;
				//range.Start.Column = c;
				//if (cursor.x >= 0) {
				//	break;
				//}
			}
		}

		private static int BinarySearchRows<T>(IList<T> list, float y, System.Func<T, float> getNum, bool lower) {
			int left = 0;
			int right = list.Count - 1;
			while (left <= right) {
				int middle = (left + right) / 2;
				float comparison = getNum(list[middle]);// list[middle].yPosition.CompareTo(y);
				if (comparison == 0) {
					return middle;
				} else if (comparison < 0) {
					left = middle + 1;
				} else {
					right = middle - 1;
				}
			}
			return lower ? left : right;
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
