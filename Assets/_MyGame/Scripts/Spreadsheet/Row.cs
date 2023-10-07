using System.Collections.Generic;
using UnityEngine;

namespace Spreadsheet {
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
