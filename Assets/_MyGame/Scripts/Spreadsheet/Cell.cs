using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Spreadsheet {
	public class Cell : MonoBehaviour, IPointerDownHandler, IPointerMoveHandler, IPointerUpHandler {
		public CellPosition position;
		public Spreadsheet spreadsheet;
		[SerializeField]
		private bool _selected;
		private Selectable _selectable;
		private Color _normalColor;
		private System.Func<string, Parse.Error> setCellData;

		public Selectable SelectableComponent => _selectable;

		public bool Selected {
			get => _selected;
			set {
				if (_selected != value && position.IsNormalPosition) {
					if (value) {
						SetColor(spreadsheet.multiSelectColor);
					} else {
						SetColor(_normalColor);
					}
				}
				_selected = value;
			}
		}

		public void SetColor(Color color) {
			ColorBlock block = _selectable.colors;
			block.normalColor = color;
			_selectable.colors = block;
		}

		private void Awake() {
			_selectable = GetComponent<Selectable>();
			_normalColor = _selectable.colors.normalColor;
		}

		public static void Set(GameObject gameObject, Spreadsheet speradsheet, int row, int column) {
			Cell cell = gameObject.GetComponent<Cell>();
			if (cell == null) {
				cell = gameObject.AddComponent<Cell>();
			}
			cell.spreadsheet = speradsheet;
			cell.position = new CellPosition(row, column);
		}

		public void AssignSetFunction(System.Func<object, object, Parse.Error> func) {
			UnityEvent<string> submitEvent = Spreadsheet.GetTextSubmitEvent(GetComponent<RectTransform>());
			if (submitEvent == null) { return; }
			object obj = spreadsheet.rows[position.Row].data;
			setCellData = str => func.Invoke(obj, str);
			submitEvent.RemoveAllListeners();
			submitEvent.AddListener(SetString);
		}

		private void SetString(string str) {
			Parse.Error err = setCellData(str);
			if (err != null && err.IsError) {
				string errStr = err.ToString();
				Debug.LogError(errStr);
				spreadsheet.SetPopup(this, errStr);
			} else {
				Row row = spreadsheet.rows[position.Row];
				if (row.Cells[position.Column] != this) {
					throw new System.Exception($"expected to be modifying {this}\nfound {row.Cells[position.Row]}");
				}
				// don't refresh self, self is being modified by the user.
				row.Cells[position.Column] = null;
				row.Refresh(spreadsheet);
				row.Cells[position.Column] = this;
			}
		}

		public void OnPointerMove(PointerEventData eventData) => spreadsheet.CellPointerMove(this);

		public void OnPointerDown(PointerEventData eventData) => spreadsheet.CellPointerDown(this);

		public void OnPointerUp(PointerEventData eventData) => spreadsheet.CellPointerUp(this);
	}
}
