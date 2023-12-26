using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Spreadsheet {
	[RequireComponent(typeof(RectTransform))]
	public class Cell : MonoBehaviour, IPointerDownHandler, IPointerMoveHandler, IPointerUpHandler {
		private int _cellTypeIndex;
		public CellPosition position;
		public Spreadsheet _spreadsheet;

		[SerializeField]
		private bool _selected;
		[SerializeField]
		public bool ErrorState;
		/// <summary>Selectable element, pointer changes color. Cached in <see cref="Awake"/></summary>
		private Selectable _selectable;
		/// <summary>Cached in <see cref="Awake"/></summary>
		private Color _normalColor;
		private Color _disabledColor;
		/// <summary>Set in <see cref="AssignSetFunction"/>. This is a cached method that invokes the column's set method</summary>
		private System.Func<string, Parse.Error> setCellData;

		public Selectable SelectableComponent => _selectable;

		public RectTransform RectTransform => GetComponent<RectTransform>();

		public int CellTypeIndex => _cellTypeIndex;

		public Spreadsheet spreadsheet {
			get => _spreadsheet;
			set {
				_spreadsheet = value;
				UpdateTooltip();
			}
		}

		public bool Interactable {
			get => Ui.TryGetTextInputInteractable(this, out bool i) && i;
			set {
				Ui.TrySetTextInputInteractable(this, value);
			}
		}

		private void OnEnable() {
			UpdateTooltip();
		}

		private void OnDisable() {
			ClearTooltip();
		}

		private void UpdateTooltip() {
			// TODO if this cell has a spreadsheet, check if it has a tooltip. if it does, create the UI for it.
		}

		private void ClearTooltip() {
			// TODO if this cell has a tooltip, release it
		}

		internal void Select() {
			Ui.TrySelectTextInput(this);
		}

		public bool Selected {
			get => _selected;
			set {
				if (_selected != value && position.IsNormalPosition) {
					if (value) {
						SetColor(spreadsheet.MultiselectColor);
					} else {
						ResetColor();
					}
				}
				_selected = value;
			}
		}

		public override string ToString() => (spreadsheet != null ? spreadsheet.name + "!" : "") + position.ToString();

		public void ToggleInteractable() => Interactable = !Interactable;
		
		public void SetCellTypeIndex(int cellTypeIndex) => _cellTypeIndex = cellTypeIndex;

		public void ToggleOffIfValid() {
			if (ErrorState) {
				return;
			}
			bool interactable = Interactable;
			if (interactable) {
				Interactable = false;
			}
		}

		public void SetCellOffNextFrameIfValidCallback(string str) {
			StartCoroutine(ToggleOffNextFrame());
			IEnumerator ToggleOffNextFrame() {
				yield return null;
				ToggleOffIfValid();
			}
		}

		public void SetColor(Color color) {
			ColorBlock block = _selectable.colors;
			block.normalColor = color;
			block.disabledColor = color;
			_selectable.colors = block;
		}

		public void ResetColor() {
			ColorBlock block = _selectable.colors;
			block.normalColor = _normalColor;
			block.disabledColor = _disabledColor;
			_selectable.colors = block;
		}

		private void Awake() {
			_selectable = GetComponent<Selectable>();
			if (_selectable != null) {
				_normalColor = _selectable.colors.normalColor;
				_disabledColor = _selectable.colors.disabledColor;
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
			RectTransform rt = GetComponent<RectTransform>();
			UnityEvent<string> submitEvent = Ui.GetTextSubmitEvent(rt);
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
			submitEvent.AddListener(SetCellOffNextFrameIfValidCallback);
		}

		public void SetString(string str) {
			spreadsheet.SetCellValue(position, str);
		}

		public void OnPointerMove(PointerEventData eventData) => spreadsheet.CellPointerMove(this);

		public void OnPointerDown(PointerEventData eventData) => spreadsheet.CellPointerDown(this);

		public void OnPointerUp(PointerEventData eventData) => spreadsheet.CellPointerUp(this);
	}
}
