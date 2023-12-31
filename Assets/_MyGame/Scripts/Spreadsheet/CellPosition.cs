using System;

namespace Spreadsheet {
	[System.Serializable]
	public struct CellPosition {
		public int Row, Column;
		public static CellPosition Invalid = new CellPosition(-1, -1);
		public static CellPosition Zero = new CellPosition(0, 0);
		public static CellPosition One = new CellPosition(1, 1);
		public bool IsEntireRow { get => Column < 0 && Row >= 0; set => Column = (value != IsEntireRow) ? ~Column : Column; }
		public bool IsEntireColumn { get => Row < 0 && Column >= 0; set => Row = (value != IsEntireColumn) ? ~Row : Row; }
		public bool IsNormalPosition { get => Column >= 0 && Row >= 0; }
		public bool IsInvalid { get => Column < 0 && Row < 0; }
		public CellPosition(int row, int column) { Row = row; Column = column; }
		public bool Equals(CellPosition other) => Row == other.Row && Column == other.Column;
		public static bool operator ==(CellPosition left, CellPosition right) => left.Equals(right);
		public static bool operator !=(CellPosition left, CellPosition right) => !left.Equals(right);
		public static CellPosition operator -(CellPosition toMakeNegative) => new CellPosition(-toMakeNegative.Row, -toMakeNegative.Column);
		public override bool Equals(object other) => other is CellPosition cell && Equals(cell);
		public override int GetHashCode() => Column | (Row << 16);
		public override string ToString() => $"{ColumnIntToString(Column)}{Row}";
		public static Parse.Error FromString(string str, out CellPosition position) {
			position = new CellPosition();
			int columnEnd = 0;
			str = str.ToUpper();
			while(columnEnd < str.Length && char.IsLetter(str[columnEnd])) {
				++columnEnd;
			}
			if (columnEnd == 0) {
				return new Parse.Error("missing column identifier", str, 0);
			}
			string columnId = str.Substring(0, columnEnd);
			position.Column = ColumnStringToInt(columnId);
			int rowEnd = columnEnd;
			while (rowEnd < str.Length && char.IsDigit(str[rowEnd])) {
				++rowEnd;
			}
			if (columnEnd == rowEnd) {
				return new Parse.Error("missing row identifier", str, columnEnd);
			}
			string rowId = str.Substring(columnEnd, rowEnd - columnEnd);
			position.Row = int.Parse(rowId);
			return new Parse.Error(null, str, rowEnd);
		}
		public static string ColumnIntToString(int columnIndex) {
			if (columnIndex < 26) {
				return ((char)('A' + columnIndex)).ToString();
			}
			int numDigitsNeeded = 1;
			int num = columnIndex;
			const int totalBase = 26;
			int baseNum = totalBase;
			while (num >= baseNum) {
				++numDigitsNeeded;
				num -= baseNum;
				baseNum *= totalBase;
			}
			char[] digits = new char[numDigitsNeeded];
			for(int i = 0; i < digits.Length; ++i) {
				digits[i] = 'A';
			}
			int currentDigit = numDigitsNeeded;
			while(num > 0) {
				digits[--currentDigit] = (char)('A' + (num % totalBase));
				num /= totalBase;
			}
			return new string(digits);
		}

		public static int ColumnStringToInt(string str) {
			string upperStr = str.ToUpper();
			const int totalBase = 26;
			int startNum = 0;
			for (int i = 1; i < str.Length; ++i) {
				startNum += (int)System.Math.Pow(totalBase, i);
			}
			int baseNum = 1;
			int number = 0;
			for (int digitIndex = 0; digitIndex < str.Length; ++digitIndex) {
				int digit = upperStr[str.Length - 1 - digitIndex] - 'A';
				number += digit * baseNum;
				baseNum *= totalBase;
			}
			number += startNum;
			return number;
		}
		public static CellPosition Min(CellPosition a, CellPosition b) {
			return new CellPosition(Math.Min(a.Row, b.Row), Math.Min(a.Column, b.Column));
		}

		public static CellPosition Max(CellPosition a, CellPosition b) {
			return new CellPosition(Math.Max(a.Row, b.Row), Math.Max(a.Column, b.Column));
		}

		public static CellPosition operator -(CellPosition a, CellPosition b) {
			return new CellPosition(a.Row - b.Row, a.Column - b.Column);
		}

		public static CellPosition operator +(CellPosition a, CellPosition b) {
			return new CellPosition(a.Row + b.Row, a.Column + b.Column);
		}
		public static implicit operator CellPosition((int row, int col) tuple) {
			return new CellPosition(tuple.row, tuple.col);
		}
	}

	[System.Serializable]
	public struct CellRange {
		public CellPosition Start, End;
		public static CellRange Invalid = new CellRange(CellPosition.Zero, new CellPosition(-1, -1));
		public bool IsValid => Start.IsNormalPosition && End.IsNormalPosition;
		public int Area => Width * Height;
		public int Width => (Math.Abs(Start.Column - End.Column) + 1);
		public int Height => (Math.Abs(Start.Row - End.Row) + 1);
		public CellPosition Size => new CellPosition(Height, Width);
		public CellPosition Min {
			get => new CellPosition(MinRow, MinColumn);
			set { Normalize(); Start = value; }
		}
		public CellPosition Max {
			get => new CellPosition(MaxRow, MaxColumn);
			set { Normalize(); End = value; }
		}
		public int MinColumn {
			get => Math.Min(Start.Column, End.Column);
			set {
				if      (Start.Column < End.Column) { Start.Column = value; }
				else if (Start.Column > End.Column) { End.Column = value; }
				else {   Start.Column = End.Column = value; }
			}
		}
		public int MaxColumn {
			get => Math.Max(Start.Column, End.Column);
			set {
				if      (Start.Column > End.Column) { Start.Column = value; }
				else if (Start.Column < End.Column) { End.Column = value; }
				else {   Start.Column = End.Column = value; }
			}
		}
		public int MinRow {
			get => Math.Min(Start.Row, End.Row);
			set {
				if      (Start.Row < End.Row) { Start.Row = value; }
				else if (Start.Row > End.Row) { End.Row = value; }
				else {   Start.Row = End.Row = value; }
			}
		}
		public int MaxRow {
			get => Math.Max(Start.Row, End.Row);
			set {
				if      (Start.Row > End.Row) { Start.Row = value; }
				else if (Start.Row < End.Row) { End.Row = value; }
				else {   Start.Row = End.Row = value; }
			}
		}

		public CellRange(int row, int column) : this (new CellPosition(row, column)) { }
		public CellRange(CellPosition position) { Start = End = position; }
		public CellRange(CellPosition start, CellPosition end) { Start = start; End = end; }
		public CellRange(CellRange range) : this(range.Start, range.End) { }
		public bool Equals(CellRange other) => Start == other.Start && End == other.End;
		public static bool operator ==(CellRange left, CellRange right) => left.Equals(right);
		public static bool operator !=(CellRange left, CellRange right) => !left.Equals(right);
		public override bool Equals(object other) => other is CellPosition cell && Equals(cell);
		public override int GetHashCode() => Start.GetHashCode() | (End.GetHashCode() << 32);
		public override string ToString() {
			//if (Start == End) { return Start.ToString(); }
			return Start + ":" + End;
		}
		public static Parse.Error FromString(string str, out CellRange selection) {
			selection = new CellRange();
			int delimeterIndex = str.IndexOf(':');
			if (delimeterIndex < 0) {
				return new Parse.Error("missing delimiter ':' between positions");
			}
			string posStr = str.Substring(0, delimeterIndex);
			Parse.Error error = CellPosition.FromString(posStr, out selection.Start);
			if (error != null && error.IsError) {
				return error;
			}
			int posStrStart = delimeterIndex + 1;
			posStr = str.Substring(posStrStart);
			error = CellPosition.FromString(posStr, out selection.End);
			if (error != null && error.IsError) {
				return error;
			}
			return new Parse.Error(null, str, posStrStart + error.letter);
		}

		public bool ContainsStrict(CellPosition position) {
			return Start.Row <= position.Row && End.Row >= position.Row
				&& Start.Column <= position.Column && End.Column >= position.Column;
		}

		public bool Contains(CellPosition position) {
			return ((Start.Row <= position.Row && End.Row >= position.Row)
				||    (Start.Row >= position.Row && End.Row <= position.Row))
				&& ((Start.Column <= position.Column && End.Column >= position.Column)
				||  (Start.Column >= position.Column && End.Column <= position.Column));
		}

		public void Normalize() {
			if(Start.Row > End.Row || Start.Column > End.Column) {
				CellPosition min = Min, max = Max;
				Start = min;
				End = max;
			}
		}

		/// <summary>
		/// Goes through Start (inclusive) to End (inclusive), Row by Column.
		/// </summary>
		/// <param name="action"></param>
		public void ForEach(Action<CellPosition> action) {
			CellPosition cursor = Start;
			for (cursor.Row = Start.Row; cursor.Row <= End.Row; ++cursor.Row) {
				for(cursor.Column = Start.Column; cursor.Column <= End.Column; ++cursor.Column) {
					action.Invoke(cursor);
				}
			}
		}

		public void AddToUnion(CellRange other) {
			if (other.Start.Row    < Start.Row)    { Start.Row = other.Start.Row; }
			if (other.Start.Column < Start.Column) { Start.Column = other.Start.Column; }
			if (other.End.Row      > End.Row)      { End.Row = other.End.Row; }
			if (other.End.Column   > End.Column)   { End.Column = other.End.Column; }
		}

		public void AddToUnion(CellPosition other) {
			if (other.Row < Start.Row)       { Start.Row = other.Row; }
			if (other.Column < Start.Column) { Start.Column = other.Column; }
			if (other.Row > End.Row)         { End.Row = other.Row; }
			if (other.Column > End.Column)   { End.Column = other.Column; }
		}

		public void ExcludeToIntersection(CellRange other) {
			if (other.Start.Row    > Start.Row)    { Start.Row = other.Start.Row; }
			if (other.Start.Column > Start.Column) { Start.Column = other.Start.Column; }
			if (other.End.Row      < End.Row)      { End.Row = other.End.Row; }
			if (other.End.Column   < End.Column)   { End.Column = other.End.Column; }
		}

		public static CellRange Union(CellRange a, CellRange b) {
			CellRange union = new CellRange(a);
			union.AddToUnion(b);
			return union;
		}

		public static CellRange Intersection(CellRange a, CellRange b) {
			CellRange intersection = new CellRange(a);
			intersection.ExcludeToIntersection(b);
			return intersection;
		}

		public bool TryGetOtherCorner(CellPosition corner, out CellPosition othercorner) {
			othercorner = new CellPosition();
			if (corner.Row == Start.Row) {
				othercorner.Row = End.Row;
			} else if (corner.Row == End.Row) {
				othercorner.Row = Start.Row;
			} else {
				othercorner.Row = -1;
			}
			if (corner.Column == Start.Column) {
				othercorner.Column = End.Column;
			} else if (corner.Column == End.Column) {
				othercorner.Column = Start.Column;
			} else {
				othercorner.Column = -1;
			}
			return othercorner.IsNormalPosition;
		}

		public static CellRange operator -(CellRange range, CellPosition delta) {
			return new CellRange(range.Start - delta, range.End - delta);
		}

		public static CellRange operator +(CellRange range, CellPosition delta) {
			return new CellRange(range.Start + delta, range.End + delta);
		}

		public RectangleMath.RectDirection GetClosestCorner(CellPosition point) {
			RectangleMath.RectDirection dir = RectangleMath.RectDirection.None;
			int fromMinX = Math.Abs(point.Column - Min.Column);
			int fromMaxX = Math.Abs(point.Column - Max.Column);
			int fromMinY = Math.Abs(point.Row - Min.Row);
			int fromMaxY = Math.Abs(point.Row - Max.Row);
			if (fromMinX <= fromMaxX) { dir |= RectangleMath.RectDirection.MinX; }
			if (fromMinX >= fromMaxX) { dir |= RectangleMath.RectDirection.MaxX; }
			if (fromMinY <= fromMaxY) { dir |= RectangleMath.RectDirection.MinY; }
			if (fromMinY >= fromMaxY) { dir |= RectangleMath.RectDirection.MaxY; }
			return dir;
		}

		public void SetCorner(RectangleMath.RectDirection dir, CellPosition point) {
			if (dir.HasFlag(RectangleMath.RectDirection.MinX)) { MinColumn = point.Column; }
			if (dir.HasFlag(RectangleMath.RectDirection.MaxX)) { MaxColumn = point.Column; }
			if (dir.HasFlag(RectangleMath.RectDirection.MinY)) { MinRow = point.Row; }
			if (dir.HasFlag(RectangleMath.RectDirection.MaxY)) { MaxRow = point.Row; }
		}
	}
}
