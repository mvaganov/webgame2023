using System.Collections.Generic;
using UnityEngine;

namespace MyGame {
	public abstract class Spreadsheet : MonoBehaviour {
		public Vector2 columnRowHeaderSize = new Vector2(100, 40);
		public GameObject prefab_defaultCell;
		public Vector2 defaultCellSize = new Vector2(100, 30);
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
					string name = GetName(data);
					row = new Row(data, name, defaultCellSize.y);
					rows.Add(row);
				} else {
					row = rows[i];
				}
				row.Render(columns);
			}
		}

		public string GetName(object obj) {
			switch (obj) {
				case Object o: return o.name;
				case null: return null;
			}
			return obj.ToString();
		}

		[System.Serializable]
		public class Column {
			public string label;
			public float width;
			private System.Func<object, object> getData;
			public System.Func<object, object> GetData {
				get => getData;
				set => getData = value;
			}
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
	}
}
