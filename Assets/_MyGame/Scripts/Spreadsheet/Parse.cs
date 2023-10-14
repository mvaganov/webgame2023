using System;
using System.Collections.Generic;
using System.Text;
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
			public override string ToString() => err;
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
					Parse.Error err = ParseFloatList(s, out List<float> out_numbers);
					if (result == null || result.Length != out_numbers.Count) {
						result = out_numbers.ToArray();
					} else {
						for(int i = 0; i < out_numbers.Count; ++i) {
							result[i] = out_numbers[i];
						}
					}
					return err;
			}
			return new Parse.Error($"unable to parse type {value.GetType()}");
		}
		public static Parse.Error ParseList(string text, out object result) {
			int index = 0;
			List<object> out_tokens = new List<object>();
			Parse.Error err = ParseList(text, ref index, out_tokens, null);
			if (out_tokens.Count == 1) {
				result = out_tokens[0];
			}
			result = out_tokens;
			return err;
		}
		public static Parse.Error ParseFloatList(string text, out List<float> list) {
			return ParseList(text, out list, Convert.ToSingle);
		}
		public static Parse.Error ParseList<T>(string text, out List<T> list, Func<object, T> conversion) {
			int index = 0;
			List<object> out_tokens = new List<object>();
			Parse.Error err = ParseList(text, ref index, out_tokens, null);
			list = new List<T>();
			for(int i = 0; i < out_tokens.Count; ++i) {
				list.Add(conversion.Invoke(out_tokens[i]));
			}
			return err;
		}
		public static Parse.Error ParseList(string text, ref int index, List<object> out_tokens, Func<char, bool> isFinished) {
			int tokenStart = -1, tokenEnd = -1;
			bool readingDigits = false;
			bool readingFloat = false; // found a decimal point. don't let another decimal point go!
			bool readingToken = false;
			char readingStringLiteral = '\0';
			List<char> parenthesisNesting = new List<char>();
			int loopguard = 0;
			int startedParse = index;
			bool finishedEarly = false;
			while (index < text.Length && !finishedEarly) {
				if (loopguard++ > 1000) {
					throw new Exception("loop broken! "+index+" @ "+text.Substring(0, index)+"|"+text.Substring(index));
				}
				char c = text[index];
				if (readingStringLiteral != '\0') {
					if (c == '\\') {
						++index;
						if (index >= text.Length) {
							return new Parse.Error($"escape sequence missing next letter @ {index-1}", text, 0, index);
						}
					} else if(c == readingStringLiteral) {
						tokenEnd = index;
						//UnityEngine.Debug.Log($"\"token {tokenStart}:{tokenEnd}\"{text.Substring(tokenStart, tokenEnd - tokenStart)}\"");
						Parse.Error err = Unescape(text, out string resultToken, tokenStart, tokenEnd);
						out_tokens.Add(resultToken);
						return err;
					}
					++index;
					if (index > text.Length) {
						return new Parse.Error($"missing {readingStringLiteral} for unfinished string literal @ {tokenStart}", text, 0, tokenStart);
					}
					continue;
				} else if (IsWhiteSpace(c)) {
					if (tokenStart >= 0) {
						tokenEnd = index;
						//UnityEngine.Debug.Log($"   token {tokenStart}:{tokenEnd}\"{text.Substring(tokenStart, tokenEnd - tokenStart)}\"");
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
						//UnityEngine.Debug.Log($"num->char token {tokenStart}:{tokenEnd}\"{text.Substring(tokenStart, tokenEnd - tokenStart)}\"");
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
					//UnityEngine.Debug.Log($", token {tokenStart}:{tokenEnd}\"{text.Substring(tokenStart, tokenEnd - tokenStart)}\"");
					++index;
				} else {
					int last = parenthesisNesting.Count - 1;
					char currentEnclosureFinish = last >= 0 ? parenthesisNesting[last] : '\0';
					if (currentEnclosureFinish == c) {
						parenthesisNesting.RemoveAt(last);
						tokenEnd = index;
						if (parenthesisNesting.Count == 0 && isFinished != null && isFinished.Invoke(c)) {
							//UnityEngine.Debug.Log($"{c} token {tokenStart}:{tokenEnd}\"{text.Substring(tokenStart, tokenEnd - tokenStart)}\"");
							finishedEarly = true;
						}
					} else if (!readingToken && IsDecimalPoint(c) && !readingFloat) {
						readingFloat = true;
						++index;
						continue;
					} else if (IsSign(c) && !readingDigits && !readingToken) {
						char nextChar = ((index + 1) < text.Length) ? text[index + 1] : '\0';
						//UnityEngine.Debug.Log($"Sign {c}, next is {nextChar}");
						if (IsDigit(nextChar) || (readingFloat = IsDecimalPoint(nextChar))) {
							tokenStart = index;
							readingDigits = true;
							++index;
							continue;
						}
					}
					char expectedFinish = EndCap(c);
					if (expectedFinish != '\0') {
						parenthesisNesting.Add(expectedFinish);
						if (startedParse != index) {
							List<object> tokens = new List<object>();
							Parse.Error error = ParseList(text, ref index, tokens, c => c == expectedFinish);
							c = text[index];
							object whatToAdd = IsStringLiteralCap(expectedFinish) ? tokens[0] : tokens;
							out_tokens.Add(whatToAdd);
							if (c != expectedFinish) {
								throw new Exception($"expected {expectedFinish} @ {index}, found {c}");
							}
							parenthesisNesting.RemoveAt(parenthesisNesting.Count-1);
							++index;
						} else {
							if (IsStringLiteralCap(expectedFinish)) {
								readingStringLiteral = expectedFinish;
								tokenStart = ++index;
								continue;
							}
						}
					}
				}
				if(tokenStart >= 0 && tokenEnd < 0 && index >= text.Length) {
					tokenEnd = text.Length;
					//UnityEngine.Debug.Log($"EOF token {tokenStart}:{tokenEnd}\"{text.Substring(tokenStart, tokenEnd - tokenStart)}\"");
				}
				if (tokenEnd >= 0) {
					string token = text.Substring(tokenStart, tokenEnd - tokenStart);
					if (readingDigits) {
						//UnityEngine.Debug.Log($"double parse '{token}'<-------------");
						//string hey = "";
						//for(int i = 0; i < token.Length; ++i) { hey += "[" + ((int)token[i]) + "]"; }
						//UnityEngine.Debug.Log(hey);
						double number = double.Parse(token);
						out_tokens.Add(number);
					} else {
						out_tokens.Add(token);
					}
					tokenStart = -1;
					tokenEnd = -1;
					readingDigits = false;
					readingFloat = false;
					readingToken = false;
					readingStringLiteral = '\0';
					continue;
				}
				++index;
			}
			//UnityEngine.Debug.Log($"###### token count: {out_tokens.Count}   {index} < {text.Length}");
			return null;
		}

		public static string Debug(object obj) {
			StringBuilder sb = new StringBuilder(); 
			switch (obj) {
				case null:
					sb.Append("null");
					break;
				case string str:
					sb.Append("\"").Append(Escape(str)).Append("\"");
					break;
				case System.Collections.IEnumerable enumerable:
					sb.Append("[");
					int count = 0;
					foreach (object item in enumerable) {
						if (count > 0) { sb.Append(", "); }
						sb.Append(Debug(item));
						++count;
					}
					sb.Append("]");
					break;
				default:
					sb.Append(obj.ToString());
					break;
			}
			return sb.ToString();
		}
		public static bool IsWhiteSpace(char c) => c switch { ' ' => true, '\t' => true, '\n' => true, '\b' => true, _ => false };
		public static bool IsDigit(char c) => c >= '0' && c <= '9';
		public static bool IsDigitOctal(char c) => c >= '0' && c <= '7';
		public static bool IsDigitHexadecimal(char c) => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');
		public static bool IsSign(char c) => c switch { '-' => true, '+' => true, _ => false };
		public static bool IsDecimalPoint(char c) => c switch { '.' => true, _ => false };
		public static bool IsComma(char c) => c switch { ',' => true, _ => false };
		public static bool IsLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
		public static bool IsStringLiteralCap(char c) => c switch { '\'' => true, '\"' => true, _ => false };
		public static char EndCap(char c) => c switch { '[' => ']', '(' => ')', '{' => '}', '<' => '>', '\'' => '\'', '\"' => '\"', _ => '\0' };
		public static char LiteralUnescape(char c) => c switch { 'a' => '\a', 'b' => '\b', 'n' => '\n', 'r' => '\r', 'f' => '\f', 't' => '\t', 'v' => '\v', _ => c };
		public static string LiteralEscape(char c) => c switch { '\a' => "\\a", '\b' => "\\b", '\n' => "\\n", '\r' => "\\r", '\f' => "\\f", '\t' => "\\t", '\v' => "\\v", _ => null };
		public static string Escape(string str) {
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < str.Length; ++i) {
				char c = str[i];
				string escaped = LiteralEscape(c);
				if (escaped != null) {
					sb.Append(escaped);
				} else {
					if (c < 32 || (c > 127 && c < 512)) {
						sb.Append("\\").Append(Convert.ToString((int)c, 8));
					} else if (c >= 512) {
						sb.Append("\\u").Append(((int)c).ToString("X4"));
					} else {
						sb.Append(c);
					}
				}
			}
			return sb.ToString();
		}
		public static Parse.Error Unescape(string str, out string unescaped, int start = 0, int end = -1) {
			StringBuilder sb = new StringBuilder();
			int stringStarted = start;
			if (end < 0) {
				end = str.Length;
			}
			for (int i = start; i < end; ++i) {
				char c = str[i];
				if (c == '\\') {
					if (stringStarted != i) {
						sb.Append(str.Substring(stringStarted, i - stringStarted));
					}
					++i;
					if (i >= str.Length) { break; }
					c = str[i];
					if (IsDigitOctal(c)) {
						int digitStart = i;
						while (IsDigitOctal(str[i+1])) {
							++i;
						}
						string octalDigits = str.Substring(digitStart, i - digitStart + 1);
						int octalValue = Convert.ToInt32(octalDigits, 8);
						sb.Append((char)octalValue);
					} else if (c == 'u') {
						if (i+4 > end) {
							unescaped = sb.ToString();
							return new Parse.Error($"expected 4 chars after index {i}, found {end - i}", str, 0, i);
						}
						string octalDigits = str.Substring(i, 4);
						i += 3;
						int hexValue = Convert.ToInt32(octalDigits, 16);
						sb.Append((char)hexValue);
					} else {
						c = LiteralUnescape(str[i]);
						sb.Append(c);
					}
					stringStarted = i + 1;
				}
			}
			sb.Append(str.Substring(stringStarted, end - stringStarted));
			unescaped = sb.ToString();
			return null;
		}
	}
}
