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

		public object GetPosition(object obj) => GetT(obj)?.position;
		public object GetRotation(object obj) => GetT(obj)?.rotation;
		public object GetParentName(object obj) {
			Transform transform = GetT(obj);
			return (transform != null && transform.parent != null) ? transform.parent.name : null;
		}
		public Parse.Error SetPosition(object obj, object positionObj) {
			Transform t = GetT(obj);
			float[] floats = new float[3];
			Parse.Error err = Parse.ConvertFloatsList(positionObj, ref floats);
			t.position = new Vector3(floats[0], floats[1], floats[2]);
			return err;
		}
		public Parse.Error SetRotation(object obj, object rotationObj) {
			Transform t = GetT(obj);
			float[] floats = new float[4];
			Parse.Error err = Parse.ConvertFloatsList(rotationObj, ref floats);
			t.rotation = new Quaternion(floats[0], floats[1], floats[2], floats[3]);
			return err;
		}
		public Parse.Error SetParentName(object obj, object nameObj) {
			Transform transform = GetT(obj);
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
			new Column{ label = "Name", width = 100, GetData = GetName, SetData = SetName },
			new Column{ label = "Position", width = 100, GetData = GetPosition, SetData = SetPosition },
			new Column{ label = "Rotation", width = 100, GetData = GetRotation, SetData = SetRotation },
			new Column{ label = "Parent", width = 100, GetData = GetParentName, SetData = SetParentName },
		});
			base.Refresh();
		}

		private void Start() {
			Refresh();
		}
	}
}
