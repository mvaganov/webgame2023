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
			RectTransform shouldRefreshTextArea = null;
			switch (rect) {
				case null: break;
				case GameObject go: SetText(go.GetComponent<RectTransform>(), text); break;
				case RectTransform rt: SetText(GetTextObject(rt), text); break;
				case InputField inf: inf.text = text; shouldRefreshTextArea = inf.GetComponent<RectTransform>(); break;
				case Text txt: txt.text = text; shouldRefreshTextArea = txt.GetComponent<RectTransform>(); break;
				case TMPro.TMP_InputField tmpinf: tmpinf.text = text; shouldRefreshTextArea = tmpinf.GetComponent<RectTransform>(); break;
				case TMPro.TMP_Text tmptxt: tmptxt.text = text; shouldRefreshTextArea = tmptxt.GetComponent<RectTransform>(); break;
				case MonoBehaviour mb: SetText(mb.GetComponent<RectTransform>(), text); break;
			}
			if (shouldRefreshTextArea != null) {
				LayoutRebuilder.ForceRebuildLayoutImmediate(shouldRefreshTextArea);
			}
		}

		public static void SetColor(Object obj, Color color) {
			Object colorObject = GetColorObject(obj);
			switch (colorObject) {
				case null: break;
				case Image img: img.color = color; break;
				case Text txt: txt.color = color; break;
				case TMPro.TMP_Text tmptxt: tmptxt.color = color; break;
				case InputField inf: {
						ColorBlock cb = inf.colors;
						cb.normalColor = color;
						inf.colors = cb;
					}
					break;
				case TMPro.TMP_InputField tinf: {
						ColorBlock cb = tinf.colors;
						cb.normalColor = color;
						tinf.colors = cb;
					}
					break;
				case Renderer r: r.material.color = color; break;
				case Material m: m.color = color; break;
			}
		}

		public static bool TryGetColor(Object obj, out Color color) {
			Object colorObject = GetColorObject(obj);
			switch (colorObject) {
				case Image img: color = img.color; return true;
				case Text txt: color = txt.color; return true;
				case TMPro.TMP_Text tmptxt: color = tmptxt.color; return true;
				case InputField inf: color = inf.colors.normalColor; return true;
				case TMPro.TMP_InputField tinf: color = tinf.colors.normalColor; return true;
				case Renderer r: color = r.material.color; return true;
				case Material m: color = m.color; return true;
			}
			color = Color.clear;
			return false;
		}

		public static Object GetColorObject(Object obj) {
			switch (obj) {
				case Image i: return i;
				case Renderer r: return r;
				case Material m: return m;
				case GameObject go:
					Image img = go.GetComponent<Image>();
					if (img != null) { return img; }
					RectTransform rt = go.GetComponent<RectTransform>();
					if (rt != null) {
						Object textUi = GetTextObject(rt);
						if (textUi != null) {
							return textUi;
						}
					}
					Renderer rend = go.GetComponent<Renderer>();
					if (rend != null) { return rend; }
					return null;
				case Transform t: return GetColorObject(t.gameObject);
				case MonoBehaviour mb: return GetColorObject(mb.gameObject);
			}
			return null;
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
				case UnityEngine.Object o: o.name = name; return null;
			}
			string errorMessage = $"Could not set {obj}.name = \"{nameObj}\"";
			Debug.Log(errorMessage);
			return new Parse.Error(errorMessage);
		}

		public static Parse.Error SetPosition(object obj, object positionObj) {
			Transform t = Ui.TransformFrom(obj);
			Parse.Error err = Parse.ParseVector3(positionObj, out Vector3 newPosition);
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
