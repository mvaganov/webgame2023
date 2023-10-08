using System.Collections.Generic;
using UnityEngine;

namespace Spreadsheet {
	[System.Serializable]
	public class Row {
		public string label;
		public float height;
		public string[] output;
		[SerializeField] private object _data;
		private Cell[] cells;
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

		public void ClearCellLookupTable() {
			if (cells == null) {
				return;
			}
			for(int i = 0; i < cells.Length; ++i) {
				cells[i] = null;
			}
			s_allocatedCells.Add(cells);
			cells = null;
		}

		public Cell[] GetCellLookupTable(bool createIfNecessary) {
			if (createIfNecessary && cells == null) {
				int index = s_allocatedCells.FindIndex(c => c.Length == output.Length);
				if (index >= 0) {
					cells = s_allocatedCells[index];
					s_allocatedCells.RemoveAt(index);
				} else {
					cells = new Cell[output.Length];
				}
			}
			return cells;
		}

		private static List<Cell[]> s_allocatedCells = new List<Cell[]>();
	}
}
