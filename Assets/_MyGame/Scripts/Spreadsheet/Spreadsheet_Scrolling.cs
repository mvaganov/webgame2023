using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spreadsheet {
	public partial class Spreadsheet {
		/// <summary>
		/// Source data used to calculate of cached <see cref="_currentCellRange"/> is invalidated
		/// </summary>
		private Vector2 _cellVisibleStart, _cellVisibleEnd;
		public Vector2 _columnRowHeaderSize = new Vector2(100, 40);
		public Vector2 _cellPadding = new Vector2(2, 1);
		public RectTransform ColumnHeadersArea;
		public RectTransform RowHeadersArea;
		private bool _refreshRowPositions = true, _refreshColumnPositions = true;
		[SerializeField] private bool _showRowHeaders = true, _showColumnHeaders = true;
		private static bool _beNoisyAboutWeirdCornercaseRefreshBehaviorWhenScrollingFast = false;
		/// <summary>
		/// Cached cell range. prevents O(Log(N)) algorithm to find where a cell range is
		/// </summary>
		private CellRange _currentCellRange;

		public CellRange GetVisibleCellRange() {
			RefreshCellPositionLookupTable();
			Vector2 start = ScrollView.content.anchoredPosition;
			start.x *= -1;
			Vector2 end = start + ScrollView.viewport.rect.size;
			if (start == _cellVisibleStart && end == _cellVisibleEnd) {
				return _currentCellRange;
			}
			_currentCellRange.Start.Row = BinarySearchLookupTable(rows, start.y, r => r.yPosition);
			_currentCellRange.Start.Column = BinarySearchLookupTable(columns, start.x, c => c.xPosition);
			_currentCellRange.End.Row = BinarySearchLookupTable(rows, end.y, r => r.yPosition);
			_currentCellRange.End.Column = BinarySearchLookupTable(columns, end.x, c => c.xPosition);
			_currentCellRange.ExcludeToIntersection(AllRange);
			start = _cellVisibleStart;
			end = _cellVisibleEnd;
			return _currentCellRange;
		}

		/// <summary>
		/// Used to find the best row/column index for the given x/y value
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="value"></param>
		/// <param name="getNum"></param>
		/// <returns></returns>
		private static int BinarySearchLookupTable<T>(IList<T> list, float value, System.Func<T, float> getNum) {
			int left = 0, right = list.Count - 1;
			while (left <= right) {
				int middle = (left + right) / 2;
				int comparison = getNum.Invoke(list[middle]).CompareTo(value);
				if (comparison == 0) {
					return middle;
				} else if (comparison < 0) {
					left = middle + 1;
				} else {
					right = middle - 1;
				}
			}
			return left - 1;
		}

		public Vector2 GetCellDrawPosition(CellPosition cellPosition) {
			RefreshCellPositionLookupTable();
			float x = cellPosition.Column >= 0 ? columns[cellPosition.Column].xPosition : 0;
			float y = cellPosition.Row >= 0 ? -rows[cellPosition.Row].yPosition : 0;
			return new Vector2(x, y);
		}

		private RectTransform PlaceCell(Cell cell, Vector2 cursor) {
			RectTransform rect = cell.RectTransform;
			rect.SetParent(ContentArea);
			rect.anchoredPosition = cursor;
			int r = cell.position.Row;
			int c = cell.position.Column;
			rect.sizeDelta = new Vector2(columns[c].width, rows[r].height);
			cell.spreadsheet = this;
			cell.AssignSetFunction(columns[c].SetData);
			rect.name = cell.position.ToString();
			return rect;
		}

		private void RefreshCellPositionLookupTable() {
			if (_refreshRowPositions) {
				CalculateRowPositions();
				_refreshRowPositions = false;
				Vector2 sizeDelta = ContentArea.sizeDelta;
				Row lastRow = rows[rows.Count - 1];
				sizeDelta.y = lastRow.yPosition + lastRow.height;
				ContentArea.sizeDelta = sizeDelta;
			}
			if (_refreshColumnPositions) {
				CalculateColumnPositions();
				_refreshColumnPositions = false;
				Vector2 sizeDelta = ContentArea.sizeDelta;
				Column lastColumn = columns[columns.Count - 1];
				sizeDelta.x = lastColumn.xPosition + lastColumn.width;
				ContentArea.sizeDelta = sizeDelta;
			}
		}

		private void CalculateRowPositions() {
			float cursor = 0;
			for (int r = 0; r < rows.Count; ++r) {
				rows[r].yPosition = cursor;
				cursor += rows[r].height + _cellPadding.y;
			}
		}

		private void CalculateColumnPositions() {
			float cursor = 0;
			for (int c = 0; c < columns.Count; ++c) {
				columns[c].xPosition = cursor;
				cursor += columns[c].width + _cellPadding.x;
			}
		}

		/// <summary>
		/// Should be notified when the scroll UI changes
		/// </summary>
		/// <param name="scroll"></param>
		public void AdjustColumnHeaders(Vector2 scroll) {
			ColumnHeadersArea.anchoredPosition = new Vector2(ContentArea.anchoredPosition.x, ColumnHeadersArea.anchoredPosition.y);
		}

		/// <summary>
		/// Should be notified when the scroll UI changes
		/// </summary>
		/// <param name="scroll"></param>
		public void AdjustRowHeaders(Vector2 scroll) {
			RowHeadersArea.anchoredPosition = new Vector2(RowHeadersArea.anchoredPosition.x, ContentArea.anchoredPosition.y);
		}

		public void ScrollToSee(Rect rect) {
			Rect adjustedViewArea = ContentArea.rect;
			Vector2 posMin = ContentArea.position;
			
			Debug.Log("NormPos "+ ScrollView.normalizedPosition+ "   pos " + posMin + "   rect " + adjustedViewArea);
			// TODO scroll to see a specific rectangle, the one belonging to the Cell that was just selected.
		}
	}
}
