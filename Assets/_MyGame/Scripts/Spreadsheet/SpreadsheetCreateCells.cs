using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Spreadsheet {
	public partial class Spreadsheet {
		[System.Serializable]
		public class CellType {
			public string name;
			public RectTransform prefab;
		}

		public Vector2 columnRowHeaderSize = new Vector2(100, 40);
		public Vector2 cellPadding = new Vector2(2, 1);
		public List<CellType> cellTypes = new List<CellType>();
		private static List<List<Cell>> s_preallocatedCellsByType = new List<List<Cell>>();
		private List<CellPosition> _toRemoveDuringUpdate = new List<CellPosition>();
		private List<CellPosition> _toAddDuringUpdate = new List<CellPosition>();
		private bool _updatingVisiblity;
		private CellRange? _rangeToUpdateAsap;
		private CellRange _lastRendered = CellRange.Invalid;
		private Vector3[] _viewportCorners = new Vector3[4];
		private Vector3[] _contentCorners = new Vector3[4];

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
			//Debug.Log(visibleRange);
			StartCoroutine(UpdateCells(visibleRange));
		}

		private IEnumerator UpdateCells(CellRange visibleRange) {
			_updatingVisiblity = true;
			PopulateAddAndRemoveLists(visibleRange);
			RemoveCellsInRemoveList();
			//Debug.Log($"old: {_lastRendered}  new: {visibleRange}\n" +
			//	$"newcells: [{string.Join(", ", _toAddDuringUpdate)}]\n" +
			//	$"oldCells: [{string.Join(", ", _toRemoveDuringUpdate)}]");
			yield return null;
			CreateCellsInCreateList();
			RefreshVisibleCells(visibleRange);
			_updatingVisiblity = false;
		}

		private void PopulateAddAndRemoveLists(CellRange visibleRange) {
			_toRemoveDuringUpdate.Clear();
			_toAddDuringUpdate.Clear();
			if (_lastRendered == visibleRange) {
				return;
			}
			CellRange union = visibleRange;
			CellRange intersection = visibleRange;
			union.Union(_lastRendered);
			intersection.Intersection(_lastRendered);
			union.ForEach(cpos => {
				if (intersection.Contains(cpos)) { return; }
				if (_lastRendered.Contains(cpos)) { _toRemoveDuringUpdate.Add(cpos); }
				if (visibleRange.Contains(cpos)) { _toAddDuringUpdate.Add(cpos); }
			});
		}

		private void RemoveCellsInRemoveList() {
			for (int i = 0; i < _toRemoveDuringUpdate.Count; ++i) {
				CellPosition cpos = _toRemoveDuringUpdate[i];
				FreeCellUi(GetCellUi(cpos));
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

		private void CreateCellsInCreateList() {
			for (int i = 0; i < _toAddDuringUpdate.Count; ++i) {
				CellPosition cpos = _toAddDuringUpdate[i];
				Vector2 cursor = GetCellDrawPosition(cpos);
				Cell cell = MakeNewCell(columns[cpos.Column].cellType).Set(this, cpos);
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
			for (int r = 0; r < rows.Count; ++r) {
				cursor.y += rows[r].height + cellPadding.y;
				range.Start.Row = r;
				if (cursor.y >= 0) {
					break;
				}
			}
			for (int c = 0; c < columns.Count; ++c) {
				cursor.x += columns[c].width + cellPadding.x;
				range.Start.Column = c;
				if (cursor.x >= 0) {
					break;
				}
			}
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

		private void RefreshVisibleCells(CellRange visibleRange) {
			for (int r = visibleRange.Start.Row; r <= visibleRange.End.Row; ++r) {
				Row row = rows[r];
				row.Refresh(this, visibleRange.Start.Column, visibleRange.End.Column);
			}
			_lastRendered = visibleRange;
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
			cell.gameObject.SetActive(true);
			return cell;
		}

		public void FreeCellUi(Cell cell) {
			if (cell == null) { return; }
			CellPosition cellPosition = cell.position;
			RectTransform rect = cell.RectTransform;
			Ui.SetText(rect, "");
			if (rect.parent != ContentArea) {
				Debug.LogError($"is {cell} beign double-freed? parented to {rect.parent.name}, not {ContentArea.name}");
			}
			rect.SetParent(_transform);
			cell.gameObject.SetActive(false);
			cell.spreadsheet = null;
			AssignCell(cellPosition, null);
			s_preallocatedCellsByType[cell.CellTypeIndex].Add(cell);
		}

		public void AssignCell(CellPosition cellPosition, Cell cell) {
			if (cellPosition.IsNormalPosition) {
				Cell[] cellUiRow = rows[cellPosition.Row].GetCellLookupTable(true);
				if (cell != null && cellUiRow[cellPosition.Column] != null) {
					Debug.LogError($"set cell @ {cellPosition}, one already here!");
					FreeCellUi(cellUiRow[cellPosition.Column]);
				}
				cellUiRow[cellPosition.Column] = cell;
			} else if (cellPosition.IsEntireColumn) {
				if (cell != null && columns[cellPosition.Column].headerCell != null) {
					Debug.LogError($"set column header @ {cellPosition.Column}, one already here!");
					FreeCellUi(columns[cellPosition.Column].headerCell);
				}
				columns[cellPosition.Column].headerCell = cell;
			} else if (cellPosition.IsEntireRow) {
				if (cell != null && rows[cellPosition.Row].headerCell != null) {
					Debug.LogError($"set row header @ {cellPosition.Row}, one already here!");
					FreeCellUi(rows[cellPosition.Row].headerCell);
				}
				rows[cellPosition.Row].headerCell = cell;
			}
		}

		public void SetupCellTypes() {
			for (int i = 0; i < cellTypes.Count; i++) {
				cellTypes[i].prefab.gameObject.SetActive(false);
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
				Cell cell = MakeNewCell(1).Set(this, cpos);
				RectTransform rect = cell.RectTransform;
				rect.SetParent(ColumnHeadersArea);
				rect.anchoredPosition = new Vector2(cursor, 0);
				rect.sizeDelta = new Vector2(columns[i].width, columnRowHeaderSize.y);
				cursor += columns[i].width + cellPadding.x;
				Ui.SetText(rect, columns[i].label);
				cell.name = columns[i].label;
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
				Cell cell = MakeNewCell(0).Set(this, cpos);
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
			cells.Clear();
			for (int r = 0; r < rows.Count; ++r) {
				Row row = rows[r];
				cursor.x = 0;
				Cell[] rowLookupTable = row.GetCellLookupTable(true);
				if (row.Cells != rowLookupTable) {
					throw new System.Exception("we have a problem... cells lookup table is not happening?");
				}
				for (int c = 0; c < row.output.Length; ++c) {
					CellPosition cpos = new CellPosition(r, c);
					Cell cell = MakeNewCell(columns[c].cellType).Set(this, cpos);
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
