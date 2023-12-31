using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Spreadsheet {
	public abstract partial class Spreadsheet : MonoBehaviour {
		public ScrollRect ScrollView;
		public CellGenerator cellGenerator;
		public Vector2 defaultCellSize = new Vector2(100, 30);
		[SerializeField] private Color _multiSelectColor = new Color(7f / 8, 1, 7f / 8);
		[SerializeField] private Color _errorCellColor = new Color(1, 7f/8, 7f / 8);
		[SerializeField] private Color _tooltipColor = new Color(1, 1, 7f / 8);
		private PointerEventData _fakePointerEventData;
		private RectTransform _transform;
		public List<Column> columns = new List<Column>();
		public List<Row> rows = new List<Row>();
		public List<Cell> _nonConformingCells = new List<Cell>();

		public Color MultiselectColor => _multiSelectColor;

		public Color ErrorCellColor => _errorCellColor;

		public RectTransform ContentArea => ScrollView.content;

		public CellRange AllRange => new CellRange(CellPosition.Zero, new CellPosition(rows.Count - 1, columns.Count - 1));

		public CellPosition LastCell => new CellPosition(rows.Count - 1, columns.Count - 1);

		public abstract System.Array Objects { get; set; }

		public PointerEventData FakePointerEventData => _fakePointerEventData != null ? _fakePointerEventData
			: _fakePointerEventData = new PointerEventData(EventSystem.current);

		private void Awake() {
			_transform = GetComponent<RectTransform>();
		}

		protected virtual void Start() {
			InitializeData();
			//InitializeColumnHeaders();
			//InitializeRowHeaders();
			//GenerateCells();
			StartCoroutine(RefreshVisibleCellsAfterUiCalculates());
			IEnumerator RefreshVisibleCellsAfterUiCalculates() {
				yield return null;
				_rangeToUpdateAsap = GetVisibleCellRange();
			}
		}

		protected virtual void Update() {
			UpdateRefreshCells();
			KeyboardUpdate();
		}

		/// <summary>
		/// Convenience method for <see cref="Objects"/> set method
		/// </summary>
		public void SetObjects<T>(List<T> _objects, System.Array value) {
			_objects.Clear();
			_objects.Capacity = value.Length;
			for (int i = 0; i < value.Length; i++) {
				T obj = (T)value.GetValue(i);
				_objects.Add(obj);
			}
		}

		/// <summary>
		/// Convenience method for <see cref="Objects"/> get method
		/// </summary>
		public System.Array GetObjects<T>(List<T> _objects, ref System.Array _value) {
			return _value != null ? _value : _value = _objects.ToArray();
		}

		public virtual void InitializeData() {
			rows.Clear();
			CreateRowsForEachObject();
		}

		protected virtual void CreateRowsForEachObject() {
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
					string name = Ui.GetName(data).ToString();
					row = new Row(data, name, defaultCellSize.y);
					rows.Add(row);
				} else {
					row = rows[i];
				}
				row.Render(columns);
			}
		}

		protected virtual void DestroyFunction(GameObject go) {
			if (Application.isPlaying) {
				Cell cell = go.GetComponent<Cell>();
				if (cell != null) {
					cellGenerator.FreeCellUi(cell);
				} else {
					Destroy(go);
				}
			} else {
				DestroyImmediate(go);
			}
		}

		/// <summary>
		/// Should be called by Unity <see cref="ScrollRect"/>
		/// </summary>
		public void RefreshVisibleCells() {
			CellRange visible = GetVisibleCellRange();
			//Debug.Log($"all {AllRange}, visible {visible}");
			RefreshCells(visible);
		}

		public Parse.Error SetCellValue(CellPosition position, string value) {
			int r = position.Row;
			int c = position.Column;
			Parse.Error error = columns[c].SetData.Invoke(rows[r].data, value);
			Cell cell = GetCellUi(position);
			if (error != null && error.IsError) {
				if (cell != null) {
					cell.ErrorState = true;
					cell.SetColor(ErrorCellColor);
					Object textObject = Ui.GetTextObject(cell.RectTransform);
					//Debug.Log("set cursor " + err.index);
					Ui.SetCursorPosition(textObject, error.index);
				}
				string errStr = error.ToString();
				Debug.LogError(errStr + "\n" + error.line + ":" + error.letter + "  idx" + error.index);
				SetMetaData(cell, new MetaData(MetaDataKind.Error, errStr));
			} else {
				RefreshRestOfRow(position);
				if (cell != null) {
					cell.ErrorState = false;
				}
			}
			return error;
		}

		private void RefreshRestOfRow(CellPosition position) {
			Row row = rows[position.Row];
			Cell c = row.Cells[position.Column];
			row.Cells[position.Column] = null;
			row.Refresh(this);
			row.Cells[position.Column] = c;
		}
	}
}
