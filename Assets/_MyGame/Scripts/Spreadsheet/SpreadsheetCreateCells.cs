using System.Collections.Generic;
using UnityEngine;

namespace Spreadsheet {
	public partial class Spreadsheet {
		public List<CellType> cellTypes = new List<CellType>();
		private static List<List<Cell>> s_preallocatedCellsByType = new List<List<Cell>>();


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
