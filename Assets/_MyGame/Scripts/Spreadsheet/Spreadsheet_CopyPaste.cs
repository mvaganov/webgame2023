using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Spreadsheet {
	public partial class Spreadsheet {
		public void CopySelectionToClipboard() {
			StringBuilder sb = new StringBuilder();
			CellPosition min = CellPosition.Invalid, max = CellPosition.Invalid;
			if (currentSelectedCell != null) {
				min = max = currentSelectedCell.position;
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
			for (int r = min.Row; r <= max.Row; ++r) {
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
