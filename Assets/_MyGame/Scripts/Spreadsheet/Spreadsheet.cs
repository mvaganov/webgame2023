using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
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
		public ScrollRect ScrollView;
		public Vector2 columnRowHeaderSize = new Vector2(100, 40);
		public Vector2 defaultCellSize = new Vector2(100, 30);
		public Vector2 cellPadding = new Vector2(2, 1);
		public List<Column> columns = new List<Column>();
		public List<Row> rows = new List<Row>();
		public List<CellType> cellTypes = new List<CellType>();
		[ContextMenuItem(nameof(CopySelectionToClipboard), nameof(CopySelectionToClipboard))]
		public List<CellRange> selection = new List<CellRange>();
		private bool _selecting;
		public CellPosition currentCellPosition;
		public CellRange currentCellSelection;
		public List<Cell> cells = new List<Cell>();
		private Cell selectedCell;
		private PointerEventData _fakePointerEventData;
		private int _popupUiIndex;
		private RectTransform _popupUiElement;
		public Color multiSelectColor;

		public RectTransform ContentArea => ScrollView.content;

		public CellRange AllRange => new CellRange(CellPosition.Zero, new CellPosition(rows.Count - 1, columns.Count - 1));

		public abstract System.Array Objects { get; set; }

		public PointerEventData FakePointerEventData => _fakePointerEventData != null ? _fakePointerEventData
			: _fakePointerEventData = new PointerEventData(EventSystem.current);

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

		public RectTransform MakeNewCell(int typeIndex, int row, int column) {
			RectTransform cell = Instantiate(cellTypes[typeIndex].prefab.gameObject).GetComponent<RectTransform>();
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

		public virtual void Initialize() {
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
			string errorMessage = $"Could not set {obj}.name = \"{nameObj}\"";
			Debug.LogError(errorMessage);
			return new Parse.Error(errorMessage);
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
				Row row = rows[i];
				RectTransform cell = MakeNewCell(0, i, -1);
				cell.SetParent(RowHeadersArea);
				cell.anchoredPosition = new Vector2(0, -cursor);
				cell.sizeDelta = new Vector2(columnRowHeaderSize.x, row.height);
				cursor += row.height + cellPadding.y;
				string label = row.label;
				SetText(cell, label);
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
			for(int r = 0; r < rows.Count; ++r) {
				Row row = rows[r];
				cursor.x = 0;
				Cell[] rowLookupTable = row.GetCellLookupTable(true);
				if (row.Cells != rowLookupTable) {
					throw new System.Exception("we have a problem... cells lookup table is not happening?");
				}
				for (int c = 0; c < row.output.Length; ++c) {
					RectTransform cellRect = MakeNewCell(columns[c].cellType, r, c);
					Cell cell = cellRect.GetComponent<Cell>();
					rowLookupTable[c] = cell;
					cellRect.SetParent(ContentArea);
					cellRect.anchoredPosition = cursor;
					cellRect.sizeDelta = new Vector2(columns[c].width, rows[r].height);
					cursor.x += columns[c].width + cellPadding.x;
					SetText(cellRect, row.output[c]);
					cell.AssignSetFunction(columns[c].SetData);
					cellRect.name = row.output[c];
				}
				cursor.y -= row.height + cellPadding.y;
			}
			cursor.y *= -1;
			cursor -= cellPadding;
			ContentArea.sizeDelta = cursor;
		}

		public void RefreshVisibleUi() {
			// TODO calculate range that is visible
			RefreshUi(AllRange);
		}

		public CellRange GetVisibleRange() {
			CellRange range = new CellRange();
			Vector3[] viewportCorners = new Vector3[4];
			Vector3[] contentCorners = new Vector3[4];
			ScrollView.viewport.GetWorldCorners(viewportCorners);
			ScrollView.content.GetWorldCorners(contentCorners);
			float viewportHeight = viewportCorners[1].y - viewportCorners[0].y;
			float viewportWidth = viewportCorners[2].x - viewportCorners[0].x;
			float contentHeight = contentCorners[1].y - contentCorners[0].y;
			float left = contentCorners[0].x - viewportCorners[0].x;
			float right = contentCorners[2].x - viewportCorners[0].x;

			//float top = contentCorners[1].y - viewportCorners[0].y;
			//float bottom = contentCorners[0].y - viewportCorners[0].y;
			// need to slip vertical, since Unity likes 0,0 at the lower left, and we want 0,0 at the top left
			float top = viewportHeight - (contentCorners[1].y - viewportCorners[0].y);
			float bottom = viewportHeight - (contentCorners[0].y - viewportCorners[0].y);
			Vector2 cursor = new Vector2(left, top);
			for(int r = 0; r < rows.Count; ++r) {
				cursor.y += rows[r].height + cellPadding.y;
				if (cursor.y >= 0) {
					range.Start.Row = r;
					break;
				}
			}
			for (int c = 0; c < columns.Count; ++c) {
				cursor.x += columns[c].width + cellPadding.x;
				if (cursor.x >= 0) {
					range.Start.Column = c;
					break;
				}
			}
			range.End = range.Start;
			//Debug.Log($"top {top}, left {left}\n{cursor} vs ({viewportWidth}, {viewportHeight})");
			if (cursor.y < viewportHeight) {
				for (int r = range.Start.Row + 1; r < rows.Count; ++r) {
					cursor.y += rows[r].height + cellPadding.y;
					if (cursor.y >= viewportHeight) {
						range.End.Row = r;
						break;
					}
				}
			}
			if (cursor.x < viewportWidth) {
				for (int c = range.Start.Column + 1; c < columns.Count; ++c) {
					cursor.x += columns[c].width + cellPadding.x;
					if (cursor.x >= viewportWidth) {
						range.End.Column = c;
						break;
					}
				}
			}
			return range;
		}

		public void UpdateUiBasedOnVisibility() {
			CellRange all = AllRange;
			CellRange visible = GetVisibleRange();
			//Debug.Log($"all {all}, visible {visible}");
			// TODO check if the currently visible cells are different from the last rendered cells.
			// TODO if they are, refresh! --that is, queue the cells to refresh.
				// refreshing a cell means
					// - get memory for the UI element
					// - arrange the UI element
					// - queue the element to have data refreshed
		}

		public void RefreshUi(CellRange visibleRange) {
			for (int r = visibleRange.Start.Row; r < visibleRange.End.Row; ++r) {
				Row row = rows[r];
				row.Refresh(this, visibleRange.Start.Column, visibleRange.End.Column);
			}
		}

		public static Object GetTextObject(RectTransform rect) {
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

		public static UnityEvent<string> GetTextSubmitEvent(RectTransform rect) {
			InputField inf = rect.GetComponentInChildren<InputField>();
			if (inf != null) { return inf.onSubmit; }
			TMPro.TMP_InputField tmpinf = rect.GetComponentInChildren<TMPro.TMP_InputField>();
			if (tmpinf != null) { return tmpinf.onSubmit; }
			return null;
		}

		public static void SetText(Object rect, string text) {
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

		public void SetPopup(Cell cell, string text) {
			if (_popupUiElement == null) {
				_popupUiElement = MakeNewCell(_popupUiIndex, -1, -1);
			} else {
				_popupUiElement.SetAsLastSibling();
			}
			RectTransform cellRectTransform = cell.GetComponent<RectTransform>();
			RectTransform popupRectTransform = _popupUiElement.GetComponent<RectTransform>();
			SetText(_popupUiElement, text);
			Vector3[] corners = new Vector3[4];
			cellRectTransform.GetLocalCorners(corners);
			popupRectTransform.anchoredPosition = corners[0];
			_popupUiElement.gameObject.SetActive(true);
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
			selectedCell = cell;
			cell.SelectableComponent.OnSelect(null);
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
			if (currentCellSelection.Area > 1) {
				selectedCell.SelectableComponent.OnPointerUp(FakePointerEventData);
				selectedCell.SelectableComponent.OnDeselect(null);
			}
		}

		public void CellPointerUp(Cell cell) {
			UpdateSelection();
			_selecting = false;
		}

		public void CopySelectionToClipboard() {
			StringBuilder sb = new StringBuilder();
			CellPosition min = CellPosition.Invalid, max = CellPosition.Invalid;
			if (selectedCell != null) {
				min = max = selectedCell.position;
			}
			if (currentCellSelection.IsValid) {
				min = CellPosition.Min(min, currentCellSelection.Min);
				max = CellPosition.Max(max, currentCellSelection.Max);
			}
			for (int i = 0; i < selection.Count; ++i) {
				CellRange csel = selection[i];
				if (csel.IsValid) {
					min = CellPosition.Min(min, csel.Min);
					max = CellPosition.Max(max, csel.Max);
				}
			}
			for(int r = min.Row; r <= max.Row; ++r) {
				if (r == -1) {
					for (int c = min.Column; c <= max.Column; ++c) {
						if (c != min.Column) {
							sb.Append('\t');
						}
						sb.Append(columns[c].label);
					}
					continue;
				}
				Row row = rows[r];
				if (r != min.Row) {
					sb.Append('\n');
				}
				for (int c = min.Column; c <= max.Column; ++c) {
					if (c != min.Column) {
						sb.Append('\t');
					}
					if (c >= 0) {
						sb.Append(row.output[c]);
					} else if (c == -1) {
						sb.Append(row.label);
					}
				}
			}
			GUIUtility.systemCopyBuffer = sb.ToString();
		}
	}
}
