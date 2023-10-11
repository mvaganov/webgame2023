using System.Collections.Generic;
using UnityEngine;

namespace Spreadsheet {
	public static class Parse {
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
			public Error(string error, string str, int line, int letter) {
				err = error; this.str = str; this.line = line; this.letter = letter;
			}
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
		public static Parse.Error ParseText(string text, ref int index, List<object> out_tokens, System.Func<char, bool> isFinished) {
			int tokenStart = -1, tokenEnd = -1;
			bool readingDigits = false;
			bool readingFloat = false;
			bool readingToken = false;
			List<char> parenthesisNesting = new List<char>();
			int loopguard = 0;
			while (index < text.Length) {
				if (loopguard++ > 1000) {
					throw new System.Exception("loop broken! "+index+" @ "+text.Substring(0, index)+"|"+text.Substring(index));
				}
				char c = text[index];
				if (IsWhiteSpace(c)) {
					if (tokenStart >= 0) {
						tokenEnd = index;
					} else {
						++index;
						continue;
					}
				} else if (IsDigit(c)) {
					if (readingDigits || readingToken) {
						++index;
						continue;
					} else if (tokenStart < 0) {
						tokenStart = index;
						readingDigits = true;
					}
				} else if (IsLetter(c)) {
					if (readingDigits) {
						tokenEnd = index;
						readingToken = true;
					} else if (readingToken) {
						++index;
						continue;
					}
				} else if (IsComma(c)) {
					if (tokenStart < 0) {
						tokenStart = index;
					}
					tokenEnd = index;
				} else {
					int last = parenthesisNesting.Count - 1;
					if (last >= 0 && parenthesisNesting[last] == c) {
						parenthesisNesting.RemoveAt(last);
						if (parenthesisNesting.Count == 0 && isFinished != null && isFinished.Invoke(c)) {
							++index;
							return null;
						}
					} else if (!readingToken && IsDecimalPoint(c) && !readingFloat) {
						readingFloat = true;
						++index;
						continue;
					} else if (IsSign(c) && !readingDigits && !readingToken) {
						char nextChar = ((index + 1) < text.Length) ? text[index + 1] : '\0';
						if (IsDigit(nextChar)) {
							readingDigits = true;
							++index;
							continue;
						}
					}
					char expectedFinish = EndCap(c);
					if (expectedFinish != '\0') {
						parenthesisNesting.Add(expectedFinish);
					}
				}
				if (tokenEnd >= 0) {
					string token = text.Substring(tokenStart, tokenEnd - tokenStart);
					if (readingDigits) {
						double number = double.Parse(token);
						out_tokens.Add(number);
					} else {
						out_tokens.Add(token);
					}
					tokenEnd = -1;
					continue;
				}
				++index;
			}
			return null;
		}
		public static bool IsWhiteSpace(char c) => c switch { ' ' => true, '\t' => true, '\n' => true, '\b' => true, _ => false };
		public static bool IsDigit(char c) => c switch { '0' => true, '1' => true, '2' => true, '3' => true, '4' => true, '5' => true, '6' => true, '7' => true, '8' => true, '9' => true, _ => false };
		public static bool IsSign(char c) => c switch { '-' => true, '+' => true, _ => false };
		public static bool IsDecimalPoint(char c) => c switch { '.' => true, _ => false };
		public static bool IsComma(char c) => c switch { ',' => true, _ => false };
		public static bool IsLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
		public static char EndCap(char c) => c switch { '[' => ']', '(' => ')', '{' => '}', '<' => '>', '\'' => '\'', '\"' => '\"', _ => '\0' };
	}
}
