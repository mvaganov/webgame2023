using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Spreadsheet {
	public static class Ui {
		public static Object GetTextObject(RectTransform rect) {
			InputField inf = rect.GetComponentInChildren<InputField>();
			if (inf != null) { return inf; }
			TMPro.TMP_InputField tmpinf = rect.GetComponentInChildren<TMPro.TMP_InputField>();
			if (tmpinf != null) { return tmpinf; }
			Text txt = rect.GetComponentInChildren<Text>();
			if (txt != null) { return txt; }
			TMPro.TMP_Text tmptxt = rect.GetComponentInChildren<TMPro.TMP_Text>();
			if (tmptxt != null) { return tmptxt; }
			return null;
		}

		public static UnityEvent<string> GetTextSubmitEvent(RectTransform rect) {
			InputField inf = rect.GetComponentInChildren<InputField>();
			if (inf != null) { return inf.onSubmit; }
			TMPro.TMP_InputField tmpinf = rect.GetComponentInChildren<TMPro.TMP_InputField>();
			if (tmpinf != null) { return tmpinf.onSubmit; }
			return null;
		}

		public static void SetText(Object rect, string text) {
			switch (rect) {
				case null: break;
				case GameObject go: SetText(go.GetComponent<RectTransform>(), text); break;
				case RectTransform rt: SetText(GetTextObject(rt), text); break;
				case InputField inf: inf.text = text; break;
				case Text txt: txt.text = text; break;
				case TMPro.TMP_InputField tmpinf: tmpinf.text = text; break;
				case TMPro.TMP_Text tmptxt: tmptxt.text = text; break;
				case MonoBehaviour mb: SetText(mb.GetComponent<RectTransform>(), text); break;
			}
		}

		public static Transform TransformFrom(object o) {
			switch (o) {
				case Transform t: return t;
				case GameObject go: return go.transform;
				case MonoBehaviour m: return m.transform;
			}
			Debug.Log("no transform for " + o);
			return null;
		}

		public static object GetPosition(object obj) => TransformFrom(obj)?.localPosition;

		public static object GetRotation(object obj) => TransformFrom(obj)?.localRotation;

		public static object GetName(object obj) {
			switch (obj) {
				case Object o: return o.name;
				case null: return null;
			}
			return obj.ToString();
		}

		public static Parse.Error SetName(object obj, object nameObj) {
			string name = nameObj.ToString();
			switch (obj) {
				case Object o: o.name = name; return null;
			}
			string errorMessage = $"Could not set {obj}.name = \"{nameObj}\"";
			Debug.LogError(errorMessage);
			return new Parse.Error(errorMessage);
		}

		public static Parse.Error SetPosition(object obj, object positionObj) {
			Transform t = Ui.TransformFrom(obj);
			float[] floats = new float[3];
			Parse.Error err = Parse.ConvertFloatsList(positionObj, ref floats);
			if (Parse.IsError(err)) {
				return err;
			}
			Vector3 newPosition = new Vector3(floats[0], floats[1], floats[2]);
			//Debug.Log($"{t.name} position: {t.localPosition} -> {newPosition}");
			t.localPosition = newPosition;
			return err;
		}

		public static Parse.Error SetRotation(object obj, object rotationObj) {
			Transform t = Ui.TransformFrom(obj);
			float[] floats = new float[4];
			Parse.Error err = Parse.ConvertFloatsList(rotationObj, ref floats);
			t.localRotation = new Quaternion(floats[0], floats[1], floats[2], floats[3]);
			return err;
		}
	}
}
