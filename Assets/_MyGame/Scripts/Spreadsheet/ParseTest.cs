using Spreadsheet;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParseTest : MonoBehaviour {
	[TextArea(1, 10)]
	public string text = "this\nis a test\nof multiline\nstring parsing";
	public int textIndex = 0;
	public bool doLineLetterTest = false;
	public void DoTest() {
		string testString = "1, 3, 5 [4, 3, 1, 5]  1, \"c\\nat\", 1.2, 3";
		List<object> tokens = new List<object>();
		int index = 0;
		Parse.Error err = Parse.ParseList(testString, ref index, tokens, null);
		string output = Parse.Debug(tokens);
		Debug.Log(output);
		float[] result = null;
		Parse.ConvertFloatsList("1, 2, 3, 5", ref result);
		Debug.Log(Parse.Debug(result));
	}

	public void DoAnotherTest() {
		Parse.GetLineLetterFromIndex(text, textIndex, out int line, out int letter);
		Debug.Log($"{textIndex} -> {line}:{letter}");
	}
	private void OnValidate() {
		if (doLineLetterTest) {
			DoAnotherTest();
		}
	}
	private void Reset() {
		DoTest();
	}
	void Start() {
	}

	// Update is called once per frame
	void Update() {

	}
}
