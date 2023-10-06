using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MyGame {
	public abstract class Spreadsheet : MonoBehaviour {
		public RectTransform prefab_defaultCell;
		[ContextMenuItem(nameof(GenerateColumnHeaders),nameof(GenerateColumnHeaders))]
		public RectTransform ColumnHeadersArea;
		[ContextMenuItem(nameof(GenerateRowHeaders), nameof(GenerateRowHeaders))]
		public RectTransform RowHeadersArea;
		public Vector2 columnRowHeaderSize = new Vector2(100, 40);
		public Vector2 defaultCellSize = new Vector2(100, 30);
		public Vector2 cellPadding = new Vector2(2, 1);
		public List<Column> columns = new List<Column>();
		public List<Row> rows = new List<Row>();

		public abstract System.Array Objects { get; set; }

		public void SetObjects<T>(List<T> _objects, System.Array value) {
			_objects.Clear();
			_objects.Capacity = value.Length;
			for (int i = 0; i < value.Length; i++) {
				T obj = (T)value.GetValue(i);
				_objects.Add(obj);
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
				RectTransform cell = Instantiate(prefab_defaultCell.gameObject).GetComponent<RectTransform>();
				cell.SetParent(ColumnHeadersArea);
				cell.anchoredPosition = new Vector2(cursor, 0);
				cursor += columns[i].width + cellPadding.x;
				SetText(cell, columns[i].label);
			}
		}

		public Object GetTextObject(RectTransform rect) {
			InputField inf = rect.GetComponent<InputField>();
			if (inf != null) { return inf; }
			return null;
		}

		public void SetText(Object rect, string text) {
			switch (rect) {
				case RectTransform rt:
					SetText(GetTextObject(rt), text);
					break;
			}
		}

		public void GenerateRowHeaders() {
			ClearRowHeaders();
		}
	}
}
