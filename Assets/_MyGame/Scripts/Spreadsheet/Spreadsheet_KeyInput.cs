using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spreadsheet {
	public partial class Spreadsheet {
		private static DurationTracker<KeyCode> _keyPressDuration = new DurationTracker<KeyCode>();
		[SerializeField]
		private float _keyHoldRepeatDuration = 0.125f;
		private Dictionary<KeyCode, Action> _keyMap;
		private Dictionary<KeyCode, Action> KeyMap => _keyMap != null ? _keyMap :
			_keyMap = new Dictionary<KeyCode, Action>() {
			[KeyCode.UpArrow] = () => CellMove((-1, 0)),
			[KeyCode.LeftArrow] = () => CellMove((0, -1)),
			[KeyCode.DownArrow] = () => CellMove((1, 0)),
			[KeyCode.RightArrow] = () => CellMove((0, 1)),
			[KeyCode.W] = () => CellMove((-1, 0)),
			[KeyCode.A] = () => CellMove((0, -1)),
			[KeyCode.S] = () => CellMove((1, 0)),
			[KeyCode.D] = () => CellMove((0, 1)),
			[KeyCode.Return] = () => {
				if (currentSelectedCell != null && Ui.TryGetTextInputInteractable(currentSelectedCell, out bool isInteractable)) {
					currentSelectedCell.ToggleInteractable();
					if (currentSelectedCell.Interactable) {
						currentSelectedCell.Select();
					}
				}
			}
		};

		private void CellMove(CellPosition direction) {
			if (MoveCurrentSelection(direction)) {
				ScrollToSee(currentSelectionPosition);
			}
		}

		private HashSet<KeyCode> _keyPress = new HashSet<KeyCode>();
		private HashSet<KeyCode> _keyRelease = new HashSet<KeyCode>();

		private void KeyboardUpdate() {
			if (currentSelectedCell != null && Ui.TryGetTextInputInteractable(currentSelectedCell, out bool interactable) && interactable) {
				return;
			}
			float t = Time.deltaTime;
			foreach(var keyMap in KeyMap) {
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
				KeyMap[keyCode].Invoke();
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
