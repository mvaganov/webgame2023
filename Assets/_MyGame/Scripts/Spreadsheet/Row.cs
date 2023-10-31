using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Spreadsheet {
	[System.Serializable]
	public class Row {
		/// <summary>
		/// Previously allocated memory used to store cell rows
		/// </summary>
		private static List<Cell[]> s_allocatedCells = new List<Cell[]>();

		public string label;
		public Cell headerCell;
		public Func<string, Parse.Error> setHeader;

		public float yPosition;
		public float height;
		public string[] output;
		[SerializeField] private object _data;
		private Cell[] cells;

		public object data { get => _data; set => _data = value; }

		public Cell[] Cells => cells;

		public Row(object data, string label, float height) {
			this._data = data;
			this.label = label;
			this.height = height;
		}
		public void Render(IList<Column> columns, int min = 0, int maxInclusive = -1) {
			if (maxInclusive < 0) {
				maxInclusive = columns.Count - 1;
			}
			if (output == null) {
				output = new string[columns.Count];
			} else if (output.Length < columns.Count) {
				Array.Resize(ref output, columns.Count);
			}
			for (int i = min; i <= maxInclusive; i++) {
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
			for (int i = 0; i < cells.Length; ++i) {
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

		public void Refresh(Spreadsheet sheet, int minColumn = 0, int maxColumnInclusive = -1) {
			if (maxColumnInclusive < 0) {
				maxColumnInclusive = cells.Length - 1;
			}
			Render(sheet.columns, minColumn, maxColumnInclusive);
			if (cells == null || cells.Length == 0) { return; }
			for (int i = minColumn; i <= maxColumnInclusive; ++i) {
				if (cells[i] == null) {
					continue;
				}
				Ui.SetText(cells[i], output[i]);
			}
		}

		public void AssignHeaderSetFunction() {
			UnityEvent<string> submitEvent = Ui.GetTextSubmitEvent(headerCell.GetComponent<RectTransform>());
			if (submitEvent == null) { return; }
			submitEvent.RemoveAllListeners();
			submitEvent.AddListener(SetLabel);
		}
		public void SetLabel(string value) => label = value;
	}
}
