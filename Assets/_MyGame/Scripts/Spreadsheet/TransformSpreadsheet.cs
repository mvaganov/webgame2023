using Spreadsheet;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame {
	public class TransformSpreadsheet : Spreadsheet.Spreadsheet {
		[ContextMenuItem(nameof(InitializeData), nameof(InitializeData))]
		[ContextMenuItem(nameof(RefreshCells), nameof(RefreshCells))]
		public List<Object> _objects = new List<Object>();
		private System.Array internalArray;

		public override System.Array Objects {
			get => GetObjects(_objects, ref internalArray);
			set {
				internalArray = null;
				SetObjects(_objects, value);
			}
		}

		public object GetParentName(object obj) {
			Transform transform = Ui.TransformFrom(obj);
			return (transform != null && transform.parent != null) ? transform.parent.name : null;
		}

		public Parse.Error SetParentName(object obj, object nameObj) {
			Transform transform = Ui.TransformFrom(obj);
			if (transform == null) {
				return new Parse.Error($"{obj} has no Transform");
			}
			Transform parent = transform != null ? transform.parent : null;
			if (parent != null) {
				parent.name = nameObj.ToString();
				return null;
			}
			return new Parse.Error("No parent");
		}

		public override void InitializeData() {
			columns.Clear();
			columns.AddRange(new Column[] {
				new Column{ label = "Name", width = 100, GetData = Ui.GetName, SetData = Ui.SetName },
				new Column{ label = "Position", width = 100, GetData = Ui.GetPosition, SetData = Ui.SetPosition },
				new Column{ label = "Rotation", width = 100, GetData = Ui.GetRotation, SetData = Ui.SetRotation },
				new Column{ label = "Parent", width = 100, GetData = GetParentName, SetData = SetParentName },
			});
			base.InitializeData();
		}
	}
}
