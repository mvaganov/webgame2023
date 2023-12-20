using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Spreadsheet {
	public partial class Spreadsheet {
		public void PasteClipboardIntoSelection() {
			string clipboard = GUIUtility.systemCopyBuffer;
			string[] rows = clipboard.Split('\n');
			string[][] data	= new string[rows.Length][];
			for(int r = 0; r < rows.Length; ++r) {
				data[r] = rows[r].Split('\t');
				for (int c = 0; c < data[r].Length; ++c) {
					CellPosition pos = new CellPosition(r, c) + currentCellSelectionRange.Min;
					string value = data[r][c];
					//Debug.Log(pos + ": " + value);
					Parse.Error error = null;
					try {
						error = SetCellValue(pos, value);
					} catch(System.Exception e) {
						Debug.LogError(e);
					}
					if (error != null) {
						Debug.LogError(error);
					}
				}
			}
			RefreshVisibleCells();
		}
		public void CopySelectionToClipboard() {
			StringBuilder sb = new StringBuilder();
			CellPosition min = CellPosition.Invalid, max = CellPosition.Invalid;
			if (currentSelectedCell != null) {
				min = max = currentSelectedCell.position;
			}
			if (currentCellSelectionRange.IsValid) {
				min = CellPosition.Min(min, currentCellSelectionRange.Min);
				max = CellPosition.Max(max, currentCellSelectionRange.Max);
			}
			for (int i = 0; i < selection.Count; ++i) {
				CellRange csel = selection[i];
				if (csel.IsValid) {
					min = CellPosition.Min(min, csel.Min);
					max = CellPosition.Max(max, csel.Max);
				}
			}
			for (int r = min.Row; r >= 0 && r <= max.Row; ++r) {
				if (r == -1) {
					for (int c = min.Column; c >= 0 && c <= max.Column; ++c) {
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
