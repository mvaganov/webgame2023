using System;
using System.Collections.Generic;
using System.Text;

namespace Spreadsheet {
	[System.Serializable]
	public struct CellPosition {
		public int Row, Column;
		public bool IsEntireRow { get => Column < 0; set => Column = (value != IsEntireRow) ? ~Column : Column; }
		public bool IsEntireColumn { get => Row < 0; set => Row = (value != IsEntireColumn) ? ~Row : Row; }
		public bool IsNormalPosition { get => Column >= 0 && Row >= 0; }
		public CellPosition(int row, int column) { Row = row; Column = column; }
		public bool Equals(CellPosition other) => Row == other.Row && Column == other.Column;
		public static bool operator ==(CellPosition left, CellPosition right) => left.Equals(right);
		public static bool operator !=(CellPosition left, CellPosition right) => !left.Equals(right);
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
				return new Parse.Error("missing column identifier", str, 0, 0);
			}
			string columnId = str.Substring(0, columnEnd);
			position.Column = ColumnStringToInt(columnId);
			int rowEnd = columnEnd;
			while (rowEnd < str.Length && char.IsDigit(str[rowEnd])) {
				++rowEnd;
			}
			if (columnEnd == rowEnd) {
				return new Parse.Error("missing row identifier", str, 0, columnEnd);
			}
			string rowId = str.Substring(columnEnd, rowEnd - columnEnd);
			position.Row = int.Parse(rowId);
			return new Parse.Error(null, str, 0, rowEnd);
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
	}

	[System.Serializable]
	public struct CellSelection {
		public CellPosition Start, End;

		public int Area { get => (Math.Abs(Start.Row - End.Row) + 1) * (Math.Abs(Start.Column - End.Column) + 1); }

		public CellSelection(int row, int column) : this (new CellPosition(row, column)) { }
		public CellSelection(CellPosition position) { Start = End = position; }
		public bool Equals(CellSelection other) => Start == other.Start && End == other.End;
		public static bool operator ==(CellSelection left, CellSelection right) => left.Equals(right);
		public static bool operator !=(CellSelection left, CellSelection right) => !left.Equals(right);
		public override bool Equals(object other) => other is CellPosition cell && Equals(cell);
		public override int GetHashCode() => Start.GetHashCode() | (End.GetHashCode() << 32);
		public override string ToString() {
			//if (Start == End) { return Start.ToString(); }
			return Start + ":" + End;
		}
		public static Parse.Error FromString(string str, out CellSelection selection) {
			selection = new CellSelection();
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
			return new Parse.Error(null, str, 0, posStrStart + error.letter);
		}

		public bool ContainsStrict(CellPosition position) {
			return Start.Row <= position.Row && End.Row >= position.Row
				&& Start.Column <= position.Column && End.Column >= position.Column;
		}

		public bool Contains(CellPosition position) {
			return ((Start.Row <= position.Row && End.Row >= position.Row)
				|| (Start.Row >= position.Row && End.Row <= position.Row))
				&& ((Start.Column <= position.Column && End.Column >= position.Column)
				|| (Start.Column >= position.Column && End.Column <= position.Column));
		}

	}
}
