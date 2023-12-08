using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spreadsheet {
	public partial class Spreadsheet {
		private static DurationTracker<KeyCode> _keyPressDuration = new DurationTracker<KeyCode>();
		[SerializeField]
		private float _keyHoldRepeatDuration = 0.125f;
		private Dictionary<KeyCode, Action> _keyMapNoCellSelected;
		private Dictionary<KeyCode, Action> _keyMapCellSelected;
		private Dictionary<KeyCode, Action> KeyMapNoCellSelected => _keyMapNoCellSelected != null ? _keyMapNoCellSelected :
			_keyMapNoCellSelected = new Dictionary<KeyCode, Action>() {
			[KeyCode.UpArrow] = () => CellMove((-1, 0)),
			[KeyCode.LeftArrow] = () => CellMove((0, -1)),
			[KeyCode.DownArrow] = () => CellMove((1, 0)),
			[KeyCode.RightArrow] = () => CellMove((0, 1)),
			[KeyCode.W] = () => CellMove((-1, 0)),
			[KeyCode.A] = () => CellMove((0, -1)),
			[KeyCode.S] = () => CellMove((1, 0)),
			[KeyCode.D] = () => CellMove((0, 1)),
			[KeyCode.LeftShift] = null,
			[KeyCode.RightShift] = null,
			[KeyCode.Return] = HandleReturn,
			[KeyCode.Tab] = HandleTab
		};

		private Dictionary<KeyCode, Action> KeyMapCellSelected => _keyMapCellSelected != null ? _keyMapCellSelected :
		_keyMapCellSelected = new Dictionary<KeyCode, Action>() {
			[KeyCode.Tab] = HandleTab
		};

		private void HandleReturn() {
			if (currentSelectedCell == null || !Ui.TryGetTextInputInteractable(currentSelectedCell, out bool isInteractable)) {
				return;
			}
			currentSelectedCell.ToggleInteractable();
			if (currentSelectedCell.Interactable) {
				currentSelectedCell.Select();
			}
		}

		private void HandleTab() {
			bool shiftPressed = (_keyPressDuration.TryGetDuration(KeyCode.LeftShift, out float lShift) && lShift > 0)
			|| (!_keyPressDuration.TryGetDuration(KeyCode.LeftShift, out float rShift) && rShift > 0);
			if (shiftPressed) {
				CellMovePrev();
			} else {
				CellMoveNext();
			}
		}

		private void CellMove(CellPosition direction) {
			if (MoveCurrentSelection(direction)) {
				ScrollToSee(currentSelectionPosition);
			}
		}

		private void CellMoveNext() {
			if (currentSelectionPosition.Column < columns.Count - 1) {
				CellMove((0, 1));
			} else if (currentSelectionPosition.Row < rows.Count - 1) {
				CellMove((1, -currentSelectionPosition.Column));
			} else {
				CellMove(-currentSelectionPosition);
			}
		}

		private void CellMovePrev() {
			if (currentSelectionPosition.Column > 0) {
				CellMove((0, -1));
			} else if (currentSelectionPosition.Row > 0) {
				CellMove((-1, columns.Count - 1));
			} else {
				CellMove(LastCell);
			}
		}

		private HashSet<KeyCode> _keyPress = new HashSet<KeyCode>();
		private HashSet<KeyCode> _keyRelease = new HashSet<KeyCode>();

		private void KeyboardUpdate() {
			bool selectedCell = currentSelectedCell != null
			&& Ui.TryGetTextInputInteractable(currentSelectedCell, out bool interactable) && interactable;

			if (selectedCell) {
				KeyMapUpdate(KeyMapCellSelected);
			} else {
				KeyMapUpdate(KeyMapNoCellSelected);
			}
		}

		private void KeyMapUpdate(Dictionary<KeyCode, Action> keyMapBlock) {
			float t = Time.deltaTime;
			foreach(var keyMap in keyMapBlock) {
				if (Input.GetKeyDown(keyMap.Key)) {
					_keyPress.Add(keyMap.Key);
					_keyPressDuration.SetDuration(keyMap.Key, 0);
				} else if (Input.GetKey(keyMap.Key)) {
					_keyPressDuration.AddDuration(keyMap.Key, t);
				} else if (Input.GetKeyUp(keyMap.Key)) {
					_keyRelease.Add(keyMap.Key);
				}
			}
			foreach (var kvp in _keyPressDuration.Ledger) {
				if (kvp.Value >= _keyHoldRepeatDuration) {
					_keyPress.Add(kvp.Key);
				}
			}
			foreach(KeyCode keyCode in _keyPress) {
				Action keyAction = keyMapBlock[keyCode];
				if (keyAction == null) { continue; }
				keyAction.Invoke();
				_keyPressDuration.AddDuration(keyCode, -_keyHoldRepeatDuration);
			}
			foreach (KeyCode keyCode in _keyRelease) {
				_keyPressDuration.ClearDuration(keyCode);
			}
			_keyPress.Clear();
			_keyRelease.Clear();
		}

		public bool MoveCurrentSelection(CellPosition delta) {
			CellRange all = AllRange;
			CellPosition newPosition = currentSelectionPosition + delta;
			if (!all.Contains(newPosition)) {
				return false;
			}
			SelectCell(null, newPosition);
			return true;
		}

		public void ScrollToSee(CellPosition position) {
			Vector2 ulCorner = new Vector2(columns[position.Column].xPosition, rows[position.Row].yPosition);
			Vector2 size = new Vector2(columns[position.Column].width, rows[position.Row].height);
			Rect nextRect = new Rect(ulCorner, size);
			//Debug.Log("nextrect " + nextRect);
			ScrollToSee(nextRect);
		}
	}
}
