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
					if (MoveCurrentSelection(keyDir.Value)) {
						ScrollToSee(currentSelectionPosition);
					}
				}
			}
			if (Input.GetKeyDown(KeyCode.Return)) {
				if (currentSelectedCell != null && Ui.TryGetTextInputInteractable(currentSelectedCell, out bool isInteractable)) {
					currentSelectedCell.ToggleInteractable();
					if (currentSelectedCell.Interactable) {
						currentSelectedCell.Select();
					}
				}
			}
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
