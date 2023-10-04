using System.Collections.Generic;
using UnityEngine;

namespace MyGame {
	public class TransformSpreadsheet : Spreadsheet {

		public Transform GetT(object o) {
			switch (o) {
				case Transform t: return t;
				case GameObject go: return go.transform;
				case MonoBehaviour m: return m.transform;
			}
			Debug.Log("no transform for " + o);
			return null;
		}

		public new object GetName(object obj) => base.GetName(obj);
		public object GetPosition(object obj) => GetT(obj)?.position;
		public object GetRotation(object obj) => GetT(obj)?.rotation;
		public object GetParentName(object obj) {
			Transform transform = GetT(obj);
			return (transform != null && transform.parent != null) ? transform.parent.name : null;
		}

		[ContextMenuItem(nameof(Refresh), nameof(Refresh))]
		public List<Object> _objects = new List<Object>();
		private System.Array internalArray;

		public override System.Array Objects {
			get => GetObjects(_objects, ref internalArray);
			set => SetObjects(_objects, value);
		}

		public override void Refresh() {
			columns.Clear();
			columns.AddRange(new Column[] {
			new Column{ label = "Name", width = 100, GetData = GetName },
			new Column{ label = "Position", width = 100, GetData = GetPosition },
			new Column{ label = "Rotation", width = 100, GetData = GetRotation },
			new Column{ label = "Parent", width = 100, GetData = GetParentName },
		});
			base.Refresh();
		}

		private void Start() {
			Refresh();
		}
	}
}
