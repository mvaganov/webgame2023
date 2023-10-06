using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MyGame {
	public abstract class Spreadsheet : MonoBehaviour {
		//public RectTransform prefab_defaultCell;
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

		public abstract System.Array Objects { get; set; }

		public void SetObjects<T>(List<T> _objects, System.Array value) {
			_objects.Clear();
			_objects.Capacity = value.Length;
			for (int i = 0; i < value.Length; i++) {
				T obj = (T)value.GetValue(i);
				_objects.Add(obj);
			}
		}

		public RectTransform MakeNewCell(string type) {
			int index = cellTypes.FindIndex(ct => ct.name == type);
			if (index >= 0) {
				return MakeNewCell(index);
			}
			return null;
		}

		public RectTransform MakeNewCell(int index) {
			RectTransform cell = Instantiate(cellTypes[index].prefab.gameObject).GetComponent<RectTransform>();
			cell.gameObject.SetActive(true);
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

		[System.Serializable]
		public class Column {
			public string label;
			public float width;
			private System.Func<object, object> getData;
			private System.Func<object, object, Parse.Error> setData;
			public System.Func<object, object> GetData { get => getData; set => getData = value; }
			public System.Func<object, object, Parse.Error> SetData { get => setData; set => setData = value; }
		}

		[System.Serializable]
		public class Row {
			public string label;
			public float height;
			public string[] output;
			[SerializeField] private object _data;
			public object data { get => _data; set => _data = value; }
			public Row(object data, string label, float height) {
				this._data = data;
				this.label = label;
				this.height = height;
			}
			public void Render(IList<Column> columns) {
				output = new string[columns.Count];
				for (int i = 0; i < columns.Count; i++) {
					object result = columns[i].GetData.Invoke(data);
					if (result != null) {
						output[i] = result.ToString();
					}
				}
			}
		}

		[System.Serializable]
		public class CellType {
			public string name;
			public RectTransform prefab;
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
			ClearCells(RowHeadersArea);
		}

		public void GenerateColumnHeaders() {
			ClearColumnHeaders();
			float cursor = 0;
			for(int i = 0; i < columns.Count; ++i) {
				RectTransform cell = MakeNewCell(0);
				cell.SetParent(ColumnHeadersArea);
				cell.anchoredPosition = new Vector2(cursor, 0);
				cell.sizeDelta = new Vector2(columns[i].width, columnRowHeaderSize.y);
				cursor += columns[i].width + cellPadding.x;
				SetText(cell, columns[i].label);
			}
		}

		public void GenerateRowHeaders() {
			ClearRowHeaders();
			float cursor = 0;
			for (int i = 0; i < rows.Count; ++i) {
				RectTransform cell = MakeNewCell(0);
				cell.SetParent(RowHeadersArea);
				cell.anchoredPosition = new Vector2(0, cursor);
				cell.sizeDelta = new Vector2(columnRowHeaderSize.x, rows[i].height);
				cursor -= rows[i].height + cellPadding.y;
				SetText(cell, rows[i].label);
			}
		}

		public void GenerateCells() {
			Vector2 cursor = Vector2.zero;
			for(int r = 0; r < rows.Count; ++r) {
				Row row = rows[r];
				cursor.x = 0;
				for(int c = 0; c < row.output.Length; ++c) {
					RectTransform cell = MakeNewCell(0);
					cell.SetParent(ContentArea);
					cell.anchoredPosition = cursor;
					cell.sizeDelta = new Vector2(columns[c].width, rows[r].height);
					cursor.x += columns[c].width + cellPadding.x;
					SetText(cell, row.output[c]);
				}
				cursor.y -= row.height + cellPadding.y;
			}
			Debug.Log(cursor);
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
			if (scroll.x == 0) {
				return;
			}
			Vector2 position = ColumnHeadersArea.anchoredPosition;
			position.x = ContentArea.anchoredPosition.x;
			ColumnHeadersArea.anchoredPosition = position;
		}

		public void AdjustRowHeaders(Vector2 scroll) {
			if (scroll.y == 0) {
				return;
			}
			Vector2 position = RowHeadersArea.anchoredPosition;
			position.y = ContentArea.anchoredPosition.y;
			RowHeadersArea.anchoredPosition = position;
		}
	}
}
