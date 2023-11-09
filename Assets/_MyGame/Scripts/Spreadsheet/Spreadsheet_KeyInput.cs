using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spreadsheet {
	public partial class Spreadsheet {
		private static readonly Dictionary<KeyCode, CellPosition> keyDirectionMap = new Dictionary<KeyCode, CellPosition>() {
			[KeyCode.UpArrow] = new CellPosition(-1, 0),
			[KeyCode.LeftArrow] = new CellPosition(0, -1),
			[KeyCode.DownArrow] = new CellPosition(1, 0),
			[KeyCode.RightArrow] = new CellPosition(0, 1),
		};

		private void KeyboardUpdate() {
			if (currentSelectedCell != null && Ui.TryGetTextInputInteractable(currentSelectedCell, out bool interactable) && interactable) {
				return;
			}
			foreach(KeyValuePair<KeyCode, CellPosition> keyDir in keyDirectionMap) {
				if (Input.GetKeyDown(keyDir.Key)) {
					MoveCurrentSelection(keyDir.Value);
				}
			}
			if (Input.GetKeyDown(KeyCode.Return)) {
				if (currentSelectedCell != null && Ui.TryGetTextInputInteractable(currentSelectedCell, out bool isInteractable)) {
					currentSelectedCell.ToggleInteractable();
				}
			}
		}

		public void MoveCurrentSelection(CellPosition delta) {
			CellRange all = AllRange;
			CellPosition newPosition = currentSelectionPosition + delta;
			if (all.Contains(newPosition)) {
				SelectCell(null, newPosition);
			} else {
				Debug.Log("cursor oob");
			}
		}
  }
}
