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
	}
}
