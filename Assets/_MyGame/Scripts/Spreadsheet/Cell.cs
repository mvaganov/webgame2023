using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Spreadsheet {
	[RequireComponent(typeof(RectTransform))]
	public class Cell : MonoBehaviour, IPointerDownHandler, IPointerMoveHandler, IPointerUpHandler {
		private int _cellTypeIndex;
		public CellPosition position;
		public Spreadsheet spreadsheet;

		[SerializeField]
		private bool _selected;
		/// <summary>Selectable element, pointer changes color. Cached in <see cref="Awake"/></summary>
		private Selectable _selectable;
		/// <summary>Cached in <see cref="Awake"/></summary>
		private Color _normalColor;
		/// <summary>Set in <see cref="AssignSetFunction"/></summary>
		private System.Func<string, Parse.Error> setCellData;

		public Selectable SelectableComponent => _selectable;

		public RectTransform RectTransform => GetComponent<RectTransform>();

		public int CellTypeIndex => _cellTypeIndex;
		public void SetCellTypeIndex(int cellTypeIndex) => _cellTypeIndex = cellTypeIndex;

		public bool Selected {
			get => _selected;
			set {
				if (_selected != value && position.IsNormalPosition) {
					if (value) {
						SetColor(spreadsheet.MultiselectColor);
					} else {
						SetColor(_normalColor);
					}
				}
				_selected = value;
			}
		}

		public override string ToString() => (spreadsheet != null ? spreadsheet.name + "!" : "") + position.ToString();

		public void SetColor(Color color) {
			ColorBlock block = _selectable.colors;
			block.normalColor = color;
			_selectable.colors = block;
		}

		private void Awake() {
			_selectable = GetComponent<Selectable>();
			if (_selectable != null) {
				_normalColor = _selectable.colors.normalColor;
			}
		}

		public static void Set(GameObject gameObject, Spreadsheet speradsheet, CellPosition cellPosition) {
			Cell cell = gameObject.GetComponent<Cell>();
			if (cell == null) { cell = gameObject.AddComponent<Cell>(); }
			cell.Set(speradsheet, cellPosition);
		}

		public Cell Set(Spreadsheet spreadsheet, CellPosition cellPosition) {
			this.spreadsheet = spreadsheet;
			spreadsheet.AssignCell(cellPosition, this);
			Selected = spreadsheet.IsSelected(cellPosition);
			position = cellPosition;
			return this;
		}

		public void AssignSetFunction(System.Func<object, object, Parse.Error> func) {
			UnityEvent<string> submitEvent = Ui.GetTextSubmitEvent(GetComponent<RectTransform>());
			if (submitEvent == null) { return; }
			if (spreadsheet == null) {
				Debug.Log("failed to assign spreadsheet?");
			}
			if (spreadsheet.rows[position.Row] == null) {
				Debug.Log("missing Rows?");
			}
			object obj = spreadsheet.rows[position.Row].data;
			setCellData = str => func.Invoke(obj, str);
			submitEvent.RemoveAllListeners();
			submitEvent.AddListener(SetString);
		}

		private void SetString(string str) {
			Parse.Error err = setCellData(str);
			if (err != null) {
				string errStr = err.ToString();
				Debug.LogError(errStr + "\n" + err.line+":"+err.letter+"  idx"+err.index);
				spreadsheet.SetPopup(this, errStr);
				SetColor(spreadsheet.ErrorCellColor);
				Object textObject = Ui.GetTextObject(RectTransform);
				//Debug.Log("set cursor " + err.index);
				Ui.SetCursorPosition(textObject, err.index);
			} else {
				RefreshRestOfRow();
			}
		}

		public void RefreshRestOfRow() {
			Row row = spreadsheet.rows[position.Row];
			if (row.Cells[position.Column] != this) {
				throw new System.Exception($"expected to be modifying {this}\nfound {row.Cells[position.Row]}");
			}
			row.Cells[position.Column] = null;
			row.Refresh(spreadsheet);
			row.Cells[position.Column] = this;
		}

		public void OnPointerMove(PointerEventData eventData) => spreadsheet.CellPointerMove(this);

		public void OnPointerDown(PointerEventData eventData) => spreadsheet.CellPointerDown(this);

		public void OnPointerUp(PointerEventData eventData) => spreadsheet.CellPointerUp(this);
	}
}
