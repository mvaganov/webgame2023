using UnityEngine;
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

		public void OnPointerMove(PointerEventData eventData) => spreadsheet.CellPointerMove(this);

		public void OnPointerDown(PointerEventData eventData) => spreadsheet.CellPointerDown(this);

		public void OnPointerUp(PointerEventData eventData) => spreadsheet.CellPointerUp(this);
	}
}
