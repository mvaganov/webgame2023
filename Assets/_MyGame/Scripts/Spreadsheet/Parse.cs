using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Spreadsheet {
	public static class Parse {
		public class Error {
			/// <summary>
			/// If this is null, then this is not actually an error, the parse info (str, line, letter) are meaningful.
			/// </summary>
			public string err;
			public string str;
			public int index;
			public int line, letter;
			public object metadata;
			public bool IsError => err != null;
			public Error(string error) { err = error; }
			public Error(string error, string str, int index, short line, short letter) {
				err = error; this.str = str; this.index = index;
			}
			public Error(string error, string str, int index) {
				err = error; this.str = str; this.index = index;
				GetLineLetterFromIndex(str, index, out line, out letter);
			}
			public override string ToString() => err;
		}

		public static bool IsError(Error err) => err != null && err.IsError;

		/// <summary>
		/// for identifying string literal tokens in a parsed lexical tree
		/// </summary>
		public struct StringLiteral {
			public string token;
			public int sourceIndex;
			public StringLiteral(string token, int index) { this.token = token; this.sourceIndex = index; }
			public override string ToString() => token;
			public int Length => token.Length;
			public char this[int i] { get { return token[i]; } }
			public static implicit operator string(StringLiteral token) => token.ToString();
		}

		/// <summary>
		/// for identifying non-string-literal tokens in a parsed lexical tree
		/// </summary>
		public struct Token {
			public string token;
			public int sourceIndex;
			public Token(string token, int index) { this.token = token; this.sourceIndex = index; }
			public override string ToString() => token;
			public int Length => token.Length;
			public char this[int i] { get { return token[i]; } }
			public static implicit operator string(Token token) => token.ToString();
		}

		public struct TokenList : IEnumerable, IEnumerable<object>, IList<object> {
			public List<object> list;
			public int sourceIndex;
			public TokenList(List<object> list, int index) { this.list = list; this.sourceIndex = index; }
			public int Count => list.Count;
			public bool IsReadOnly => false;
			object IList<object>.this[int index] { get { return list[index]; } set { list[index] = value; } }
			public object this[int i] { get { return list[i]; } set { list[i] = value; } }
			public static implicit operator string(TokenList list) => Debug(list);
			public IEnumerator GetEnumerator() => list.GetEnumerator();
			IEnumerator<object> IEnumerable<object>.GetEnumerator() => list.GetEnumerator();
			public int IndexOf(object item) => list.IndexOf(item);
			public void Insert(int index, object item) => list.Insert(index, item);
			public void RemoveAt(int index) => list.RemoveAt(index);
			public void Add(object item) => list.Add(item);
			public void Clear() => list.Clear();
			public bool Contains(object item) => list.Contains(item);
			public void CopyTo(object[] array, int arrayIndex) => list.CopyTo(array, arrayIndex);
			public bool Remove(object item) => list.Remove(item);
		}

		public static Parse.Error ParseVector3(object positionObj, out Vector3 v3) {
//Log($"parsing \"{positionObj}\"");
			float[] floats = null;
			switch (positionObj) {
				case Vector3 v: v3 = v; return null;
				case float[] f: floats = f; break;
			}
			Parse.Error err = null;
			if (floats == null) {
				floats = new float[3];
				err = Parse.ConvertFloatsList(positionObj, ref floats);
				if (Parse.IsError(err)) {
					v3 = Vector3.zero;
					return err;
				}
			}
			int floatCount = floats != null ? floats.Length : 0;
			if (floatCount < 3 && (err == null || !err.IsError)) {
				string errorMessage = $"Expected 3 values, found {floatCount}";
				if (err == null) {
					err = new Parse.Error(errorMessage);
				} else {
					err.err = errorMessage;
				}
			}
			float x = GetNum(floats, 0, 0);
			float y = GetNum(floats, 1, 0);
			float z = GetNum(floats, 2, 0);
			v3 = new Vector3(x, y, z);
			return err;
			float GetNum(float[] list, int i, float v) => list.Length > i ? list[i] : v;
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
				case IList<float> floats:
					if (result == null) {
						result = new float[floats.Count];
					}
					int limit = Mathf.Min(result.Length, floats.Count);
					for (int i = 0; i < limit; ++i) {
						result[i] = floats[i];
					}
					return null;
				case null:
					return new Parse.Error($"null unacceptable");
				case string s:
					//UnityEngine.Debug.Log($"parsing string '{s}'");
					Parse.Error err = ParseFloatList(s, out List<float> out_numbers);
					//UnityEngine.Debug.Log($"parsed [{out_numbers.Count}] {{{string.Join(",", out_numbers)}}}");
					if (result == null || out_numbers == null || result.Length != out_numbers.Count) {
						if (out_numbers != null) {
							result = out_numbers.ToArray();
						}
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
			//if (out_tokens.Count == 1) {
			//	result = out_tokens[0];
			//}
			result = out_tokens;
			return err;
		}
		
		public static Parse.Error ParseFloatList(string text, out List<float> list) {
			return ParseList(text, out list, ConvertToSingle);
		}

		public static Parse.Error ConvertToSingle(object obj) {
			Parse.Error err = new Error(null);
			switch (obj) {
				case TokenList tokList:
					err.err = $"cannot convert TokenList({tokList.Count}) '{Debug(tokList)}' to float";
					break;
				case List<object> list:
					err.err = $"cannot convert List({list.Count}) '{Debug(list)}' to float";
					break;
				case Token:
					err.err = $"cannot convert Token '{obj}' to float";
					break;
				case string:
				case StringLiteral:
					string str = obj.ToString();
					if (!float.TryParse(str, out float parsed)) {
						err.err = $"could not convert {str.GetType()} '{str}' to float";
					}
					err.metadata = parsed;
					break;
				case float f: err.metadata = f; break;
				case double d: err.metadata = (float)d; break;
				case int i: err.metadata = (float)i; break;
				case long l: err.metadata = (float)l; break;
				case short b: err.metadata = (float)b; break;
				default:
					err.metadata = Convert.ToSingle(obj);
					break;
			}
			return err;
		}

		/// <summary>
		/// parses text into a list, and attempts to convert all elements into type T
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="text"></param>
		/// <param name="list"></param>
		/// <param name="conversion">should return a <see cref="Parse.Error"/>, where the meta data is of type T</param>
		/// <returns></returns>
		public static Parse.Error ParseList<T>(string text, out List<T> list, Func<object, Parse.Error> conversion) {
			int index = 0;
			List<object> out_tokens = new List<object>();
			Parse.Error err = ParseList(text, ref index, out_tokens, null);
//Log($"Parsing float list {text}: {string.Join(", ", out_tokens)}");
			if (IsError(err)) {
				list = null;
				return err;
			}
			//Log(DebugHierarchyWithTypes(out_tokens, 0));
			Parse.Error conversionError = ConvertList(text, out_tokens, out list, conversion);
			if (!IsError(err) && IsError(conversionError)) {
				err = conversionError;
			}
			return err;
		}

		private static Parse.Error ConvertList<T>(string text, List<object> out_tokens, out List<T> list, Func<object, Parse.Error> conversion) {
			list = new List<T>();
			Parse.Error err = null;
			for (int i = 0; i < out_tokens.Count; ++i) {
				//Log("converting '" + out_tokens[i] + "'   " + (GetTokenIndex(out_tokens[i], out int tokenIndex) ? "@" + tokenIndex : ""));
				Parse.Error conversionError = conversion.Invoke(out_tokens[i]);
				bool conversionHappenedCorrectly = conversionError != null && !conversionError.IsError;
				if (conversionHappenedCorrectly) {
					if (conversionError.metadata is T data) {
						list.Add(data);
					} else {
						conversionHappenedCorrectly = false;
					}
				} else if (conversionError != null) {
					ApplyErrorLocation(conversionError, text, out_tokens[i]);
					return conversionError;
				}
				if (!conversionHappenedCorrectly) {
					err = new Error($"could not convert token {i} ({out_tokens[i]} {out_tokens[i].GetType()}) into {typeof(T)} with {conversion.Method.Name}");
					ApplyErrorLocation(err, text, out_tokens[i]);
					return err;
				}
			}
			return err;
		}

		private static void ApplyErrorLocation(Error error, string rootText, object token) {
			if (GetTokenIndex(token, out int tokenIndex)) {
				error.index = tokenIndex;
				Parse.GetLineLetterFromIndex(rootText, tokenIndex, out error.line, out error.letter);
			}
		}

		public static bool GetTokenIndex(object obj, out int index) {
			switch (obj) {
				case Token token:
					index = token.sourceIndex;
					return true;
				case StringLiteral stringLiteral:
					index = stringLiteral.sourceIndex;
					return true;
				case TokenList tokList:
					index = tokList.sourceIndex;
					return true;
			}
			index = 0;
			return false;
		}

		/// <summary>
		/// element zero is the number of rows. each element after that is the index starting that row.
		/// [1] should always be zero, unless there is hidden header data.
		/// </summary>
		/// <param name="str"></param>
		/// <param name="lineIndexTable"></param>
		public static void CalculateLineIndexTable(string str, ref int[] lineIndexTable) {
			int lineCount = 1;
			for(int i = 0; i < str.Length; ++i) {
				if (str[i] == '\n') { ++lineCount; }
			}
			if (lineIndexTable.Length < lineCount) {
				lineIndexTable = new int[lineCount];
			}
			lineIndexTable[0] = lineCount;
			int line = 1;
			for (int i = 0; i < str.Length; ++i) {
				if (str[i] == '\n') { lineIndexTable[line++] = i; }
			}
		}

		public static void GetLineLetterFromIndex(int[] lineIndexTable, int index, out int line, out int letter) {
			line = Array.BinarySearch(lineIndexTable, 1, lineIndexTable[0]-1, index);
			if (line < 0) { line = (~line); }
			if (line == 1) {
				letter = index + 1;
			} else {
				letter = index - lineIndexTable[line - 1];
			}
		}

		private static Dictionary<Thread, int[]> _lineIndexCalculatorByThread = new Dictionary<Thread, int[]>();

		public static void GetLineLetterFromIndex(string text, int index, out int line, out int letter) {
			int[] lineIndexTable = GetThreadedLineIndexTable();
			CalculateLineIndexTable(text, ref lineIndexTable);
			_lineIndexCalculatorByThread[Thread.CurrentThread] = lineIndexTable;
			GetLineLetterFromIndex(lineIndexTable, index, out line, out letter);
			int[] GetThreadedLineIndexTable() {
				Thread t = Thread.CurrentThread;
				if (!_lineIndexCalculatorByThread.TryGetValue(t, out int[] table)) {
					table = _lineIndexCalculatorByThread[t] = new int[1];
				}
				ClearDeadThreadsHoldingIndexTables();
				return table;
				void ClearDeadThreadsHoldingIndexTables() {
					List<Thread> deadThreads = null;
					foreach (var kvp in _lineIndexCalculatorByThread) {
						if (!kvp.Key.IsAlive) {
							if (deadThreads == null) { deadThreads = new List<Thread>(); }
							deadThreads.Add(kvp.Key);
						}
					}
					if (deadThreads == null) { return; }
					for (int i = 0; i < deadThreads.Count; ++i) {
						_lineIndexCalculatorByThread.Remove(deadThreads[i]);
					}
				}
			}
		}
		private static void Log(string text) {
			UnityEngine.Debug.Log(text);
		}

		public static Parse.Error ParseList(string text, ref int index, List<object> out_tokens, Func<char, bool> isFinished) {
			int tokenStart = -1, tokenEnd = -1;
			bool readingDigits = false;
			bool readingFloat = false; // found a decimal point. don't let another decimal point go!
			bool readingOtherToken = false;
			bool IsReadingToken() => tokenStart >= 0;
			bool IsFinishedReadingToken() => tokenEnd >= 0;
			int TokenLength() => tokenEnd - tokenStart;
			string CurrentToken() => text.Substring(tokenStart, TokenLength());
			char readingStringLiteral = '\0';
			bool IsReadingStringLiteral() => readingStringLiteral != '\0';
			List<char> parenthesisNesting = new List<char>();
			int loopguard = 0;
			int startedParse = index;
			bool finishedEarly = false;
			bool finishedLastTokenElementWithComma = false;
			while (index < text.Length && !finishedEarly) {
				if (loopguard++ > 1000) {
					throw new Exception("loop broken! "+index+" @ "+text.Substring(0, index)+"|"+text.Substring(index));
				}
				char c = text[index];
//Log($"{c}@{index} d{readingDigits} f{readingFloat} t{readingOtherToken} s{readingStringLiteral != '\0'}");
				if (IsReadingStringLiteral()) {
					if (c == '\\') {
						++index;
						if (index >= text.Length) {
							return new Parse.Error($"escape sequence missing next letter", text, index - 1);
						}
					} else if (c == readingStringLiteral) {
						tokenEnd = index;
//Log($"\"token {tokenStart}:{tokenEnd}\"{CurrentToken()}\"");
						Parse.Error err = Unescape(text, out string resultToken, tokenStart, tokenEnd);
						out_tokens.Add(resultToken);
						return err;
					}
					++index;
					if (index > text.Length) {
						return new Parse.Error($"missing {readingStringLiteral} for unfinished string literal", text, tokenStart);
					}
//Log($"continue literal {index} < {text.Length}");
					continue;
				} else if (IsWhiteSpace(c)) {
					if (IsReadingToken()) {
						tokenEnd = index;
//Log($"   token {tokenStart}:{tokenEnd}\"{CurrentToken()}\"");
					} else {
						++index;
						if (index < text.Length) {
//Log($"continue whitespace {index} ({c}) < {text.Length}");
							continue;
						}
					}
				} else if (IsDigit(c)) {
					if (readingDigits || readingOtherToken) {
						++index;
						if (index < text.Length) {
//Log($"continue digit {index} < {text.Length}");
							continue;
						}
					} else if (!IsReadingToken()) {
						tokenStart = index;
						readingDigits = true;
					}
				} else if (IsLetter(c)) {
					if (readingDigits) {
						tokenEnd = index;
//Log($"num->char token {tokenStart}:{tokenEnd}\"{CurrentToken()}\"");
						readingOtherToken = true;
					} else if (readingOtherToken) {
						++index;
						if (index < text.Length) {
//Log($"continue token {index} < {text.Length}");
							continue;
						} else {
//Log("token finished?");
						}
					} else if (!IsReadingToken()) {
						tokenStart = index;
						readingOtherToken = true;
//Log("token started");
					}
				} else if (IsComma(c)) {
					bool emptyCommaNeedsEmptyToken = !IsReadingToken() && finishedLastTokenElementWithComma;
					if (emptyCommaNeedsEmptyToken) {
						tokenStart = index;
					}
					if (IsReadingToken()) {
						tokenEnd = index;
						++index;
					} else {
						finishedLastTokenElementWithComma = true;
					}
//string tokenBeforeComma = IsReadingToken() ? $"\"{CurrentToken()}\"" : "none";
//Log($", token {tokenStart}:{tokenEnd} {tokenBeforeComma}");
				} else {
					int last = parenthesisNesting.Count - 1;
					char currentEnclosureFinish = last >= 0 ? parenthesisNesting[last] : '\0';
//Log($"enclosure? {c} vs {currentEnclosureFinish}");
					if (currentEnclosureFinish == c) {
//Log("found end!");
						parenthesisNesting.RemoveAt(last);
						if (IsReadingToken()) {
//Log("finished at EXPECTED enclosure token");
							tokenEnd = index;
//	Log($"{c} token {tokenStart}:{tokenEnd} \"{CurrentToken()}\"");
//} else {
//	Log($"finished multi-token enclosure {c} {index} / {text.Length}");
						}
						if (parenthesisNesting.Count == 0 && (isFinished == null || isFinished.Invoke(c))) {
							finishedEarly = true;
						} else {
//Log($"NOT FINISHED EARLY??? ({string.Join(", ", parenthesisNesting)}), finished?{isFinished}");
						}
					} else if (!readingOtherToken && IsDecimalPoint(c)) {
						if (!readingFloat) {
							readingFloat = true;
						} else {
							return new Parse.Error("floating point with multple decimals not allowed", text, index);
						}
						++index;
						if (index < text.Length) {
//Log($"continue float {index} < {text.Length}");
							continue;
						}
					} else if (IsSign(c) && !readingDigits && !readingOtherToken) {
						char nextChar = ((index + 1) < text.Length) ? text[index + 1] : '\0';
//Log($"Sign {c}, next is {nextChar}");
						if (IsDigit(nextChar) || IsDecimalPoint(nextChar)) {
							tokenStart = index;
							readingDigits = true;
							++index;
							if (index < text.Length) {
//Log($"continue signed digit {index} < {text.Length}");
								continue;
							}
						}
					}
//Log("~~~~~~~~~~~~~~");
					char expectedFinish = EndCap(c);
					if (expectedFinish != '\0') {
						if (IsReadingToken()) {
//Log("finished at unexpected enclosure token");
							tokenEnd = index;
						}
//Log($"enclosure {c} expects finish with {expectedFinish}");
						parenthesisNesting.Add(expectedFinish);
						if (startedParse != index) {
							List<object> tokens = new List<object>();
							int startedNewListAt = index;
							Parse.Error error = ParseList(text, ref index, tokens, c => c == expectedFinish);
							if (IsError(error)) {
								return error;
							}
//Log($"new list started@{startedParse}, ended@{index} / {text.Length}    \"{text}\"");
							c = index < text.Length ? text[index] : '\0';
							object whatToAdd = IsStringLiteralCap(expectedFinish) ? tokens[0] :
								new TokenList(tokens, startedNewListAt);
							out_tokens.Add(whatToAdd);
							if (c != expectedFinish) {
								string whatIsHere = c != '\0' ? c.ToString() : "EndOfText";
								string errorMessage = $"expected '{expectedFinish}', found '{whatIsHere}'";
								return new Parse.Error(errorMessage, text, index);
							}
							parenthesisNesting.RemoveAt(parenthesisNesting.Count - 1);
							//++index;
						} else {
							if (IsStringLiteralCap(expectedFinish)) {
								readingStringLiteral = expectedFinish;
								tokenStart = ++index;
								if (index < text.Length) {
//Log($"literal started {index} < {text.Length}");
									continue;
								}
							}
						}
					}
//Log("$$$$$$$$$$$$$$$ "+ finishedEarly);
					if (!finishedEarly) {
						char unexpectedFinish = BeginCap(c);
						if (unexpectedFinish != '\0') {
							return new Parse.Error($"unexpected end of enclosure '{unexpectedFinish}'", text, index);
						}
					}
				}
				if(IsReadingToken() && !IsFinishedReadingToken() && index >= text.Length-1) {
					tokenEnd = text.Length;
					finishedEarly = true;
//Log($"EOF token {tokenStart}:{tokenEnd}\"{CurrentToken()}\"");
				}
				if (IsFinishedReadingToken()) {
					if (!IsReadingToken()) {
						throw new Exception($"token ended before it began? {index}/{text.Length}");
					}
					string token = CurrentToken();
					if (readingDigits) {
//Log($"double parse '{token}'<-------------");
//string hey = ""; for (int i = 0; i < token.Length; ++i) {
//	char ch = token[i] != '\0' ? token[i] : ' '; hey += $"[{(int)token[i]} {ch}]";
//}
//Log(hey);

						double number = double.Parse(token);
						out_tokens.Add(number);
					} else {
						if (IsReadingStringLiteral()) {
							StringLiteral stringLiteral = new StringLiteral(token, tokenStart);
							out_tokens.Add(stringLiteral);
						} else {
							out_tokens.Add(new Token(token, tokenStart));
						}
					}
					tokenStart = -1;
					tokenEnd = -1;
					readingDigits = false;
					readingFloat = false;
					readingOtherToken = false;
					readingStringLiteral = '\0';
					finishedLastTokenElementWithComma = IsComma(c);
//if (finishedLastTokenElementWithComma) {
//	Log(",,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,");
//}
//Log($"finished token ({token}) {index} < {text.Length}");
					continue;
				}
				if (!finishedEarly) {
					++index;
				}
			}
//Log($"###### token count: {out_tokens.Count}   {index} < {text.Length}");
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
				case IEnumerable enumerable:
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


		public static string DebugHierarchyWithTypes(IList<object> tokens, int indent) {
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < tokens.Count; ++i) {
				if (i > 0) {
					sb.Append("\n");
				}
				for (int ind = 0; ind < indent; ++ind) {
					sb.Append("  ");
				}
				sb.Append(i);
				if (GetTokenIndex(tokens[i], out int index)) {
					sb.Append("@").Append(index);
				}
				sb.Append(" ");
				if (tokens[i] == null) {
					sb.Append("(null)");
				}
				switch (tokens[i]) {
					case IList<object> list:
						sb.Append("list (").Append(list.Count).Append(")\n");
						sb.Append(DebugHierarchyWithTypes(list, indent + 1));
						break;
					default:
						sb.Append($"({tokens[i].GetType()}) \'").Append(tokens[i].ToString()).Append("'");
						break;
				}
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
		public static char EndCap(char c) => c switch { '[' => ']', '(' => ')', '{' => '}', '\'' => '\'', '\"' => '\"', _ => '\0' };
		public static char BeginCap(char c) => c switch { ']' => '[', ')' => '(', '}' => '{', '\'' => '\'', '\"' => '\"', _ => '\0' };
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
							return new Parse.Error($"expected 4 chars after index {i}, found {end - i}", str, i);
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
