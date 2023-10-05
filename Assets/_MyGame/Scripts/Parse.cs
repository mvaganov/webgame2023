using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Parse
{
	public class Error {
		/// <summary>
		/// If this is null, then this is not actually an error, the parse info (str, line, letter) are meaningful.
		/// </summary>
		public string err;
		public string str;
		public int line, letter;
		public object metadata;
		public bool IsError => err != null;
		public Error(string error) { err = error; }

	}
	public static Parse.Error ConvertFloatsList(object value, ref float[] result) {
		switch (value) {
			case float f:
				result[0] = f;
				return null;
			case Vector2 v2:
				result[0] = v2.x;
				result[1] = v2.y;
				return null;
			case Vector3 v3:
				result[0] = v3.x;
				result[1] = v3.y;
				result[2] = v3.z;
				return null;
			case Quaternion q:
				result[0] = q.x;
				result[1] = q.y;
				result[2] = q.z;
				result[3] = q.w;
				return null;
			case float[] floats:
				if (result == null) {
					result = new float[floats.Length];
				}
				int limit = Mathf.Min(result.Length, floats.Length);
				for (int i = 0; i < limit; ++i) {
					result[i] = floats[i];
				}
				return null;
			case null:
				return new Parse.Error($"null unacceptable");
			case string s:
				// TODO possibly strip one layer of braces (, [, {
				// read 3 floating point numbers
				// skip any delimeters
				return new Parse.Error("string parse not yet implemented");
		}
		return new Parse.Error($"unable to parse type {value.GetType()}");
	}
	public static Parse.Error ParseText(string text, ref int index, List<object> out_tokens, System.Func<char,bool> isFinished) {
		char expectedFinish = '\0';
		int tokenStart = -1, tokenEnd = -1;
		int addedElements = 0;
		bool addingString = false;
		bool readingWhitespace = false;
		List<char> letters = new List<char>();
		while (index < text.Length) {
			char c = text[index];
			switch (c) {
				case '[': case '(': case '{': case '<':
					expectedFinish = EndCap(c);

					if (tokenStart >= 0 && tokenEnd < 0) {
						tokenEnd = index;
					}
					break;
				case '\'': case '\"':
					addingString = true;
					break;
				case ' ':
				case '\t':
				case '\n':
				case '\r':

					break;
				case '-':
				case '.':
				case '0': case '1': case '2': case '3': case '4': case '5': case '6': case '7': case '8': case '9':
					if (tokenStart < 0) {
						tokenStart = index;
					}
					break;
				default:
					break;
			}
			++index;
		}
		return null;
	}
	public static bool IsDigit(char c)  => (c >= '0' && c <= '9');
	public static bool IsSign(char c) => c == '-' || c == '+';
	public static bool IsDecimalPoint(char c) => c >= '.' || c == ',';
	public static char EndCap(char c) {
		switch (c) {
			case '[': return ']';
			case '(': return ')';
			case '{': return '}';
			case '<': return '>';
			case '\'': return '\'';
			case '\"': return '\"';
		}
		return '\0';
	}
}
