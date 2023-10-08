using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Spreadsheet {
	public abstract class Spreadsheet : MonoBehaviour {
		[System.Serializable]
		public class CellType {
			public string name;
			public RectTransform prefab;
		}

		[ContextMenuItem(nameof(GenerateColumnHeaders),nameof(GenerateColumnHeaders))]
		public RectTransform ColumnHeadersArea;
		[ContextMenuItem(nameof(GenerateRowHeaders), nameof(GenerateRowHeaders))]
		public RectTransform RowHeadersArea;
		public RectTransform ContentArea;
		public Vector2 columnRowHeaderSize = new Vector2(100, 40);
		public Vector2 defaultCellSize = new Vector2(100, 30);
		public Vector2 cellPadding = new Vector2(2, 1);
		public List<Column> columns = new List<Column>();
		public List<Row> rows = new List<Row>();
		public List<CellType> cellTypes = new List<CellType>();
		public List<CellSelection> selection = new List<CellSelection>();
		private bool _selecting;
		public CellPosition currentCellPosition;
		public CellSelection currentCellSelection;
		public List<Cell> cells = new List<Cell>();

		public ColorBlock colorBlock;

		public abstract System.Array Objects { get; set; }

		//public void Select(int row, int column) {
		//	selection.Clear();
		//	selection.Add(new CellSelection(row, column));
		//}

		//public void ToggleSelection(int row, int column, bool toggle) {
		//	CellSelection selected = new CellSelection(row, column);
		//	int index = selection.IndexOf(selected);
		//	if (index < 0) {
		//		selection.Add(selected);
		//	} else {
		//		selection.RemoveAt(index);
		//	}
		//}

		//public void AddSelection(int row, int column) {
		//	CellPosition position = new CellPosition(row, column);
		//	AddSelection(position);
		//}

		//public void AddSelection(CellPosition position) {
		//	Debug.Log("adding " + position);
		//	CellSelection selected = new CellSelection(position);
		//	int index = selection.IndexOf(selected);
		//	if (index < 0) {
		//		selection.Add(selected);
		//	}
		//}

		//public void RemoveSelection(int row, int column) {
		//	CellSelection selected = new CellSelection(row, column);
		//	int index = selection.IndexOf(selected);
		//	if (index >= 0) {
		//		selection.RemoveAt(index);
		//	}
		//}

		public void SetObjects<T>(List<T> _objects, System.Array value) {
			_objects.Clear();
			_objects.Capacity = value.Length;
			for (int i = 0; i < value.Length; i++) {
				T obj = (T)value.GetValue(i);
				_objects.Add(obj);
			}
		}

		public RectTransform MakeNewCell(string type, int row, int column) {
			int index = cellTypes.FindIndex(ct => ct.name == type);
			if (index >= 0) {
				return MakeNewCell(index, row, column);
			}
			return null;
		}

		public RectTransform MakeNewCell(int index, int row, int column) {
			RectTransform cell = Instantiate(cellTypes[index].prefab.gameObject).GetComponent<RectTransform>();
			cell.gameObject.SetActive(true);
			Cell.Set(cell.gameObject, this, row, column);
			return cell;
		}

		public void SetupCellTypes() {
			for (int i = 0; i < cellTypes.Count; i++) {
				cellTypes[i].prefab.gameObject.SetActive(false);
			}
		}

		public System.Array GetObjects<T>(List<T> _objects, ref System.Array _value) {
			return _value != null ? _value : _value = _objects.ToArray();
		}

		public virtual void Refresh() {
			rows.Clear();
			int count = Mathf.Max(Objects.Length, rows.Count);
			if (Objects.Length < count) {
				System.Array arr = new object[count];
				for (int i = 0; i < Objects.Length; i++) {
					arr.SetValue(Objects.GetValue(i), i);
				}
				Objects = arr;
			}
			for (int i = 0; i < count; i++) {
				object data = Objects.GetValue(i);
				Row row;
				if (rows.Count <= i) {
					string name = GetName(data).ToString();
					row = new Row(data, name, defaultCellSize.y);
					rows.Add(row);
				} else {
					row = rows[i];
				}
				row.Render(columns);
			}
		}

		public object GetName(object obj) {
			switch (obj) {
				case Object o: return o.name;
				case null: return null;
			}
			return obj.ToString();
		}

		public Parse.Error SetName(object obj, object nameObj) {
			string name = nameObj.ToString();
			switch (obj) {
				case Object o: o.name = name; return null;
			}
			return new Parse.Error($"Could not set {obj}.name = {nameObj}");
		}

		private void DestroyFunction(GameObject go) {
			if (Application.isPlaying) {
				Destroy(go);
			} else {
				DestroyImmediate(go);
			}
		}

		public void ClearCells(RectTransform parent) {
			for(int i = parent.childCount-1; i >= 0; --i) {
				DestroyFunction(parent.GetChild(i).gameObject);
			}
		}

		public void ClearColumnHeaders() {
			ClearCells(ColumnHeadersArea);
		}

		public void ClearRowHeaders() {
			for(int i = 0; i < rows.Count; ++i) {
				rows[i].ClearCellLookupTable();
			}
			ClearCells(RowHeadersArea);
		}

		public void GenerateColumnHeaders() {
			ClearColumnHeaders();
			float cursor = 0;
			for(int i = 0; i < columns.Count; ++i) {
				RectTransform cell = MakeNewCell(1, -1, i);
				cell.SetParent(ColumnHeadersArea);
				cell.anchoredPosition = new Vector2(cursor, 0);
				cell.sizeDelta = new Vector2(columns[i].width, columnRowHeaderSize.y);
				cursor += columns[i].width + cellPadding.x;
				SetText(cell, columns[i].label);
				cell.name = columns[i].label;
			}
			cursor -= cellPadding.x;
			ColumnHeadersArea.sizeDelta = new Vector2(cursor, columnRowHeaderSize.y);
		}

		public void GenerateRowHeaders() {
			ClearRowHeaders();
			float cursor = 0;
			for (int i = 0; i < rows.Count; ++i) {
				RectTransform cell = MakeNewCell(0, i, -1);
				cell.SetParent(RowHeadersArea);
				cell.anchoredPosition = new Vector2(0, -cursor);
				cell.sizeDelta = new Vector2(columnRowHeaderSize.x, rows[i].height);
				cursor += rows[i].height + cellPadding.y;
				SetText(cell, rows[i].label);
				cell.name = rows[i].label;
			}
			cursor -= cellPadding.y;
			RowHeadersArea.sizeDelta = new Vector2(columnRowHeaderSize.x, cursor);
		}

		public void GenerateCells() {
			Vector2 cursor = Vector2.zero;
			cells.Clear();
			for(int r = 0; r < rows.Count; ++r) {
				Row row = rows[r];
				cursor.x = 0;
				Cell[] rowLookupTable = row.GetCellLookupTable(true);
				for (int c = 0; c < row.output.Length; ++c) {
					RectTransform cellRect = MakeNewCell(columns[c].cellType, r, c);
					Cell cell = cellRect.GetComponent<Cell>();
					rowLookupTable[c] = cell;
					cellRect.SetParent(ContentArea);
					cellRect.anchoredPosition = cursor;
					cellRect.sizeDelta = new Vector2(columns[c].width, rows[r].height);
					cursor.x += columns[c].width + cellPadding.x;
					SetText(cellRect, row.output[c]);
					cellRect.name = row.output[c];

				}
				cursor.y -= row.height + cellPadding.y;
			}
			cursor.y *= -1;
			cursor -= cellPadding;
			ContentArea.sizeDelta = cursor;
		}

		public Object GetTextObject(RectTransform rect) {
			InputField inf = rect.GetComponentInChildren<InputField>();
			if (inf != null) { return inf; }
			TMPro.TMP_InputField tmpinf = rect.GetComponentInChildren<TMPro.TMP_InputField>();
			if (tmpinf != null) { return tmpinf; }
			Text txt = rect.GetComponentInChildren<Text>();
			if (txt != null) { return txt; }
			TMPro.TMP_Text tmptxt = rect.GetComponentInChildren<TMPro.TMP_Text>();
			if (tmptxt != null) { return tmptxt; }
			return null;
		}

		public void SetText(Object rect, string text) {
			switch (rect) {
				case null: break;
				case GameObject go: SetText(go.GetComponent<RectTransform>(), text); break;
				case RectTransform rt: SetText(GetTextObject(rt), text); break;
				case InputField inf: inf.text = text; break;
				case Text txt: txt.text = text; break;
				case TMPro.TMP_InputField tmpinf: tmpinf.text = text; break;
				case TMPro.TMP_Text tmptxt: tmptxt.text = text; break;
				case MonoBehaviour mb: SetText(mb.GetComponent<RectTransform>(), text); break;
			}
		}

		public void AdjustColumnHeaders(Vector2 scroll) {
			ColumnHeadersArea.anchoredPosition = new Vector2(ContentArea.anchoredPosition.x, ColumnHeadersArea.anchoredPosition.y);
		}

		public void AdjustRowHeaders(Vector2 scroll) {
			RowHeadersArea.anchoredPosition = new Vector2(RowHeadersArea.anchoredPosition.x, ContentArea.anchoredPosition.y);
		}

		public void CellPointerDown(Cell cell) {
			_selecting = true;
			cell.Selected = true;
			currentCellSelection = new CellSelection(cell.position);
			UpdateSelection();
		}

		public void CellPointerMove(Cell cell) {
			if (_selecting) {
				currentCellSelection.End = cell.position;
				UpdateSelection();
			}
		}

		private void UpdateSelection() {
			for(int r = 0; r < rows.Count; ++r) {
				Row row = rows[r];
				Cell[] cells = row.GetCellLookupTable(false);
				if (cells == null) {
					continue;
				}
				for (int c = 0; c < cells.Length; ++c) {
					Cell cell = cells[c];
					cell.Selected = currentCellSelection.Contains(cell.position);
				}
			}
		}

		public void CellPointerUp(Cell cell) {
			UpdateSelection();
			_selecting = false;
		}
	}
}
