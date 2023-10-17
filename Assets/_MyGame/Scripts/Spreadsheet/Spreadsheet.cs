using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Spreadsheet {
	// TODO break up this file into multiple.
	// - updating currently generated cells
	// - scrolling
	// - keeping visible cells
	public abstract partial class Spreadsheet : MonoBehaviour {
		[System.Serializable]
		public class CellType {
			public string name;
			public RectTransform prefab;
		}

		[ContextMenuItem(nameof(GenerateColumnHeaders),nameof(GenerateColumnHeaders))]
		public RectTransform ColumnHeadersArea;
		[ContextMenuItem(nameof(GenerateRowHeaders), nameof(GenerateRowHeaders))]
		public RectTransform RowHeadersArea;
		public ScrollRect ScrollView;
		public Vector2 columnRowHeaderSize = new Vector2(100, 40);
		public Vector2 defaultCellSize = new Vector2(100, 30);
		public Vector2 cellPadding = new Vector2(2, 1);
		public List<Column> columns = new List<Column>();
		public List<Row> rows = new List<Row>();
		public CellRange _lastRendered = CellRange.Invalid;
		public List<Cell> cells = new List<Cell>();
		private Cell selectedCell;
		private PointerEventData _fakePointerEventData;
		private int _popupUiIndex;
		private RectTransform _popupUiElement;
		public Color multiSelectColor;
		private RectTransform _transform;
		private bool _updatingVisiblity;
		private bool _mustUpdateVisiblity;
		private CellRange _rangeToUpdate;

		public RectTransform ContentArea => ScrollView.content;

		public CellRange AllRange => new CellRange(CellPosition.Zero, new CellPosition(rows.Count - 1, columns.Count - 1));

		public abstract System.Array Objects { get; set; }

		public PointerEventData FakePointerEventData => _fakePointerEventData != null ? _fakePointerEventData
			: _fakePointerEventData = new PointerEventData(EventSystem.current);

		private void Awake() {
			_transform = GetComponent<RectTransform>();
		}

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

		public virtual void Initialize() {
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
					string name = GetName(data).ToString();
					row = new Row(data, name, defaultCellSize.y);
					rows.Add(row);
				} else {
					row = rows[i];
				}
				row.Render(columns);
			}
		}

		public object GetName(object obj) {
			switch (obj) {
				case Object o: return o.name;
				case null: return null;
			}
			return obj.ToString();
		}

		public Parse.Error SetName(object obj, object nameObj) {
			string name = nameObj.ToString();
			switch (obj) {
				case Object o: o.name = name; return null;
			}
			string errorMessage = $"Could not set {obj}.name = \"{nameObj}\"";
			Debug.LogError(errorMessage);
			return new Parse.Error(errorMessage);
		}

		private void DestroyFunction(GameObject go) {
			if (Application.isPlaying) {
				Cell cell = go.GetComponent<Cell>();
				if (cell != null) {
					FreeCellUi(cell);
				} else {
					Destroy(go);
				}
			} else {
				DestroyImmediate(go);
			}
		}

		public Vector2 GetCellDrawPosition(CellPosition cellPosition) {
			Vector2 cursor = Vector2.zero;
			for(int r = 0; r < cellPosition.Row; ++r) {
				cursor.y -= rows[r].height + cellPadding.y;
			}
			for (int c = 0; c < cellPosition.Column; ++c) {
				cursor.x += columns[c].width + cellPadding.x;
			}
			return cursor;
		}

		private RectTransform PlaceCell(Cell cell, Vector3 cursor) {
			RectTransform rect = cell.RectTransform;
			rect.SetParent(ContentArea);
			rect.anchoredPosition = cursor;
			int r = cell.position.Row;
			int c = cell.position.Column;
			rect.sizeDelta = new Vector2(columns[c].width, rows[r].height);
			cell.AssignSetFunction(columns[c].SetData);
			rect.name = cell.position.ToString();
			return rect;
		}

		public void RefreshVisibleUi() {
			// TODO calculate range that is visible
			RefreshUi(AllRange);
		}

		public CellRange GetVisibleRange() {
			CellRange range = new CellRange();
			Vector3[] viewportCorners = new Vector3[4];
			Vector3[] contentCorners = new Vector3[4];
			ScrollView.viewport.GetWorldCorners(viewportCorners);
			ScrollView.content.GetWorldCorners(contentCorners);
			float viewportHeight = viewportCorners[1].y - viewportCorners[0].y;
			float viewportWidth = viewportCorners[2].x - viewportCorners[0].x;
			float contentHeight = contentCorners[1].y - contentCorners[0].y;
			float left = contentCorners[0].x - viewportCorners[0].x;
			float right = contentCorners[2].x - viewportCorners[0].x;

			//float top = contentCorners[1].y - viewportCorners[0].y;
			//float bottom = contentCorners[0].y - viewportCorners[0].y;
			// need to slip vertical, since Unity likes 0,0 at the lower left, and we want 0,0 at the top left
			float top = viewportHeight - (contentCorners[1].y - viewportCorners[0].y);
			float bottom = viewportHeight - (contentCorners[0].y - viewportCorners[0].y);
			Vector2 cursor = new Vector2(left, top);
			for(int r = 0; r < rows.Count; ++r) {
				cursor.y += rows[r].height + cellPadding.y;
				range.Start.Row = r;
				if (cursor.y >= 0) {
					break;
				}
			}
			for (int c = 0; c < columns.Count; ++c) {
				cursor.x += columns[c].width + cellPadding.x;
				range.Start.Column = c;
				if (cursor.x >= 0) {
					break;
				}
			}
			range.End = range.Start;
			//Debug.Log($"top {top}, left {left}\n{cursor} vs ({viewportWidth}, {viewportHeight})");
			if (cursor.y < viewportHeight) {
				for (int r = range.Start.Row + 1; r < rows.Count; ++r) {
					cursor.y += rows[r].height + cellPadding.y;
					range.End.Row = r;
					if (cursor.y >= viewportHeight) {
						break;
					}
				}
			}
			if (cursor.x < viewportWidth) {
				for (int c = range.Start.Column + 1; c < columns.Count; ++c) {
					cursor.x += columns[c].width + cellPadding.x;
					range.End.Column = c;
					if (cursor.x >= viewportWidth) {
						break;
					}
				}
			}
			//Debug.Log(range);
			return range;
		}

		public void UpdateUiBasedOnVisibility() {
			CellRange visible = GetVisibleRange();
			//Debug.Log($"all {AllRange}, visible {visible}");
			RefreshUi(visible);
		}

		public void RefreshUi(CellRange visibleRange) {
			if (_updatingVisiblity) {
				_mustUpdateVisiblity = true;
				_rangeToUpdate = visibleRange;
				return;
			}
			_mustUpdateVisiblity = false;
			//Debug.Log(visibleRange);
			StartCoroutine(UpdateCells(visibleRange));
		}

		protected virtual void Update() {
			if (_mustUpdateVisiblity && !_updatingVisiblity) {
				RefreshUi(_rangeToUpdate);
			}
		}

		private IEnumerator UpdateCells(CellRange visibleRange) {
			_updatingVisiblity = true;
			List<CellPosition> toRemove = new List<CellPosition>();
			List<CellPosition> toAdd = new List<CellPosition>();
			if (_lastRendered != visibleRange) {
				CellRange union = visibleRange;
				CellRange intersection = visibleRange;
				union.Union(_lastRendered);
				intersection.Intersection(_lastRendered);
				union.ForEach(cpos => {
					if (intersection.Contains(cpos)) { return; }
					if (_lastRendered.Contains(cpos)) { toRemove.Add(cpos); }
					if (visibleRange.Contains(cpos)) { toAdd.Add(cpos); }
				});
			}
			//toRemove.ForEach(cellPos => FreeCellUi(GetCellUi(cellPos)));
			for(int i = 0; i < toRemove.Count; ++i) {
				CellPosition cpos = toRemove[i];
				FreeCellUi(GetCellUi(cpos));
			}
			yield return null;
			//Debug.Log($"old: {_lastRendered}  new: {visibleRange}\nnew cells: [{string.Join(", ", toAdd)}], oldCells: [{string.Join(", ", toRemove)}]");
			for (int i = 0; i < toAdd.Count; ++i) {
				CellPosition cpos = toAdd[i];
				Vector2 cursor = GetCellDrawPosition(cpos);
				Cell cell = MakeNewCell(columns[cpos.Column].cellType).Set(this, cpos);
				PlaceCell(cell, cursor);
			}
			yield return null;
			for (int r = visibleRange.Start.Row; r <= visibleRange.End.Row; ++r) {
				Row row = rows[r];
				row.Refresh(this, visibleRange.Start.Column, visibleRange.End.Column);
			}
			_lastRendered = visibleRange;
			_updatingVisiblity = false;
		}

		public Cell GetCellUi(CellPosition cellPosition) {
			if (cellPosition.IsNormalPosition) {
				Cell[] cellUiRow = rows[cellPosition.Row].GetCellLookupTable(false);
				if (cellUiRow != null) {
					return cellUiRow[cellPosition.Column];
				}
			} else if (cellPosition.IsEntireColumn) {
				return columns[cellPosition.Column].headerCell;
			} else if (cellPosition.IsEntireRow) {
				return rows[cellPosition.Row].headerCell;
			}
			return null;
		}

		public void SetPopup(Cell cell, string text) {
			if (_popupUiElement == null) {
				Cell popup = MakeNewCell(_popupUiIndex).Set(this, CellPosition.Invalid);
				_popupUiElement = popup.RectTransform;
			} else {
				_popupUiElement.SetAsLastSibling();
			}
			RectTransform cellRectTransform = cell.GetComponent<RectTransform>();
			RectTransform popupRectTransform = _popupUiElement.GetComponent<RectTransform>();
			Ui.SetText(_popupUiElement, text);
			Vector3[] corners = new Vector3[4];
			cellRectTransform.GetLocalCorners(corners);
			popupRectTransform.anchoredPosition = corners[0];
			_popupUiElement.gameObject.SetActive(true);
		}

		public void AdjustColumnHeaders(Vector2 scroll) {
			ColumnHeadersArea.anchoredPosition = new Vector2(ContentArea.anchoredPosition.x, ColumnHeadersArea.anchoredPosition.y);
		}

		public void AdjustRowHeaders(Vector2 scroll) {
			RowHeadersArea.anchoredPosition = new Vector2(RowHeadersArea.anchoredPosition.x, ContentArea.anchoredPosition.y);
		}
	}
}
