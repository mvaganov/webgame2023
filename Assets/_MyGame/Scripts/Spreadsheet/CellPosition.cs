namespace Spreadsheet {
	[System.Serializable]
	public struct CellPosition {
		public int Row, Column;
		public bool IsEntireRow { get => Column < 0; set => Column = (value != IsEntireRow) ? ~Column : Column; }
		public bool IsEntireColumn { get => Row < 0; set => Row = (value != IsEntireColumn) ? ~Row : Row; }
		public CellPosition(int row, int column) { Row = row; Column = column; }
		public bool Equals(CellPosition other) => Row == other.Row && Column == other.Column;
		public static bool operator ==(CellPosition left, CellPosition right) => left.Equals(right);
		public static bool operator !=(CellPosition left, CellPosition right) => !left.Equals(right);
		public override bool Equals(object other) => other is CellPosition cell && Equals(cell);
		public override int GetHashCode() => Column | (Row << 16);
		public override string ToString() => $"{ColumnToString(Column)},{Row}";
		public static string ColumnToString(int columnIndex) {
			// TODO finish me when less sleepy.
			if (columnIndex <= 26) {
				return ((char)('A' + columnIndex)).ToString();
			}
			int tensPlace = columnIndex/26;
			return "too big";
		}
	}

	[System.Serializable]
	public struct CellSelection {
		public CellPosition Start, End;
		public CellSelection(int row, int column) : this (new CellPosition(row, column)) { }
		public CellSelection(CellPosition position) { Start = End = position; }
		public bool Equals(CellSelection other) => Start == other.Start && End == other.End;
		public static bool operator ==(CellSelection left, CellSelection right) => left.Equals(right);
		public static bool operator !=(CellSelection left, CellSelection right) => !left.Equals(right);
		public override bool Equals(object other) => other is CellPosition cell && Equals(cell);
		public override int GetHashCode() => Start.GetHashCode() | (End.GetHashCode() << 32);
		public override string ToString() {
			if (Start == End) { return Start.ToString(); }
			return Start + ":" + End;
		}
	}
}
